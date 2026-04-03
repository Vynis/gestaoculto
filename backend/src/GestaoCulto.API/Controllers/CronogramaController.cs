using System;
using System.Linq;
using System.Threading.Tasks;
using GestaoCulto.Domain.Entities;
using GestaoCulto.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GestaoCulto.API.Controllers
{
    public class EtapaRequest
    {
        public long CultoId { get; set; }
        public int Sequencia { get; set; }
        public DateTime HorarioInicio { get; set; }
        public int DuracaoMinutos { get; set; }
        public string Atividade { get; set; } = string.Empty;
        public string? Descricao { get; set; }
        public long? MinisterioResponsavelId { get; set; }
        public long StatusEtapaId { get; set; }
        public EtapaMinisterioAcaoRequest[] AcoesMinisterio { get; set; } = Array.Empty<EtapaMinisterioAcaoRequest>();
    }

    public class EtapaMinisterioAcaoRequest
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
    [Route("api/cronograma")]
    public class CronogramaController : ControllerBase
    {
        private readonly GestaoCultoDbContext _db;

        public CronogramaController(GestaoCultoDbContext db)
        {
            _db = db;
        }

        [HttpGet("culto/{cultoId:long}")]
        public async Task<IActionResult> ListarPorCulto(long cultoId)
        {
            var etapas = await _db.EtapasCulto
                .AsNoTracking()
                .Where(e => e.CultoId == cultoId)
                .OrderBy(e => e.Sequencia)
                .Select(e => new
                {
                    e.Id,
                    e.CultoId,
                    e.Sequencia,
                    e.HorarioInicio,
                    e.DuracaoMinutos,
                    e.HorarioFimCalculado,
                    e.Atividade,
                    e.Descricao,
                    e.MinisterioResponsavelId,
                    MinisterioResponsavelNome = _db.Ministerios.Where(m => m.Id == e.MinisterioResponsavelId).Select(m => m.Nome).FirstOrDefault(),
                    e.AtrasoMinutos,
                    e.StatusEtapaId
                })
                .ToListAsync();

            etapas = etapas
                .GroupBy(x => new
                {
                    x.CultoId,
                    x.Sequencia,
                    Atividade = (x.Atividade ?? string.Empty).Trim().ToLower()
                })
                .Select(g => g.OrderBy(x => x.Id).First())
                .OrderBy(x => x.Sequencia)
                .ThenBy(x => x.Id)
                .ToList();

            var etapaIds = etapas.Select(x => (long)x.Id).ToList();
            var acoes = await _db.EtapasCultoMinisteriosAcoes
                .AsNoTracking()
                .Where(x => etapaIds.Contains(x.EtapaCultoId))
                .OrderBy(x => x.EtapaCultoId)
                .ThenBy(x => x.Ordem ?? int.MaxValue)
                .ThenBy(x => x.Id)
                .Select(x => new
                {
                    x.Id,
                    x.EtapaCultoId,
                    x.MinisterioId,
                    MinisterioNome = _db.Ministerios.Where(m => m.Id == x.MinisterioId).Select(m => m.Nome).FirstOrDefault(),
                    x.Ordem,
                    x.DescricaoAcao,
                    x.Observacao,
                    x.Ativo
                })
                .ToListAsync();

            var mapaAcoes = acoes
                .GroupBy(x => x.EtapaCultoId)
                .ToDictionary(g => g.Key, g => (object)g.ToList());

            var resposta = etapas.Select(x => new
            {
                x.Id,
                x.CultoId,
                x.Sequencia,
                x.HorarioInicio,
                x.DuracaoMinutos,
                x.HorarioFimCalculado,
                x.Atividade,
                x.Descricao,
                x.MinisterioResponsavelId,
                x.MinisterioResponsavelNome,
                x.AtrasoMinutos,
                x.StatusEtapaId,
                AcoesMinisterio = mapaAcoes.ContainsKey(x.Id) ? mapaAcoes[x.Id] : Array.Empty<object>()
            });

            return Ok(resposta);
        }

        [HttpPost]
        [Authorize(Roles = "ADMIN,GESTAO_CULTO")]
        public async Task<IActionResult> Criar([FromBody] EtapaRequest dto)
        {
            var culto = await _db.Cultos
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == dto.CultoId);

            if (culto == null)
            {
                return BadRequest(new { mensagem = "Culto inválido para cadastro da etapa." });
            }

            var horarioInicio = dto.HorarioInicio;
            if (horarioInicio.Year < 1000)
            {
                horarioInicio = culto.DataCulto.Date.Add(horarioInicio.TimeOfDay);
            }

            var entity = new EtapaCulto
            {
                CultoId = dto.CultoId,
                Sequencia = dto.Sequencia,
                HorarioInicio = horarioInicio,
                DuracaoMinutos = dto.DuracaoMinutos,
                HorarioFimCalculado = horarioInicio.AddMinutes(dto.DuracaoMinutos),
                Atividade = dto.Atividade,
                Descricao = dto.Descricao,
                MinisterioResponsavelId = dto.MinisterioResponsavelId,
                StatusEtapaId = dto.StatusEtapaId,
                CriadoEm = DateTime.UtcNow
            };

            _db.EtapasCulto.Add(entity);
            await _db.SaveChangesAsync();
            await SalvarAcoesMinisterio(entity.Id, dto.AcoesMinisterio, false);
            return Ok(await MontarEtapaResposta(entity.Id));
        }

        [HttpPut("{id:long}")]
        [Authorize(Roles = "ADMIN,GESTAO_CULTO")]
        public async Task<IActionResult> Atualizar(long id, [FromBody] EtapaRequest dto)
        {
            var entity = await _db.EtapasCulto.FirstOrDefaultAsync(x => x.Id == id);
            if (entity == null)
            {
                return NotFound(new { mensagem = "Etapa não encontrada." });
            }

            var culto = await _db.Cultos
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == dto.CultoId);

            if (culto == null)
            {
                return BadRequest(new { mensagem = "Culto inválido para atualização da etapa." });
            }

            var horarioInicio = dto.HorarioInicio;
            if (horarioInicio.Year < 1000)
            {
                horarioInicio = culto.DataCulto.Date.Add(horarioInicio.TimeOfDay);
            }

            entity.CultoId = dto.CultoId;
            entity.Sequencia = dto.Sequencia;
            entity.HorarioInicio = horarioInicio;
            entity.DuracaoMinutos = dto.DuracaoMinutos;
            entity.HorarioFimCalculado = horarioInicio.AddMinutes(dto.DuracaoMinutos);
            entity.Atividade = dto.Atividade;
            entity.Descricao = dto.Descricao;
            entity.MinisterioResponsavelId = dto.MinisterioResponsavelId;
            entity.StatusEtapaId = dto.StatusEtapaId;
            entity.AtualizadoEm = DateTime.UtcNow;

            await _db.SaveChangesAsync();
            await SalvarAcoesMinisterio(entity.Id, dto.AcoesMinisterio, true);
            return Ok(await MontarEtapaResposta(entity.Id));
        }

        [HttpDelete("{id:long}")]
        [Authorize(Roles = "ADMIN,GESTAO_CULTO")]
        public async Task<IActionResult> Excluir(long id)
        {
            var entity = await _db.EtapasCulto.FirstOrDefaultAsync(x => x.Id == id);
            if (entity == null)
            {
                return NotFound(new { mensagem = "Etapa não encontrada." });
            }

            _db.EtapasCulto.Remove(entity);
            await _db.SaveChangesAsync();
            return Ok(new { mensagem = "Etapa excluída com sucesso." });
        }

        [HttpDelete("culto/{cultoId:long}")]
        [Authorize(Roles = "ADMIN,GESTAO_CULTO")]
        public async Task<IActionResult> ExcluirTodasDoCulto(long cultoId)
        {
            var etapas = await _db.EtapasCulto.Where(x => x.CultoId == cultoId).ToListAsync();
            if (!etapas.Any())
            {
                return Ok(new { mensagem = "Nenhuma etapa encontrada para este culto.", removidas = 0 });
            }

            _db.EtapasCulto.RemoveRange(etapas);
            await _db.SaveChangesAsync();

            return Ok(new { mensagem = "Todas as etapas do culto foram excluídas com sucesso.", removidas = etapas.Count });
        }

        private async Task SalvarAcoesMinisterio(long etapaId, EtapaMinisterioAcaoRequest[] itens, bool limparAntes)
        {
            if (limparAntes)
            {
                var antigas = await _db.EtapasCultoMinisteriosAcoes
                    .Where(x => x.EtapaCultoId == etapaId)
                    .ToListAsync();

                if (antigas.Any())
                {
                    _db.EtapasCultoMinisteriosAcoes.RemoveRange(antigas);
                    await _db.SaveChangesAsync();
                }
            }

            var normalizadas = (itens ?? Array.Empty<EtapaMinisterioAcaoRequest>())
                .Where(x => x.MinisterioId > 0 && !string.IsNullOrWhiteSpace(x.DescricaoAcao))
                .Select((x, index) => new EtapaCultoMinisterioAcao
                {
                    EtapaCultoId = etapaId,
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

            _db.EtapasCultoMinisteriosAcoes.AddRange(normalizadas);
            await _db.SaveChangesAsync();
        }

        private async Task<object> MontarEtapaResposta(long etapaId)
        {
            var etapa = await _db.EtapasCulto
                .AsNoTracking()
                .Where(e => e.Id == etapaId)
                .Select(e => new
                {
                    e.Id,
                    e.CultoId,
                    e.Sequencia,
                    e.HorarioInicio,
                    e.DuracaoMinutos,
                    e.HorarioFimCalculado,
                    e.Atividade,
                    e.Descricao,
                    e.MinisterioResponsavelId,
                    MinisterioResponsavelNome = _db.Ministerios.Where(m => m.Id == e.MinisterioResponsavelId).Select(m => m.Nome).FirstOrDefault(),
                    e.AtrasoMinutos,
                    e.StatusEtapaId
                })
                .FirstAsync();

            var acoes = await _db.EtapasCultoMinisteriosAcoes
                .AsNoTracking()
                .Where(x => x.EtapaCultoId == etapaId)
                .OrderBy(x => x.Ordem ?? int.MaxValue)
                .ThenBy(x => x.Id)
                .Select(x => new
                {
                    x.Id,
                    x.EtapaCultoId,
                    x.MinisterioId,
                    MinisterioNome = _db.Ministerios.Where(m => m.Id == x.MinisterioId).Select(m => m.Nome).FirstOrDefault(),
                    x.Ordem,
                    x.DescricaoAcao,
                    x.Observacao,
                    x.Ativo
                })
                .ToListAsync();

            return new
            {
                etapa.Id,
                etapa.CultoId,
                etapa.Sequencia,
                etapa.HorarioInicio,
                etapa.DuracaoMinutos,
                etapa.HorarioFimCalculado,
                etapa.Atividade,
                etapa.Descricao,
                etapa.MinisterioResponsavelId,
                etapa.MinisterioResponsavelNome,
                etapa.AtrasoMinutos,
                etapa.StatusEtapaId,
                AcoesMinisterio = acoes
            };
        }
    }
}
