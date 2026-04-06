using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using GestaoCulto.Application.DTOs.Cadastros;
using GestaoCulto.Domain.Entities;
using GestaoCulto.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GestaoCulto.API.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/voluntarios")]
    public class VoluntariosController : ControllerBase
    {
        private readonly GestaoCultoDbContext _db;

        public VoluntariosController(GestaoCultoDbContext db)
        {
            _db = db;
        }

        [HttpGet]
        public async Task<IActionResult> Listar()
        {
            var listaBase = await _db.Voluntarios
                .AsNoTracking()
                .OrderBy(v => v.Nome)
                .Select(v => new VoluntarioDto
                {
                    Id = v.Id,
                    UsuarioId = v.UsuarioId,
                    UsuarioNome = _db.Usuarios.Where(u => u.Id == v.UsuarioId).Select(u => u.Nome).FirstOrDefault(),
                    Nome = v.Nome,
                    Telefone = v.Telefone,
                    Email = v.Email,
                    MinisterioPrincipalId = v.MinisterioPrincipalId,
                    Observacoes = v.Observacoes,
                    RestricoesIndisponibilidade = v.RestricoesIndisponibilidade,
                    Ativo = v.Ativo,
                    MinisterioIds = new List<long>()
                })
                .ToListAsync();

            var idsVoluntarios = listaBase.Select(x => x.Id).ToList();
            var vinculos = await _db.MinisteriosVoluntarios
                .AsNoTracking()
                .Where(x => idsVoluntarios.Contains(x.VoluntarioId))
                .OrderByDescending(x => x.Principal)
                .ThenBy(x => x.MinisterioId)
                .Select(x => new { x.VoluntarioId, x.MinisterioId })
                .ToListAsync();

            var mapaVinculos = vinculos
                .GroupBy(x => x.VoluntarioId)
                .ToDictionary(g => g.Key, g => g.Select(x => x.MinisterioId).ToList());

            foreach (var item in listaBase)
            {
                item.MinisterioIds = mapaVinculos.ContainsKey(item.Id)
                    ? mapaVinculos[item.Id]
                    : new List<long>();
            }

            return Ok(listaBase);
        }

        [HttpGet("opcoes")]
        public async Task<IActionResult> Opcoes()
        {
            var vinculos = await _db.Voluntarios
                .AsNoTracking()
                .Where(v => v.UsuarioId.HasValue)
                .Select(v => new { v.Id, v.UsuarioId })
                .ToListAsync();

            var mapaVinculos = vinculos
                .Where(x => x.UsuarioId.HasValue)
                .ToDictionary(x => x.UsuarioId!.Value, x => (long?)x.Id);

            var usuariosBase = await _db.Usuarios
                .AsNoTracking()
                .Where(u => u.Ativo)
                .OrderBy(u => u.Nome)
                .Select(u => new VoluntarioUsuarioOpcaoDto
                {
                    Id = u.Id,
                    Nome = u.Nome,
                    Email = u.Email
                })
                .ToListAsync();

            foreach (var usuario in usuariosBase)
            {
                usuario.VoluntarioId = mapaVinculos.ContainsKey(usuario.Id) ? mapaVinculos[usuario.Id] : null;
            }

            return Ok(new { usuarios = usuariosBase });
        }

        [HttpPost]
        [Authorize(Roles = "ADMIN,GESTAO_CULTO,LIDER_MINISTERIO")]
        public async Task<IActionResult> Criar([FromBody] VoluntarioDto dto)
        {
            var nome = (dto.Nome ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(nome))
            {
                return BadRequest(new { mensagem = "Informe o nome do voluntário." });
            }

            var email = NormalizarEmail(dto.Email);

            var nomeDuplicado = await ExisteNomeDuplicado(nome, null);
            if (nomeDuplicado)
            {
                return BadRequest(new { mensagem = "Já existe um voluntário cadastrado com este nome." });
            }

            if (!string.IsNullOrWhiteSpace(email))
            {
                var emailDuplicado = await ExisteEmailDuplicado(email, null);
                if (emailDuplicado)
                {
                    return BadRequest(new { mensagem = "Já existe um voluntário cadastrado com este e-mail." });
                }
            }

            if (dto.UsuarioId.HasValue && dto.UsuarioId.Value > 0)
            {
                var usuarioExiste = await _db.Usuarios.AnyAsync(x => x.Id == dto.UsuarioId.Value);
                if (!usuarioExiste)
                {
                    return BadRequest(new { mensagem = "Usuário informado não existe." });
                }

                var usuarioJaVinculado = await _db.Voluntarios.AnyAsync(x => x.UsuarioId == dto.UsuarioId.Value);
                if (usuarioJaVinculado)
                {
                    return BadRequest(new { mensagem = "Este usuário já está vinculado a outro voluntário." });
                }
            }

            var ministerioIds = (dto.MinisterioIds ?? new List<long>())
                .Where(x => x > 0)
                .Distinct()
                .ToList();

            if (!ministerioIds.Any() && dto.MinisterioPrincipalId.HasValue && dto.MinisterioPrincipalId.Value > 0)
            {
                ministerioIds.Add(dto.MinisterioPrincipalId.Value);
            }

            var ministerioPrincipalId = dto.MinisterioPrincipalId;
            if (ministerioPrincipalId.HasValue && ministerioPrincipalId.Value > 0 && !ministerioIds.Contains(ministerioPrincipalId.Value))
            {
                ministerioIds.Insert(0, ministerioPrincipalId.Value);
            }

            if (!ministerioPrincipalId.HasValue && ministerioIds.Any())
            {
                ministerioPrincipalId = ministerioIds[0];
            }

            var entity = new Voluntario
            {
                UsuarioId = dto.UsuarioId.HasValue && dto.UsuarioId.Value > 0 ? dto.UsuarioId : null,
                Nome = nome,
                Telefone = dto.Telefone,
                Email = email,
                MinisterioPrincipalId = ministerioPrincipalId,
                Observacoes = dto.Observacoes,
                RestricoesIndisponibilidade = dto.RestricoesIndisponibilidade,
                Ativo = dto.Ativo
            };

            _db.Voluntarios.Add(entity);
            await _db.SaveChangesAsync();

            if (ministerioIds.Any())
            {
                var vinculos = ministerioIds.Select(id => new MinisterioVoluntario
                {
                    MinisterioId = id,
                    VoluntarioId = entity.Id,
                    Principal = entity.MinisterioPrincipalId == id,
                    CriadoEm = System.DateTime.UtcNow
                }).ToList();

                _db.MinisteriosVoluntarios.AddRange(vinculos);
                await _db.SaveChangesAsync();
            }

            dto.Id = entity.Id;
            dto.MinisterioPrincipalId = entity.MinisterioPrincipalId;
            dto.MinisterioIds = ministerioIds;
            return CreatedAtAction(nameof(Listar), new { id = entity.Id }, dto);
        }

        [HttpPut("{id:long}")]
        [Authorize(Roles = "ADMIN,GESTAO_CULTO,LIDER_MINISTERIO")]
        public async Task<IActionResult> Atualizar(long id, [FromBody] VoluntarioDto dto)
        {
            var entity = await _db.Voluntarios.FirstOrDefaultAsync(x => x.Id == id);
            if (entity == null)
            {
                return NotFound(new { mensagem = "Voluntário não encontrado." });
            }

            var nome = (dto.Nome ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(nome))
            {
                return BadRequest(new { mensagem = "Informe o nome do voluntário." });
            }

            var email = NormalizarEmail(dto.Email);

            var nomeDuplicado = await ExisteNomeDuplicado(nome, id);
            if (nomeDuplicado)
            {
                return BadRequest(new { mensagem = "Já existe um voluntário cadastrado com este nome." });
            }

            if (!string.IsNullOrWhiteSpace(email))
            {
                var emailDuplicado = await ExisteEmailDuplicado(email, id);
                if (emailDuplicado)
                {
                    return BadRequest(new { mensagem = "Já existe um voluntário cadastrado com este e-mail." });
                }
            }

            if (dto.UsuarioId.HasValue && dto.UsuarioId.Value > 0)
            {
                var usuarioExiste = await _db.Usuarios.AnyAsync(x => x.Id == dto.UsuarioId.Value);
                if (!usuarioExiste)
                {
                    return BadRequest(new { mensagem = "Usuário informado não existe." });
                }

                var usuarioJaVinculado = await _db.Voluntarios.AnyAsync(x => x.UsuarioId == dto.UsuarioId.Value && x.Id != id);
                if (usuarioJaVinculado)
                {
                    return BadRequest(new { mensagem = "Este usuário já está vinculado a outro voluntário." });
                }
            }

            var ministerioIds = (dto.MinisterioIds ?? new List<long>())
                .Where(x => x > 0)
                .Distinct()
                .ToList();

            if (!ministerioIds.Any() && dto.MinisterioPrincipalId.HasValue && dto.MinisterioPrincipalId.Value > 0)
            {
                ministerioIds.Add(dto.MinisterioPrincipalId.Value);
            }

            var ministerioPrincipalId = dto.MinisterioPrincipalId;
            if (ministerioPrincipalId.HasValue && ministerioPrincipalId.Value > 0 && !ministerioIds.Contains(ministerioPrincipalId.Value))
            {
                ministerioIds.Insert(0, ministerioPrincipalId.Value);
            }

            if (!ministerioPrincipalId.HasValue && ministerioIds.Any())
            {
                ministerioPrincipalId = ministerioIds[0];
            }

            entity.Nome = nome;
            entity.UsuarioId = dto.UsuarioId.HasValue && dto.UsuarioId.Value > 0 ? dto.UsuarioId : null;
            entity.Telefone = dto.Telefone;
            entity.Email = email;
            entity.MinisterioPrincipalId = ministerioPrincipalId;
            entity.Observacoes = dto.Observacoes;
            entity.RestricoesIndisponibilidade = dto.RestricoesIndisponibilidade;
            entity.Ativo = dto.Ativo;
            entity.AtualizadoEm = System.DateTime.UtcNow;

            var vinculosAntigos = await _db.MinisteriosVoluntarios
                .Where(x => x.VoluntarioId == entity.Id)
                .ToListAsync();

            if (vinculosAntigos.Any())
            {
                _db.MinisteriosVoluntarios.RemoveRange(vinculosAntigos);
            }

            if (ministerioIds.Any())
            {
                var vinculosNovos = ministerioIds.Select(ministerioId => new MinisterioVoluntario
                {
                    MinisterioId = ministerioId,
                    VoluntarioId = entity.Id,
                    Principal = entity.MinisterioPrincipalId == ministerioId,
                    CriadoEm = System.DateTime.UtcNow
                }).ToList();

                _db.MinisteriosVoluntarios.AddRange(vinculosNovos);
            }

            await _db.SaveChangesAsync();

            dto.Id = entity.Id;
            dto.UsuarioId = entity.UsuarioId;
            dto.MinisterioPrincipalId = entity.MinisterioPrincipalId;
            dto.MinisterioIds = ministerioIds;
            return Ok(dto);
        }

        [HttpDelete("{id:long}")]
        [Authorize(Roles = "ADMIN,GESTAO_CULTO,LIDER_MINISTERIO")]
        public async Task<IActionResult> Excluir(long id)
        {
            var entity = await _db.Voluntarios.FirstOrDefaultAsync(x => x.Id == id);
            if (entity == null)
            {
                return NotFound(new { mensagem = "Voluntário não encontrado." });
            }

            var possuiEscalas = await _db.Escalas.AnyAsync(x => x.VoluntarioId == id);
            if (possuiEscalas)
            {
                return BadRequest(new
                {
                    mensagem = "Não é possível excluir voluntário com escalas vinculadas. Remova as escalas primeiro."
                });
            }

            _db.Voluntarios.Remove(entity);
            await _db.SaveChangesAsync();

            return Ok(new { mensagem = "Voluntário excluído com sucesso." });
        }

        private async Task<bool> ExisteNomeDuplicado(string nome, long? ignorarId)
        {
            var nomeNormalizado = nome.Trim().ToLower();
            return await _db.Voluntarios.AnyAsync(x =>
                (!ignorarId.HasValue || x.Id != ignorarId.Value)
                && x.Nome.ToLower() == nomeNormalizado);
        }

        private async Task<bool> ExisteEmailDuplicado(string email, long? ignorarId)
        {
            var emailNormalizado = email.Trim().ToLower();
            return await _db.Voluntarios.AnyAsync(x =>
                (!ignorarId.HasValue || x.Id != ignorarId.Value)
                && x.Email != null
                && x.Email.ToLower() == emailNormalizado);
        }

        private static string? NormalizarEmail(string? email)
        {
            var valor = (email ?? string.Empty).Trim();
            return string.IsNullOrWhiteSpace(valor) ? null : valor.ToLower();
        }
    }
}
