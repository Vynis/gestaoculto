using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GestaoCulto.Application.DTOs.Cadastros;
using GestaoCulto.Application.Interfaces;
using GestaoCulto.Domain.Entities;
using GestaoCulto.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace GestaoCulto.API.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/usuarios")]
    public class UsuariosController : ControllerBase
    {
        private readonly GestaoCultoDbContext _db;
        private readonly IPasswordHasher _passwordHasher;
        private readonly IConfiguration _configuration;

        public UsuariosController(GestaoCultoDbContext db, IPasswordHasher passwordHasher, IConfiguration configuration)
        {
            _db = db;
            _passwordHasher = passwordHasher;
            _configuration = configuration;
        }

        [HttpGet]
        public async Task<IActionResult> Listar([FromQuery] string? busca)
        {
            var query = _db.Usuarios.AsNoTracking();
            if (!string.IsNullOrWhiteSpace(busca))
            {
                var termo = busca.Trim().ToLower();
                query = query.Where(x => x.Nome.ToLower().Contains(termo) || x.Email.ToLower().Contains(termo));
            }

            var listaBase = await query
                .OrderBy(x => x.Nome)
                .Select(x => new UsuarioDto
                {
                    Id = x.Id,
                    Nome = x.Nome,
                    Email = x.Email,
                    Telefone = x.Telefone,
                    Ativo = x.Ativo,
                    DeveTrocarSenha = x.DeveTrocarSenha,
                    Perfis = new List<string>(),
                    PerfilIds = new List<long>()
                })
                .ToListAsync();

            var ids = listaBase.Select(x => x.Id).ToList();
            var perfis = await _db.UsuariosPerfis
                .AsNoTracking()
                .Where(x => ids.Contains(x.UsuarioId))
                .Join(_db.Perfis,
                    up => up.PerfilId,
                    p => p.Id,
                    (up, p) => new
                    {
                        up.UsuarioId,
                        up.PerfilId,
                        p.Nome,
                        p.Codigo
                    })
                .ToListAsync();

            var mapaIds = perfis
                .GroupBy(x => x.UsuarioId)
                .ToDictionary(g => g.Key, g => (IList<long>)g.Select(x => x.PerfilId).Distinct().ToList());

            var mapaNomes = perfis
                .GroupBy(x => x.UsuarioId)
                .ToDictionary(g => g.Key, g => (IList<string>)g.Select(x => $"{x.Nome} ({x.Codigo})").Distinct().ToList());

            foreach (var item in listaBase)
            {
                item.PerfilIds = mapaIds.ContainsKey(item.Id) ? mapaIds[item.Id] : new List<long>();
                item.Perfis = mapaNomes.ContainsKey(item.Id) ? mapaNomes[item.Id] : new List<string>();
            }

            return Ok(listaBase);
        }

        [HttpGet("opcoes")]
        public async Task<IActionResult> Opcoes()
        {
            var perfis = await _db.Perfis
                .AsNoTracking()
                .Where(x => x.Ativo)
                .OrderBy(x => x.Nome)
                .Select(x => new PerfilOpcaoDto
                {
                    Id = x.Id,
                    Nome = x.Nome,
                    Codigo = x.Codigo
                })
                .ToListAsync();

            return Ok(new { perfis });
        }

        [HttpPost]
        [Authorize(Roles = "ADMIN,GESTAO_CULTO")]
        public async Task<IActionResult> Criar([FromBody] UsuarioRequestDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Senha))
            {
                return BadRequest(new { mensagem = "Informe uma senha para criar o usuário." });
            }

            var emailNormalizado = NormalizarEmail(dto.Email);
            var existeEmail = await _db.Usuarios.AnyAsync(x => x.Email == emailNormalizado);
            if (existeEmail)
            {
                return BadRequest(new { mensagem = "Já existe usuário com este e-mail." });
            }

            var entity = new Usuario
            {
                Nome = dto.Nome,
                Email = emailNormalizado,
                Telefone = LimparTexto(dto.Telefone),
                Ativo = dto.Ativo,
                SenhaHash = _passwordHasher.Hash(dto.Senha.Trim()),
                CriadoEm = DateTime.UtcNow
            };

            _db.Usuarios.Add(entity);
            await _db.SaveChangesAsync();

            await SalvarPerfis(entity.Id, dto.PerfilIds);

            return Ok(new { mensagem = "Usuário criado com sucesso.", id = entity.Id });
        }

        [HttpPut("{id:long}")]
        [Authorize(Roles = "ADMIN,GESTAO_CULTO")]
        public async Task<IActionResult> Atualizar(long id, [FromBody] UsuarioRequestDto dto)
        {
            var entity = await _db.Usuarios.FirstOrDefaultAsync(x => x.Id == id);
            if (entity == null)
            {
                return NotFound(new { mensagem = "Usuário não encontrado." });
            }

            var emailNormalizado = NormalizarEmail(dto.Email);
            var existeEmail = await _db.Usuarios.AnyAsync(x => x.Email == emailNormalizado && x.Id != id);
            if (existeEmail)
            {
                return BadRequest(new { mensagem = "Já existe usuário com este e-mail." });
            }

            entity.Nome = dto.Nome;
            entity.Email = emailNormalizado;
            entity.Telefone = LimparTexto(dto.Telefone);
            entity.Ativo = dto.Ativo;
            entity.AtualizadoEm = DateTime.UtcNow;

            if (!string.IsNullOrWhiteSpace(dto.Senha))
            {
                entity.SenhaHash = _passwordHasher.Hash(dto.Senha.Trim());
                entity.DeveTrocarSenha = false;
            }

            var perfisAtuais = await _db.UsuariosPerfis.Where(x => x.UsuarioId == id).ToListAsync();
            if (perfisAtuais.Any())
            {
                _db.UsuariosPerfis.RemoveRange(perfisAtuais);
            }

            await _db.SaveChangesAsync();
            await SalvarPerfis(id, dto.PerfilIds);

            return Ok(new { mensagem = "Usuário atualizado com sucesso." });
        }

        [HttpDelete("{id:long}")]
        [Authorize(Roles = "ADMIN,GESTAO_CULTO")]
        public async Task<IActionResult> Excluir(long id)
        {
            var entity = await _db.Usuarios.FirstOrDefaultAsync(x => x.Id == id);
            if (entity == null)
            {
                return NotFound(new { mensagem = "Usuário não encontrado." });
            }

            var perfisAtuais = await _db.UsuariosPerfis.Where(x => x.UsuarioId == id).ToListAsync();
            if (perfisAtuais.Any())
            {
                _db.UsuariosPerfis.RemoveRange(perfisAtuais);
            }

            _db.Usuarios.Remove(entity);
            await _db.SaveChangesAsync();

            return Ok(new { mensagem = "Usuário excluído com sucesso." });
        }

        [HttpPost("{id:long}/resetar-senha")]
        [Authorize(Roles = "ADMIN")]
        public async Task<IActionResult> ResetarSenha(long id)
        {
            var senhaPadrao = _configuration["AdminPasswordReset:DefaultPassword"]?.Trim();
            if (string.IsNullOrWhiteSpace(senhaPadrao) || senhaPadrao.Length < 6)
            {
                return StatusCode(503, new { mensagem = "A senha padrão administrativa não está configurada no servidor." });
            }

            var usuario = await _db.Usuarios.FirstOrDefaultAsync(x => x.Id == id);
            if (usuario == null)
            {
                return NotFound(new { mensagem = "Usuário não encontrado." });
            }

            usuario.SenhaHash = _passwordHasher.Hash(senhaPadrao);
            usuario.DeveTrocarSenha = true;
            usuario.AtualizadoEm = DateTime.UtcNow;

            var recuperacoesPendentes = await _db.UsuariosRecuperacaoSenha
                .Where(x => x.UsuarioId == id && x.UsadoEm == null)
                .ToListAsync();
            foreach (var recuperacao in recuperacoesPendentes)
            {
                recuperacao.UsadoEm = DateTime.UtcNow;
                recuperacao.AtualizadoEm = DateTime.UtcNow;
            }

            await _db.SaveChangesAsync();
            return Ok(new { mensagem = "Senha redefinida. O usuário deverá alterá-la no próximo acesso." });
        }

        private async Task SalvarPerfis(long usuarioId, IList<long> perfilIds)
        {
            var ids = (perfilIds ?? new List<long>())
                .Where(x => x > 0)
                .Distinct()
                .ToList();

            if (!ids.Any())
            {
                return;
            }

            var idsValidos = await _db.Perfis
                .AsNoTracking()
                .Where(x => ids.Contains(x.Id) && x.Ativo)
                .Select(x => x.Id)
                .ToListAsync();

            foreach (var perfilId in idsValidos)
            {
                _db.UsuariosPerfis.Add(new UsuarioPerfil
                {
                    UsuarioId = usuarioId,
                    PerfilId = perfilId
                });
            }

            await _db.SaveChangesAsync();
        }

        private static string NormalizarEmail(string email)
        {
            return (email ?? string.Empty).Trim().ToLowerInvariant();
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
