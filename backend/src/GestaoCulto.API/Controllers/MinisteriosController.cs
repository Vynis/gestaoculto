using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GestaoCulto.Domain.Entities;
using GestaoCulto.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GestaoCulto.API.Controllers
{
    public class MinisterioLiderRequest
    {
        public long UsuarioId { get; set; }
        public bool Principal { get; set; }
    }

    public class MinisterioRequest
    {
        public string Nome { get; set; } = string.Empty;
        public string? Descricao { get; set; }
        public bool Ativo { get; set; } = true;
        public List<MinisterioLiderRequest> Lideres { get; set; } = new List<MinisterioLiderRequest>();
        public List<long> VoluntarioIds { get; set; } = new List<long>();
        public List<MinisterioFuncaoPadraoRequest> FuncoesPadrao { get; set; } = new List<MinisterioFuncaoPadraoRequest>();
    }

    public class MinisterioFuncaoPadraoRequest
    {
        public string Nome { get; set; } = string.Empty;
        public string? BlocoCronograma { get; set; }
        public int? Ordem { get; set; }
        public bool Ativo { get; set; } = true;
    }

    [ApiController]
    [Authorize]
    [Route("api/ministerios")]
    public class MinisteriosController : ControllerBase
    {
        private readonly GestaoCultoDbContext _db;

        public MinisteriosController(GestaoCultoDbContext db)
        {
            _db = db;
        }

        [HttpGet]
        public async Task<IActionResult> Listar([FromQuery] string? busca)
        {
            var query = _db.Ministerios.AsNoTracking();
            if (!string.IsNullOrWhiteSpace(busca))
            {
                var termo = busca.Trim().ToLower();
                query = query.Where(x => x.Nome.ToLower().Contains(termo) || (x.Descricao != null && x.Descricao.ToLower().Contains(termo)));
            }

            var listaBase = await query
                .OrderBy(x => x.Nome)
                .Select(x => new
                {
                    x.Id,
                    x.Nome,
                    x.Codigo,
                    x.Descricao,
                    x.Ativo
                })
                .ToListAsync();

            var ids = listaBase.Select(x => x.Id).ToList();
            var lideres = await _db.MinisteriosLideres
                .AsNoTracking()
                .Where(x => ids.Contains(x.MinisterioId))
                .Join(_db.Usuarios,
                    x => x.UsuarioId,
                    u => u.Id,
                    (x, u) => new
                    {
                        x.MinisterioId,
                        x.UsuarioId,
                        UsuarioNome = u.Nome,
                        x.Principal
                    })
                .ToListAsync();

            var voluntarios = await _db.MinisteriosVoluntarios
                .AsNoTracking()
                .Where(x => ids.Contains(x.MinisterioId))
                .Join(_db.Voluntarios,
                    x => x.VoluntarioId,
                    v => v.Id,
                    (x, v) => new
                    {
                        x.MinisterioId,
                        x.VoluntarioId,
                        VoluntarioNome = v.Nome,
                        x.Principal
                })
                .ToListAsync();

            var funcoesPadrao = await _db.MinisteriosFuncoesPadrao
                .AsNoTracking()
                .Where(x => ids.Contains(x.MinisterioId))
                .OrderBy(x => x.MinisterioId)
                .ThenBy(x => x.Ordem)
                .ThenBy(x => x.Nome)
                .Select(x => new
                {
                    x.Id,
                    x.MinisterioId,
                    x.Nome,
                    x.BlocoCronograma,
                    x.Ordem,
                    x.Ativo
                })
                .ToListAsync();

            var resposta = listaBase.Select(item => new
            {
                item.Id,
                item.Nome,
                item.Codigo,
                item.Descricao,
                item.Ativo,
                Lideres = lideres.Where(x => x.MinisterioId == item.Id).OrderByDescending(x => x.Principal).ThenBy(x => x.UsuarioNome),
                Voluntarios = voluntarios.Where(x => x.MinisterioId == item.Id).OrderByDescending(x => x.Principal).ThenBy(x => x.VoluntarioNome),
                FuncoesPadrao = funcoesPadrao.Where(x => x.MinisterioId == item.Id).OrderBy(x => x.Ordem).ThenBy(x => x.Nome)
            });

            return Ok(resposta);
        }

        [HttpGet("opcoes")]
        public async Task<IActionResult> OpcoesCadastro()
        {
            var usuarios = await _db.Usuarios
                .AsNoTracking()
                .Where(x => x.Ativo)
                .OrderBy(x => x.Nome)
                .Select(x => new { x.Id, x.Nome, x.Email })
                .ToListAsync();

            var voluntarios = await _db.Voluntarios
                .AsNoTracking()
                .Where(x => x.Ativo)
                .OrderBy(x => x.Nome)
                .Select(x => new { x.Id, x.Nome, x.Email })
                .ToListAsync();

            return Ok(new { usuarios, voluntarios });
        }

        [HttpPost]
        [Authorize(Roles = "ADMIN,GESTAO_CULTO")]
        public async Task<IActionResult> Criar([FromBody] MinisterioRequest dto)
        {
            var entity = new Ministerio
            {
                Nome = dto.Nome,
                Codigo = GerarCodigo(dto.Nome),
                Descricao = dto.Descricao,
                Ativo = dto.Ativo,
                CriadoEm = DateTime.UtcNow
            };

            _db.Ministerios.Add(entity);
            await _db.SaveChangesAsync();

            await SalvarVinculos(entity.Id, dto.Lideres, dto.VoluntarioIds, dto.FuncoesPadrao);
            return Ok(new { mensagem = "Ministério criado com sucesso.", id = entity.Id });
        }

        [HttpPut("{id:long}")]
        [Authorize(Roles = "ADMIN,GESTAO_CULTO")]
        public async Task<IActionResult> Atualizar(long id, [FromBody] MinisterioRequest dto)
        {
            var entity = await _db.Ministerios.FirstOrDefaultAsync(x => x.Id == id);
            if (entity == null)
            {
                return NotFound(new { mensagem = "Ministério não encontrado." });
            }

            entity.Nome = dto.Nome;
            entity.Codigo = GerarCodigo(dto.Nome);
            entity.Descricao = dto.Descricao;
            entity.Ativo = dto.Ativo;
            entity.AtualizadoEm = DateTime.UtcNow;

            var lideresAtuais = await _db.MinisteriosLideres.Where(x => x.MinisterioId == id).ToListAsync();
            var voluntariosAtuais = await _db.MinisteriosVoluntarios.Where(x => x.MinisterioId == id).ToListAsync();
            var funcoesAtuais = await _db.MinisteriosFuncoesPadrao.Where(x => x.MinisterioId == id).ToListAsync();
            if (lideresAtuais.Any())
            {
                _db.MinisteriosLideres.RemoveRange(lideresAtuais);
            }

            if (voluntariosAtuais.Any())
            {
                _db.MinisteriosVoluntarios.RemoveRange(voluntariosAtuais);
            }

            if (funcoesAtuais.Any())
            {
                _db.MinisteriosFuncoesPadrao.RemoveRange(funcoesAtuais);
            }

            await _db.SaveChangesAsync();
            await SalvarVinculos(id, dto.Lideres, dto.VoluntarioIds, dto.FuncoesPadrao);

            return Ok(new { mensagem = "Ministério atualizado com sucesso." });
        }

        [HttpDelete("{id:long}")]
        [Authorize(Roles = "ADMIN,GESTAO_CULTO")]
        public async Task<IActionResult> Excluir(long id)
        {
            var entity = await _db.Ministerios.FirstOrDefaultAsync(x => x.Id == id);
            if (entity == null)
            {
                return NotFound(new { mensagem = "Ministério não encontrado." });
            }

            var lideresAtuais = await _db.MinisteriosLideres.Where(x => x.MinisterioId == id).ToListAsync();
            var voluntariosAtuais = await _db.MinisteriosVoluntarios.Where(x => x.MinisterioId == id).ToListAsync();
            if (lideresAtuais.Any())
            {
                _db.MinisteriosLideres.RemoveRange(lideresAtuais);
            }

            if (voluntariosAtuais.Any())
            {
                _db.MinisteriosVoluntarios.RemoveRange(voluntariosAtuais);
            }

            _db.Ministerios.Remove(entity);
            await _db.SaveChangesAsync();
            return Ok(new { mensagem = "Ministério excluído com sucesso." });
        }

        private async Task SalvarVinculos(long ministerioId, List<MinisterioLiderRequest> lideres, List<long> voluntarioIds, List<MinisterioFuncaoPadraoRequest> funcoesPadrao)
        {
            var lideresNormalizados = (lideres ?? new List<MinisterioLiderRequest>())
                .Where(x => x.UsuarioId > 0)
                .GroupBy(x => x.UsuarioId)
                .Select(g => new MinisterioLiderRequest
                {
                    UsuarioId = g.Key,
                    Principal = g.Any(l => l.Principal)
                })
                .ToList();

            if (lideresNormalizados.Any() && !lideresNormalizados.Any(x => x.Principal))
            {
                lideresNormalizados[0].Principal = true;
            }

            foreach (var lider in lideresNormalizados)
            {
                _db.MinisteriosLideres.Add(new MinisterioLider
                {
                    MinisterioId = ministerioId,
                    UsuarioId = lider.UsuarioId,
                    Principal = lider.Principal,
                    CriadoEm = DateTime.UtcNow
                });
            }

            var voluntariosNormalizados = (voluntarioIds ?? new List<long>())
                .Where(x => x > 0)
                .Distinct()
                .ToList();

            for (var i = 0; i < voluntariosNormalizados.Count; i++)
            {
                _db.MinisteriosVoluntarios.Add(new MinisterioVoluntario
                {
                    MinisterioId = ministerioId,
                    VoluntarioId = voluntariosNormalizados[i],
                    Principal = i == 0,
                    CriadoEm = DateTime.UtcNow
                });
            }

            var funcoesNormalizadas = (funcoesPadrao ?? new List<MinisterioFuncaoPadraoRequest>())
                .Select((item, index) => new
                {
                    Nome = (item.Nome ?? string.Empty).Trim(),
                    BlocoCronograma = NormalizarBlocoCronograma(item.BlocoCronograma),
                    Ordem = item.Ordem ?? index + 1,
                    Ativo = item.Ativo
                })
                .Where(x => !string.IsNullOrWhiteSpace(x.Nome))
                .GroupBy(x => x.Nome, StringComparer.OrdinalIgnoreCase)
                .Select(g => g.OrderBy(x => x.Ordem).First())
                .OrderBy(x => x.Ordem)
                .ThenBy(x => x.Nome)
                .ToList();

            for (var i = 0; i < funcoesNormalizadas.Count; i++)
            {
                var funcao = funcoesNormalizadas[i];
                _db.MinisteriosFuncoesPadrao.Add(new MinisterioFuncaoPadrao
                {
                    MinisterioId = ministerioId,
                    Nome = funcao.Nome,
                    BlocoCronograma = funcao.BlocoCronograma,
                    Ordem = funcao.Ordem > 0 ? funcao.Ordem : i + 1,
                    Ativo = funcao.Ativo,
                    CriadoEm = DateTime.UtcNow
                });
            }

            await _db.SaveChangesAsync();
        }

        private static string GerarCodigo(string nome)
        {
            if (string.IsNullOrWhiteSpace(nome))
            {
                return "MINISTERIO";
            }

            return nome
                .Trim()
                .ToUpperInvariant()
                .Replace(" ", "_")
                .Replace("-", "_");
        }

        private static string NormalizarBlocoCronograma(string? valor)
        {
            var texto = (valor ?? string.Empty).Trim().ToUpperInvariant();
            return texto switch
            {
                "PRINCIPAL" => "PRINCIPAL",
                "LOUNGE" => "LOUNGE",
                "ADICIONAL" => "ADICIONAL",
                "SOMENTE_EQUIPE" => "SOMENTE_EQUIPE",
                _ => "SOMENTE_EQUIPE"
            };
        }
    }
}
