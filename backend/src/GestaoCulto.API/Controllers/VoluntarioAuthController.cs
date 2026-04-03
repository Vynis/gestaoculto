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

namespace GestaoCulto.API.Controllers
{
    public class VoluntarioSolicitarCodigoRequest
    {
        public string Identificador { get; set; } = string.Empty;
        public string? Canal { get; set; }
    }

    public class VoluntarioAtivarAcessoRequest
    {
        public string Identificador { get; set; } = string.Empty;
        public string Codigo { get; set; } = string.Empty;
        public string Senha { get; set; } = string.Empty;
        public bool ManterConectado { get; set; } = true;
        public string? Nome { get; set; }
        public string? Email { get; set; }
        public string? Telefone { get; set; }
    }

    public class VoluntarioRedefinirAcessoRequest
    {
        public string Identificador { get; set; } = string.Empty;
        public string Codigo { get; set; } = string.Empty;
        public string NovaSenha { get; set; } = string.Empty;
    }

    [ApiController]
    [Route("api/voluntario-auth")]
    public class VoluntarioAuthController : ControllerBase
    {
        private readonly GestaoCultoDbContext _db;
        private readonly IPasswordHasher _passwordHasher;
        private readonly ITokenService _tokenService;

        public VoluntarioAuthController(
            GestaoCultoDbContext db,
            IPasswordHasher passwordHasher,
            ITokenService tokenService)
        {
            _db = db;
            _passwordHasher = passwordHasher;
            _tokenService = tokenService;
        }

        [AllowAnonymous]
        [HttpPost("solicitar-ativacao")]
        public async Task<IActionResult> SolicitarAtivacao([FromBody] VoluntarioSolicitarCodigoRequest request)
        {
            var voluntario = await BuscarVoluntarioPorIdentificador(request.Identificador, apenasComUsuario: false);
            if (voluntario == null)
            {
                return BadRequest(new { mensagem = "Voluntário não encontrado. Verifique e-mail/telefone informado." });
            }

            if (!voluntario.Ativo)
            {
                return BadRequest(new { mensagem = "Seu cadastro está inativo. Procure a gestão do culto." });
            }

            var codigo = GerarCodigoNumerico(6);
            _db.VoluntariosAcessoAtivacao.Add(new VoluntarioAcessoAtivacao
            {
                VoluntarioId = voluntario.Id,
                CodigoHash = CalcularHash(codigo),
                Canal = LimparTexto(request.Canal),
                ExpiraEm = DateTime.UtcNow.AddHours(24),
                Tentativas = 0,
                CriadoEm = DateTime.UtcNow
            });

            await _db.SaveChangesAsync();

            return Ok(new
            {
                mensagem = "Código de ativação gerado com sucesso.",
                codigo,
                expiraEm = DateTime.UtcNow.AddHours(24),
                voluntario = voluntario.Nome
            });
        }

        [AllowAnonymous]
        [HttpPost("ativar-acesso")]
        public async Task<IActionResult> AtivarAcesso([FromBody] VoluntarioAtivarAcessoRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Senha) || request.Senha.Trim().Length < 6)
            {
                return BadRequest(new { mensagem = "A senha deve ter no mínimo 6 caracteres." });
            }

            var voluntario = await BuscarVoluntarioPorIdentificador(request.Identificador, apenasComUsuario: false);
            if (voluntario == null)
            {
                return BadRequest(new { mensagem = "Voluntário não encontrado." });
            }

            var codigoHash = CalcularHash(request.Codigo);
            var ativacao = await _db.VoluntariosAcessoAtivacao
                .Where(x => x.VoluntarioId == voluntario.Id && x.UsadoEm == null)
                .OrderByDescending(x => x.Id)
                .FirstOrDefaultAsync();

            if (ativacao == null || ativacao.ExpiraEm < DateTime.UtcNow)
            {
                return BadRequest(new { mensagem = "Código inválido ou expirado. Gere um novo código." });
            }

            if (ativacao.Tentativas >= 5)
            {
                return BadRequest(new { mensagem = "Muitas tentativas inválidas. Gere um novo código." });
            }

            if (!string.Equals(ativacao.CodigoHash, codigoHash, StringComparison.Ordinal))
            {
                ativacao.Tentativas += 1;
                ativacao.AtualizadoEm = DateTime.UtcNow;
                await _db.SaveChangesAsync();
                return BadRequest(new { mensagem = "Código inválido." });
            }

            var email = EscolherEmailVoluntario(voluntario, request.Email);
            if (string.IsNullOrWhiteSpace(email))
            {
                return BadRequest(new { mensagem = "Informe um e-mail válido para ativar seu acesso." });
            }

            var usuario = voluntario.UsuarioId.HasValue
                ? await _db.Usuarios.Include(x => x.Perfis).FirstOrDefaultAsync(x => x.Id == voluntario.UsuarioId.Value)
                : null;

