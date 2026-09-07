using System;
using System.Linq;
using System.Threading.Tasks;
using GestaoCulto.Application.DTOs.Auth;
using GestaoCulto.Application.Interfaces;
using GestaoCulto.Domain.Entities;
using GestaoCulto.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GestaoCulto.Infrastructure.Services
{
    public class AuthService : IAuthService
    {
        private readonly GestaoCultoDbContext _db;
        private readonly IPasswordHasher _passwordHasher;
        private readonly ITokenService _tokenService;
        private readonly IGoogleTokenValidator _googleTokenValidator;

        public AuthService(
            GestaoCultoDbContext db,
            IPasswordHasher passwordHasher,
            ITokenService tokenService,
            IGoogleTokenValidator googleTokenValidator)
        {
            _db = db;
            _passwordHasher = passwordHasher;
            _tokenService = tokenService;
            _googleTokenValidator = googleTokenValidator;
        }

        public async Task<AuthResponseDto> LoginAsync(LoginRequestDto request)
        {
            var usuario = await _db.Usuarios
                .Include(x => x.Perfis)
                .ThenInclude(x => x.Perfil)
                .FirstOrDefaultAsync(x => x.Email == request.Email && x.Ativo);

            if (usuario == null || !_passwordHasher.Verificar(request.Senha, usuario.SenhaHash))
            {
                throw new InvalidOperationException("E-mail ou senha inválidos.");
            }

            var perfis = usuario.Perfis.Select(x => x.Perfil.Codigo).ToList();
            var token = _tokenService.GerarToken(usuario, perfis, request.ManterConectado);
            usuario.UltimoLoginEm = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            return new AuthResponseDto
            {
                Token = token,
                ExpiraEm = request.ManterConectado ? DateTime.UtcNow.AddDays(7) : DateTime.UtcNow.AddHours(8),
                Nome = usuario.Nome,
                Email = usuario.Email,
                DeveTrocarSenha = usuario.DeveTrocarSenha,
                Perfis = perfis
            };
        }

        public async Task<AuthResponseDto> LoginComGoogleAsync(GoogleLoginRequestDto request)
        {
            var payload = await _googleTokenValidator.ValidarAsync(request.IdToken);
            if (payload == null)
            {
                throw new InvalidOperationException("Falha na validação do login Google.");
            }

            var usuario = await _db.Usuarios
                .Include(x => x.Perfis)
                .ThenInclude(x => x.Perfil)
                .Include(x => x.UsuarioGoogle)
                .FirstOrDefaultAsync(x => x.Email == payload.Email && x.Ativo);

            if (usuario == null)
            {
                throw new InvalidOperationException("Usuário não autorizado. Solicite acesso ao administrador.");
            }

            if (usuario.UsuarioGoogle == null)
            {
                usuario.UsuarioGoogle = new UsuarioGoogle
                {
                    UsuarioId = usuario.Id,
                    GoogleSub = payload.Subject,
                    EmailGoogle = payload.Email,
                    AvatarUrl = payload.Picture,
                    VinculadoEm = DateTime.UtcNow,
                    CriadoEm = DateTime.UtcNow
                };
            }
            else
            {
                usuario.UsuarioGoogle.GoogleSub = payload.Subject;
                usuario.UsuarioGoogle.EmailGoogle = payload.Email;
                usuario.UsuarioGoogle.AvatarUrl = payload.Picture;
                usuario.UsuarioGoogle.UltimoLoginGoogleEm = DateTime.UtcNow;
                usuario.UsuarioGoogle.AtualizadoEm = DateTime.UtcNow;
            }

            usuario.UltimoLoginEm = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            var perfis = usuario.Perfis.Select(x => x.Perfil.Codigo).ToList();
            return new AuthResponseDto
            {
                Token = _tokenService.GerarToken(usuario, perfis, request.ManterConectado),
                ExpiraEm = request.ManterConectado ? DateTime.UtcNow.AddDays(7) : DateTime.UtcNow.AddHours(8),
                Nome = usuario.Nome,
                Email = usuario.Email,
                DeveTrocarSenha = usuario.DeveTrocarSenha,
                Perfis = perfis
            };
        }
    }
}
