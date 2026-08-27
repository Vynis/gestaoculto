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
    public class TemplateCultoRequest
    {
        public string Nome { get; set; } = string.Empty;
        public string TipoCulto { get; set; } = string.Empty;
        public string? Descricao { get; set; }
        public bool Ativo { get; set; } = true;
        public List<TemplateEtapaRequest> Etapas { get; set; } = new List<TemplateEtapaRequest>();
    }

    public class TemplateEtapaRequest
    {
        public int Sequencia { get; set; }
        public string? HorarioInicialPadrao { get; set; }
        public int DuracaoMinutos { get; set; }
        public string Atividade { get; set; } = string.Empty;
        public string? BlocoCronograma { get; set; }
        public string? Descricao { get; set; }
        public long? MinisterioResponsavelId { get; set; }
        public long[] MinisterioIds { get; set; } = Array.Empty<long>();
        public string? Observacoes { get; set; }
        public long? StatusEtapaId { get; set; }
        public TemplateEtapaMinisterioAcaoRequest[] AcoesMinisterio { get; set; } = Array.Empty<TemplateEtapaMinisterioAcaoRequest>();
    }

    public class TemplateEtapaMinisterioAcaoRequest
    {
        public long? Id { get; set; }
        public long MinisterioId { get; set; }
        public int? Ordem { get; set; }
        public string DescricaoAcao { get; set; } = string.Empty;
        public string? Observacao { get; set; }
        public bool Ativo { get; set; } = true;
    }

    [ApiController]
    [Authorize]
    [Route("api/templates-culto")]
    public class TemplatesCultoController : ControllerBase
    {
        private readonly GestaoCultoDbContext _db;

        public TemplatesCultoController(GestaoCultoDbContext db)
        {
            _db = db;
        }

        [HttpGet]
        public async Task<IActionResult> Listar()
        {
            var templates = await _db.TemplatesCulto
                .AsNoTracking()
                .OrderBy(x => x.Nome)
                .Select(x => new
                {
                    x.Id,
                    x.Nome,
                    x.TipoCulto,
                    x.Descricao,
                    x.Ativo
                })
                .ToListAsync();

            var ids = templates.Select(x => (long)x.Id).ToList();
            var etapas = await _db.TemplatesEtapasCulto
                .AsNoTracking()
                .Where(x => ids.Contains(x.TemplateCultoId))
                .OrderBy(x => x.TemplateCultoId)
                .ThenBy(x => x.Sequencia)
                .Select(x => new
                {
                    x.TemplateCultoId,
                    x.Id,
                    x.Sequencia,
                    HorarioInicialPadrao = x.HorarioInicialPadrao.HasValue ? x.HorarioInicialPadrao.Value.ToString() : null,
                    x.DuracaoMinutos,
                    x.Atividade,
                    x.BlocoCronograma,
                    x.Descricao,
                    x.MinisterioResponsavelId,
                    x.Observacoes,
                    x.StatusEtapaId
                })
                .ToListAsync();

            var etapaIds = etapas.Select(x => (long)x.Id).ToList();
            var ministeriosEtapa = await _db.TemplatesEtapasCultoMinisterios
                .AsNoTracking()
                .Where(x => etapaIds.Contains(x.TemplateEtapaCultoId))
                .OrderBy(x => x.TemplateEtapaCultoId)
                .ThenBy(x => x.MinisterioId)
                .Select(x => new
                {
                    x.TemplateEtapaCultoId,
                    x.MinisterioId,
                    MinisterioNome = _db.Ministerios.Where(m => m.Id == x.MinisterioId).Select(m => m.Nome).FirstOrDefault()
                })
                .ToListAsync();

            var acoes = await _db.TemplatesEtapasCultoMinisteriosAcoes
                .AsNoTracking()
                .Where(x => etapaIds.Contains(x.TemplateEtapaCultoId))
                .OrderBy(x => x.TemplateEtapaCultoId)
                .ThenBy(x => x.Ordem ?? int.MaxValue)
                .ThenBy(x => x.Id)
                .Select(x => new
                {
                    x.Id,
                    x.TemplateEtapaCultoId,
                    x.MinisterioId,
                    MinisterioNome = _db.Ministerios.Where(m => m.Id == x.MinisterioId).Select(m => m.Nome).FirstOrDefault(),
                    x.Ordem,
                    x.DescricaoAcao,
                    x.Observacao,
                    x.Ativo
                })
                .ToListAsync();

            var mapaAcoes = acoes
                .GroupBy(x => x.TemplateEtapaCultoId)
                .ToDictionary(g => g.Key, g => (object)g.ToList());

            var mapaMinisteriosIds = ministeriosEtapa
                .GroupBy(x => x.TemplateEtapaCultoId)
                .ToDictionary(g => g.Key, g => (object)g.Select(x => x.MinisterioId).Distinct().ToList());

            var mapaMinisterios = ministeriosEtapa
                .GroupBy(x => x.TemplateEtapaCultoId)
                .ToDictionary(g => g.Key, g => (object)g.Select(x => new { x.MinisterioId, x.MinisterioNome }).Distinct().ToList());

            var etapasComAcoes = etapas.Select(x => new
            {
                x.TemplateCultoId,
                x.Id,
                x.Sequencia,
                x.HorarioInicialPadrao,
                x.DuracaoMinutos,
                x.Atividade,
                x.BlocoCronograma,
                x.Descricao,
                x.MinisterioResponsavelId,
                MinisterioIds = mapaMinisteriosIds.ContainsKey(x.Id) ? mapaMinisteriosIds[x.Id] : Array.Empty<long>(),
                Ministerios = mapaMinisterios.ContainsKey(x.Id) ? mapaMinisterios[x.Id] : Array.Empty<object>(),
                x.Observacoes,
                x.StatusEtapaId,
                AcoesMinisterio = mapaAcoes.ContainsKey(x.Id) ? mapaAcoes[x.Id] : Array.Empty<object>()
            }).ToList();

            var mapaEtapas = etapasComAcoes
                .GroupBy(x => x.TemplateCultoId)
                .ToDictionary(g => g.Key, g => g.ToList());

            var lista = templates.Select(x => new
            {
                x.Id,
                x.Nome,
                x.TipoCulto,
                x.Descricao,
                x.Ativo,
                Etapas = mapaEtapas.ContainsKey(x.Id) ? (object)mapaEtapas[x.Id] : Array.Empty<object>()
            });

            return Ok(lista);
        }

        [HttpPost]
        [Authorize(Roles = "ADMIN,GESTAO_CULTO")]
        public async Task<IActionResult> Criar([FromBody] TemplateCultoRequest dto)
        {
            var entity = new TemplateCulto
            {
                Nome = dto.Nome,
                TipoCulto = dto.TipoCulto,
                Descricao = dto.Descricao,
                Ativo = dto.Ativo,
                CriadoEm = DateTime.UtcNow
            };

            _db.TemplatesCulto.Add(entity);
            await _db.SaveChangesAsync();

            await SalvarEtapasTemplate(entity.Id, dto.Etapas);

            return Ok(await MontarTemplateCompleto(entity.Id));
        }

        [HttpPut("{id:long}")]
        [Authorize(Roles = "ADMIN,GESTAO_CULTO")]
        public async Task<IActionResult> Atualizar(long id, [FromBody] TemplateCultoRequest dto)
        {
            var entity = await _db.TemplatesCulto.FirstOrDefaultAsync(x => x.Id == id);
            if (entity == null)
            {
                return NotFound(new { mensagem = "Template não encontrado." });
            }

            entity.Nome = dto.Nome;
            entity.TipoCulto = dto.TipoCulto;
            entity.Descricao = dto.Descricao;
            entity.Ativo = dto.Ativo;
            entity.AtualizadoEm = DateTime.UtcNow;

            await _db.SaveChangesAsync();

            var etapasAtuais = await _db.TemplatesEtapasCulto
                .Where(x => x.TemplateCultoId == entity.Id)
                .ToListAsync();

            if (etapasAtuais.Any())
            {
                _db.TemplatesEtapasCulto.RemoveRange(etapasAtuais);
                await _db.SaveChangesAsync();
            }

            await SalvarEtapasTemplate(entity.Id, dto.Etapas);

            return Ok(await MontarTemplateCompleto(entity.Id));
        }

        [HttpDelete("{id:long}")]
        [Authorize(Roles = "ADMIN,GESTAO_CULTO")]
        public async Task<IActionResult> Excluir(long id)
        {
            var entity = await _db.TemplatesCulto.FirstOrDefaultAsync(x => x.Id == id);
            if (entity == null)
            {
                return NotFound(new { mensagem = "Template não encontrado." });
            }

            var etapas = await _db.TemplatesEtapasCulto
                .Where(x => x.TemplateCultoId == entity.Id)
                .ToListAsync();

            if (etapas.Any())
            {
                _db.TemplatesEtapasCulto.RemoveRange(etapas);
            }

            _db.TemplatesCulto.Remove(entity);
            await _db.SaveChangesAsync();
            return Ok(new { mensagem = "Template excluído com sucesso." });
        }

        private async Task SalvarEtapasTemplate(long templateId, List<TemplateEtapaRequest> etapas)
        {
            var normalizadas = (etapas ?? new List<TemplateEtapaRequest>())
                .Where(x => !string.IsNullOrWhiteSpace(x.Atividade) && x.DuracaoMinutos > 0)
                .OrderBy(x => x.Sequencia <= 0 ? int.MaxValue : x.Sequencia)
                .ToList();

            if (!normalizadas.Any())
            {
                return;
            }

            for (var i = 0; i < normalizadas.Count; i++)
            {
                var item = normalizadas[i];
                var horario = ParseHorarioOpcional(item.HorarioInicialPadrao);
                var ministerioIds = (item.MinisterioIds ?? Array.Empty<long>())
                    .Where(x => x > 0)
                    .Distinct()
                    .ToList();

                var ministeriosNasAcoes = (item.AcoesMinisterio ?? Array.Empty<TemplateEtapaMinisterioAcaoRequest>())
                    .Where(x => x.MinisterioId > 0)
                    .Select(x => x.MinisterioId)
                    .Distinct()
                    .ToList();

                foreach (var ministerioAcao in ministeriosNasAcoes)
                {
                    if (!ministerioIds.Contains(ministerioAcao))
                    {
                        ministerioIds.Add(ministerioAcao);
                    }
                }

                if (item.MinisterioResponsavelId.HasValue && item.MinisterioResponsavelId.Value > 0 && !ministerioIds.Contains(item.MinisterioResponsavelId.Value))
                {
                    ministerioIds.Insert(0, item.MinisterioResponsavelId.Value);
                }

                var entity = new TemplateEtapaCulto
                {
                    TemplateCultoId = templateId,
                    Sequencia = i + 1,
                    HorarioInicialPadrao = horario,
                    DuracaoMinutos = item.DuracaoMinutos,
                    Atividade = item.Atividade,
                    BlocoCronograma = NormalizarBlocoCronograma(item.BlocoCronograma),
                    Descricao = item.Descricao,
                    MinisterioResponsavelId = ministerioIds.Any() ? ministerioIds.First() : item.MinisterioResponsavelId,
                    Observacoes = item.Observacoes,
                    StatusEtapaId = item.StatusEtapaId,
                    CriadoEm = DateTime.UtcNow
                };

                _db.TemplatesEtapasCulto.Add(entity);
                await _db.SaveChangesAsync();
                await SalvarMinisteriosEtapaTemplate(entity.Id, ministerioIds);
                await SalvarAcoesEtapaTemplate(entity.Id, item.AcoesMinisterio);
            }
        }

        private async Task<object> MontarTemplateCompleto(long id)
        {
            var template = await _db.TemplatesCulto
                .AsNoTracking()
                .Where(x => x.Id == id)
                .Select(x => new
                {
                    x.Id,
                    x.Nome,
                    x.TipoCulto,
                    x.Descricao,
                    x.Ativo
                })
                .FirstAsync();

            var etapas = await _db.TemplatesEtapasCulto
                .AsNoTracking()
                .Where(x => x.TemplateCultoId == id)
                .OrderBy(x => x.Sequencia)
                .Select(x => new
                {
                    x.Id,
                    x.Sequencia,
                    HorarioInicialPadrao = x.HorarioInicialPadrao.HasValue ? x.HorarioInicialPadrao.Value.ToString() : null,
                    x.DuracaoMinutos,
                    x.Atividade,
                    x.BlocoCronograma,
                    x.Descricao,
                    x.MinisterioResponsavelId,
                    x.Observacoes,
                    x.StatusEtapaId
                })
                .ToListAsync();

            var etapaIds = etapas.Select(x => (long)x.Id).ToList();
            var ministeriosEtapa = await _db.TemplatesEtapasCultoMinisterios
                .AsNoTracking()
                .Where(x => etapaIds.Contains(x.TemplateEtapaCultoId))
                .OrderBy(x => x.TemplateEtapaCultoId)
                .ThenBy(x => x.MinisterioId)
                .Select(x => new
                {
                    x.TemplateEtapaCultoId,
                    x.MinisterioId,
                    MinisterioNome = _db.Ministerios.Where(m => m.Id == x.MinisterioId).Select(m => m.Nome).FirstOrDefault()
                })
                .ToListAsync();

            var acoes = await _db.TemplatesEtapasCultoMinisteriosAcoes
                .AsNoTracking()
                .Where(x => etapaIds.Contains(x.TemplateEtapaCultoId))
                .OrderBy(x => x.TemplateEtapaCultoId)
                .ThenBy(x => x.Ordem ?? int.MaxValue)
                .ThenBy(x => x.Id)
                .Select(x => new
                {
                    x.Id,
                    x.TemplateEtapaCultoId,
                    x.MinisterioId,
                    MinisterioNome = _db.Ministerios.Where(m => m.Id == x.MinisterioId).Select(m => m.Nome).FirstOrDefault(),
                    x.Ordem,
                    x.DescricaoAcao,
                    x.Observacao,
                    x.Ativo
                })
                .ToListAsync();

            var mapaAcoes = acoes
                .GroupBy(x => x.TemplateEtapaCultoId)
                .ToDictionary(g => g.Key, g => (object)g.ToList());

            var mapaMinisteriosIds = ministeriosEtapa
                .GroupBy(x => x.TemplateEtapaCultoId)
                .ToDictionary(g => g.Key, g => (object)g.Select(x => x.MinisterioId).Distinct().ToList());

            var mapaMinisterios = ministeriosEtapa
                .GroupBy(x => x.TemplateEtapaCultoId)
                .ToDictionary(g => g.Key, g => (object)g.Select(x => new { x.MinisterioId, x.MinisterioNome }).Distinct().ToList());

            var etapasComAcoes = etapas.Select(x => new
            {
                x.Id,
                x.Sequencia,
                x.HorarioInicialPadrao,
                x.DuracaoMinutos,
                x.Atividade,
                x.BlocoCronograma,
                x.Descricao,
                x.MinisterioResponsavelId,
                MinisterioIds = mapaMinisteriosIds.ContainsKey(x.Id) ? mapaMinisteriosIds[x.Id] : Array.Empty<long>(),
                Ministerios = mapaMinisterios.ContainsKey(x.Id) ? mapaMinisterios[x.Id] : Array.Empty<object>(),
                x.Observacoes,
                x.StatusEtapaId,
                AcoesMinisterio = mapaAcoes.ContainsKey(x.Id) ? mapaAcoes[x.Id] : Array.Empty<object>()
            }).ToList();

            return new
            {
                template.Id,
                template.Nome,
                template.TipoCulto,
                template.Descricao,
                template.Ativo,
                Etapas = etapasComAcoes
            };
        }

        private async Task SalvarAcoesEtapaTemplate(long templateEtapaId, TemplateEtapaMinisterioAcaoRequest[] itens)
        {
            var normalizadas = (itens ?? Array.Empty<TemplateEtapaMinisterioAcaoRequest>())
                .Where(x => x.MinisterioId > 0 && !string.IsNullOrWhiteSpace(x.DescricaoAcao))
                .Select((x, index) => new TemplateEtapaCultoMinisterioAcao
                {
                    TemplateEtapaCultoId = templateEtapaId,
                    MinisterioId = x.MinisterioId,
                    Ordem = x.Ordem ?? (index + 1),
                    DescricaoAcao = x.DescricaoAcao.Trim(),
                    Observacao = string.IsNullOrWhiteSpace(x.Observacao) ? null : x.Observacao.Trim(),
                    Ativo = x.Ativo,
                    CriadoEm = DateTime.UtcNow
                })
                .ToList();

            if (!normalizadas.Any())
            {
                return;
            }

            _db.TemplatesEtapasCultoMinisteriosAcoes.AddRange(normalizadas);
            await _db.SaveChangesAsync();
        }

        private async Task SalvarMinisteriosEtapaTemplate(long templateEtapaId, IList<long> ministerioIds)
        {
            var ids = (ministerioIds ?? Array.Empty<long>())
                .Where(x => x > 0)
                .Distinct()
                .ToList();

            if (!ids.Any())
            {
                return;
            }

            var idsValidos = await _db.Ministerios
                .AsNoTracking()
                .Where(x => ids.Contains(x.Id) && x.Ativo)
                .Select(x => x.Id)
                .ToListAsync();

            if (!idsValidos.Any())
            {
                return;
            }

            var vinculos = idsValidos.Select(id => new TemplateEtapaCultoMinisterio
            {
                TemplateEtapaCultoId = templateEtapaId,
                MinisterioId = id
            }).ToList();

            _db.TemplatesEtapasCultoMinisterios.AddRange(vinculos);
            await _db.SaveChangesAsync();
        }

        private static TimeSpan? ParseHorarioOpcional(string? valor)
        {
            if (string.IsNullOrWhiteSpace(valor))
            {
                return null;
            }

            if (TimeSpan.TryParse(valor, out var horario))
            {
                return horario;
            }

            return null;
        }

        private static string NormalizarBlocoCronograma(string? valor)
        {
            var texto = (valor ?? string.Empty).Trim();
            return string.IsNullOrWhiteSpace(texto) ? "PRINCIPAL" : texto;
        }
    }
}