            if (usuario == null)
            {
                var emailJaExiste = await _db.Usuarios.AnyAsync(x => x.Email == email && x.Id != (voluntario.UsuarioId ?? 0));
                if (emailJaExiste)
                {
                    return BadRequest(new { mensagem = "Este e-mail já está em uso por outro acesso. Procure a gestão." });
                }

                usuario = new Usuario
                {
                    Nome = LimparTexto(request.Nome) ?? voluntario.Nome,
                    Email = email,
                    Telefone = EscolherTelefoneVoluntario(voluntario, request.Telefone),
                    SenhaHash = _passwordHasher.Hash(request.Senha.Trim()),
                    Ativo = true,
                    OrigemConta = "VOLUNTARIO_ATIVACAO",
                    DeveTrocarSenha = false,
                    CriadoEm = DateTime.UtcNow
                };
                _db.Usuarios.Add(usuario);
                await _db.SaveChangesAsync();
                voluntario.UsuarioId = usuario.Id;
            }
            else
            {
                var emailJaExiste = await _db.Usuarios.AnyAsync(x => x.Email == email && x.Id != usuario.Id);
                if (emailJaExiste)
                {
                    return BadRequest(new { mensagem = "Este e-mail já está em uso por outro acesso. Procure a gestão." });
                }

                usuario.Nome = LimparTexto(request.Nome) ?? usuario.Nome;
                usuario.Email = email;
                usuario.Telefone = EscolherTelefoneVoluntario(voluntario, request.Telefone);
                usuario.SenhaHash = _passwordHasher.Hash(request.Senha.Trim());
                usuario.Ativo = true;
                usuario.DeveTrocarSenha = false;
                usuario.OrigemConta = usuario.OrigemConta ?? "VOLUNTARIO_ATIVACAO";
                usuario.AtualizadoEm = DateTime.UtcNow;
            }

            voluntario.Nome = LimparTexto(request.Nome) ?? voluntario.Nome;
            voluntario.Email = email;
            voluntario.Telefone = EscolherTelefoneVoluntario(voluntario, request.Telefone);
            voluntario.AtualizadoEm = DateTime.UtcNow;

            var perfilVoluntario = await _db.Perfis.FirstOrDefaultAsync(x => x.Codigo == "VOLUNTARIO");
            if (perfilVoluntario != null)
            {
                var possuiPerfil = await _db.UsuariosPerfis.AnyAsync(x => x.UsuarioId == usuario.Id && x.PerfilId == perfilVoluntario.Id);
                if (!possuiPerfil)
                {
                    _db.UsuariosPerfis.Add(new UsuarioPerfil
                    {
                        UsuarioId = usuario.Id,
                        PerfilId = perfilVoluntario.Id
                    });
                }
            }

            ativacao.UsadoEm = DateTime.UtcNow;
            ativacao.AtualizadoEm = DateTime.UtcNow;

            await _db.SaveChangesAsync();

            var perfis = await _db.UsuariosPerfis
                .Where(x => x.UsuarioId == usuario.Id)
                .Join(_db.Perfis, up => up.PerfilId, p => p.Id, (up, p) => p.Codigo)
                .Distinct()
                .ToListAsync();

