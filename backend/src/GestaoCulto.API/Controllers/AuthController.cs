using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using GestaoCulto.Application.DTOs.Auth;
using GestaoCulto.Application.Interfaces;
using GestaoCulto.Domain.Entities;
using GestaoCulto.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace GestaoCulto.API.Controllers
{
    public class EsqueciSenhaRequest
    {
        public string Email { get; set; } = string.Empty;
    }

    public class RedefinirSenhaPorTokenRequest
    {
        public string Token { get; set; } = string.Empty;
        public string NovaSenha { get; set; } = string.Empty;
    }

    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly GestaoCultoDbContext _db;
        private readonly IPasswordHasher _passwordHasher;
        private readonly IEmailSender _emailSender;
        private readonly IConfiguration _configuration;

        public AuthController(
            IAuthService authService,
            GestaoCultoDbContext db,
            IPasswordHasher passwordHasher,
            IEmailSender emailSender,
            IConfiguration configuration)
        {
            _authService = authService;
            _db = db;
            _passwordHasher = passwordHasher;
            _emailSender = emailSender;
            _configuration = configuration;
        }

        [AllowAnonymous]
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequestDto request)
        {
            var response = await _authService.LoginAsync(request);
            return Ok(response);
        }

        [AllowAnonymous]
        [HttpPost("google")]
        public async Task<IActionResult> LoginGoogle([FromBody] GoogleLoginRequestDto request)
        {
            var response = await _authService.LoginComGoogleAsync(request);
            return Ok(response);
        }

        [AllowAnonymous]
        [HttpPost("esqueci-senha")]
        public async Task<IActionResult> EsqueciSenha([FromBody] EsqueciSenhaRequest request)
        {
            var email = (request.Email ?? string.Empty).Trim().ToLower();
            if (string.IsNullOrWhiteSpace(email))
            {
                return BadRequest(new { mensagem = "Informe um e-mail válido." });
            }

            var usuario = await _db.Usuarios
                .AsTracking()
                .FirstOrDefaultAsync(x => x.Ativo && x.Email.ToLower() == email);

            if (usuario != null)
            {
                var token = GerarTokenSeguro();
                _db.UsuariosRecuperacaoSenha.Add(new UsuarioRecuperacaoSenha
                {
                    UsuarioId = usuario.Id,
                    TokenHash = CalcularHash(token),
                    Contexto = "GESTAO",
                    ExpiraEm = DateTime.UtcNow.AddMinutes(ObterExpiracaoMinutos()),
                    CriadoEm = DateTime.UtcNow
                });

                await _db.SaveChangesAsync();

                var link = $"{ObterFrontendBaseUrl().TrimEnd('/')}/auth/recuperar-senha?token={Uri.EscapeDataString(token)}";
                var html = $@"
                    <p>Olá {usuario.Nome},</p>
                    <p>Recebemos um pedido para redefinir sua senha da Gestão de Culto.</p>
                    <p><a href=""{link}"">Clique aqui para redefinir sua senha</a></p>
                    <p>Se você não solicitou, ignore este e-mail.</p>
                    <p>Este link expira em {ObterExpiracaoMinutos()} minutos.</p>";

                await _emailSender.SendAsync(usuario.Email, "Redefinição de senha - Gestão de Culto", html);
            }

            return Ok(new { mensagem = "Se o e-mail estiver cadastrado, você receberá um link para redefinir a senha." });
        }

        [AllowAnonymous]
        [HttpPost("redefinir-senha")]
        public async Task<IActionResult> RedefinirSenha([FromBody] RedefinirSenhaPorTokenRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.NovaSenha) || request.NovaSenha.Trim().Length < 6)
            {
                return BadRequest(new { mensagem = "A nova senha deve ter no mínimo 6 caracteres." });
            }

            var tokenHash = CalcularHash(request.Token);
            var recuperacao = await _db.UsuariosRecuperacaoSenha
                .Include(x => x.Usuario)
                .Where(x => x.Contexto == "GESTAO" && x.TokenHash == tokenHash && x.UsadoEm == null)
                .OrderByDescending(x => x.Id)
                .FirstOrDefaultAsync();

            if (recuperacao == null || recuperacao.ExpiraEm < DateTime.UtcNow)
            {
                return BadRequest(new { mensagem = "Link inválido ou expirado. Solicite uma nova recuperação." });
            }

            recuperacao.Usuario.SenhaHash = _passwordHasher.Hash(request.NovaSenha.Trim());
            recuperacao.Usuario.DeveTrocarSenha = false;
            recuperacao.Usuario.AtualizadoEm = DateTime.UtcNow;
            recuperacao.UsadoEm = DateTime.UtcNow;
            recuperacao.AtualizadoEm = DateTime.UtcNow;

            await _db.SaveChangesAsync();
            return Ok(new { mensagem = "Senha redefinida com sucesso." });
        }

        private int ObterExpiracaoMinutos()
        {
            var raw = _configuration["PasswordReset:ExpirationMinutes"];
            return int.TryParse(raw, out var minutos) && minutos > 0 ? minutos : 30;
        }

        private string ObterFrontendBaseUrl()
        {
            return _configuration["PasswordReset:FrontendBaseUrl"]?.TrimEnd('/')
                   ?? "http://localhost:4200";
        }

        private static string GerarTokenSeguro()
        {
            var bytes = new byte[32];
            RandomNumberGenerator.Fill(bytes);
            return Convert.ToBase64String(bytes)
                .Replace('+', '-')
                .Replace('/', '_')
                .Replace("=", string.Empty);
        }

        private static string CalcularHash(string valor)
        {
            using var sha = SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes((valor ?? string.Empty).Trim());
            return Convert.ToBase64String(sha.ComputeHash(bytes));
        }
    }
}