            return Ok(new AuthResponseDto
            {
                Token = _tokenService.GerarToken(usuario, perfis, request.ManterConectado),
                ExpiraEm = request.ManterConectado ? DateTime.UtcNow.AddDays(7) : DateTime.UtcNow.AddHours(8),
                Nome = usuario.Nome,
                Email = usuario.Email,
                Perfis = perfis
            });
        }

        [AllowAnonymous]
        [HttpPost("solicitar-recuperacao")]
        public async Task<IActionResult> SolicitarRecuperacao([FromBody] VoluntarioSolicitarCodigoRequest request)
        {
            var voluntario = await BuscarVoluntarioPorIdentificador(request.Identificador, apenasComUsuario: true);
            if (voluntario == null)
            {
                return BadRequest(new { mensagem = "Acesso não encontrado para o identificador informado." });
            }

            var codigo = GerarCodigoNumerico(6);
            _db.VoluntariosAcessoRecuperacao.Add(new VoluntarioAcessoRecuperacao
            {
                VoluntarioId = voluntario.Id,
                CodigoHash = CalcularHash(codigo),
                Canal = LimparTexto(request.Canal),
                ExpiraEm = DateTime.UtcNow.AddMinutes(30),
                Tentativas = 0,
                CriadoEm = DateTime.UtcNow
            });

            await _db.SaveChangesAsync();

            return Ok(new
            {
                mensagem = "Código de recuperação gerado com sucesso.",
                codigo,
                expiraEm = DateTime.UtcNow.AddMinutes(30),
                voluntario = voluntario.Nome
            });
        }

        [AllowAnonymous]
        [HttpPost("redefinir-acesso")]
        public async Task<IActionResult> RedefinirAcesso([FromBody] VoluntarioRedefinirAcessoRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.NovaSenha) || request.NovaSenha.Trim().Length < 6)
            {
                return BadRequest(new { mensagem = "A nova senha deve ter no mínimo 6 caracteres." });
            }

            var voluntario = await BuscarVoluntarioPorIdentificador(request.Identificador, apenasComUsuario: true);
            if (voluntario == null || !voluntario.UsuarioId.HasValue)
            {
                return BadRequest(new { mensagem = "Acesso não encontrado para o identificador informado." });
            }

            var recuperacao = await _db.VoluntariosAcessoRecuperacao
                .Where(x => x.VoluntarioId == voluntario.Id && x.UsadoEm == null)
                .OrderByDescending(x => x.Id)
                .FirstOrDefaultAsync();

            if (recuperacao == null || recuperacao.ExpiraEm < DateTime.UtcNow)
            {
                return BadRequest(new { mensagem = "Código inválido ou expirado. Solicite novo código." });
            }

            if (recuperacao.Tentativas >= 5)
            {
                return BadRequest(new { mensagem = "Muitas tentativas inválidas. Solicite novo código." });
            }

            var codigoHash = CalcularHash(request.Codigo);
            if (!string.Equals(recuperacao.CodigoHash, codigoHash, StringComparison.Ordinal))
            {
                recuperacao.Tentativas += 1;
                recuperacao.AtualizadoEm = DateTime.UtcNow;
                await _db.SaveChangesAsync();
                return BadRequest(new { mensagem = "Código inválido." });
            }

            var usuario = await _db.Usuarios.FirstOrDefaultAsync(x => x.Id == voluntario.UsuarioId.Value);
            if (usuario == null)
            {
                return BadRequest(new { mensagem = "Usuário vinculado não encontrado." });
            }

            usuario.SenhaHash = _passwordHasher.Hash(request.NovaSenha.Trim());
            usuario.DeveTrocarSenha = false;
            usuario.AtualizadoEm = DateTime.UtcNow;

            recuperacao.UsadoEm = DateTime.UtcNow;
            recuperacao.AtualizadoEm = DateTime.UtcNow;

            await _db.SaveChangesAsync();
            return Ok(new { mensagem = "Senha redefinida com sucesso." });
        }

        private async Task<Voluntario?> BuscarVoluntarioPorIdentificador(string identificador, bool apenasComUsuario)
        {
            var email = NormalizarEmail(identificador);
            var telefone = NormalizarTelefone(identificador);

            var query = _db.Voluntarios.AsQueryable();
            if (apenasComUsuario)
            {
                query = query.Where(x => x.UsuarioId.HasValue);
            }

            var ordenado = query
                .OrderByDescending(x => x.Ativo)
                .ThenBy(x => x.Id);

            if (!string.IsNullOrEmpty(email))
            {
                var porEmail = await ordenado.FirstOrDefaultAsync(x => x.Email != null && x.Email.ToLower() == email);
                if (porEmail != null)
                {
                    return porEmail;
                }
            }

            if (!string.IsNullOrEmpty(telefone))
            {
                var candidatosTelefone = await ordenado
                    .Where(x => x.Telefone != null)
                    .ToListAsync();

                return candidatosTelefone.FirstOrDefault(x => NormalizarTelefone(x.Telefone) == telefone);
            }

            return null;
        }

        private static string GerarCodigoNumerico(int tamanho)
        {
            const string digitos = "0123456789";
            var bytes = new byte[tamanho];
            RandomNumberGenerator.Fill(bytes);

            var sb = new StringBuilder(tamanho);
            for (var i = 0; i < tamanho; i += 1)
            {
                sb.Append(digitos[bytes[i] % digitos.Length]);
            }

            return sb.ToString();
        }

        private static string CalcularHash(string valor)
        {
            using var sha = SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes((valor ?? string.Empty).Trim());
            var hash = sha.ComputeHash(bytes);
            return Convert.ToBase64String(hash);
        }

        private static string? EscolherEmailVoluntario(Voluntario voluntario, string? emailInformado)
        {
            var email = NormalizarEmail(emailInformado);
            if (!string.IsNullOrWhiteSpace(email))
            {
                return email;
            }

            email = NormalizarEmail(voluntario.Email);
            return string.IsNullOrWhiteSpace(email) ? null : email;
        }

        private static string? EscolherTelefoneVoluntario(Voluntario voluntario, string? telefoneInformado)
        {
            var telefone = LimparTexto(telefoneInformado);
            if (!string.IsNullOrWhiteSpace(telefone))
            {
                return telefone;
            }

            return LimparTexto(voluntario.Telefone);
        }

        private static string NormalizarEmail(string? email)
        {
            return (email ?? string.Empty).Trim().ToLowerInvariant();
        }

        private static string NormalizarTelefone(string? telefone)
        {
            var texto = telefone ?? string.Empty;
            var chars = texto.Where(char.IsDigit).ToArray();
            return new string(chars);
        }

        private static string? LimparTexto(string? valor)
        {
            if (string.IsNullOrWhiteSpace(valor))
            {
                return null;
            }

            return valor.Trim();
        }
    }
}
