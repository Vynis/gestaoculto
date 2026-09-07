using System;
using System.Collections.Generic;
using System.Data;
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
        public string? BlocoCronograma { get; set; }
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

    public class CronogramaBlocoOrdemRequest
    {
        public string? BlocoCronograma { get; set; }
        public long[] EtapaIds { get; set; } = Array.Empty<long>();
    }

    public class CronogramaReordenarRequest
    {
        public CronogramaBlocoOrdemRequest[] Blocos { get; set; } = Array.Empty<CronogramaBlocoOrdemRequest>();
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
                    e.BlocoCronograma,
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
                    BlocoCronograma = (x.BlocoCronograma ?? "PRINCIPAL").Trim().ToLower(),
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
                    x.BlocoCronograma,
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

            var blocoCronograma = NormalizarBlocoCronograma(dto.BlocoCronograma);
            var conflitoCriacao = await ExisteConflitoSequenciaPorBloco(dto.CultoId, dto.Sequencia, blocoCronograma, null);
            if (conflitoCriacao)
            {
                return BadRequest(new { mensagem = $"Já existe uma etapa no bloco '{blocoCronograma}' com a sequência {dto.Sequencia}." });
            }

            var entity = new EtapaCulto
            {
                CultoId = dto.CultoId,
                Sequencia = dto.Sequencia,
                HorarioInicio = horarioInicio,
                DuracaoMinutos = dto.DuracaoMinutos,
                HorarioFimCalculado = horarioInicio.AddMinutes(dto.DuracaoMinutos),
                Atividade = dto.Atividade,
                BlocoCronograma = blocoCronograma,
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

            var blocoCronograma = NormalizarBlocoCronograma(dto.BlocoCronograma);
            var conflitoAtualizacao = await ExisteConflitoSequenciaPorBloco(dto.CultoId, dto.Sequencia, blocoCronograma, id);
            if (conflitoAtualizacao)
            {
                return BadRequest(new { mensagem = $"Já existe uma etapa no bloco '{blocoCronograma}' com a sequência {dto.Sequencia}." });
            }

            entity.CultoId = dto.CultoId;
            entity.Sequencia = dto.Sequencia;
            entity.HorarioInicio = horarioInicio;
            entity.DuracaoMinutos = dto.DuracaoMinutos;
            entity.HorarioFimCalculado = horarioInicio.AddMinutes(dto.DuracaoMinutos);
            entity.Atividade = dto.Atividade;
            entity.BlocoCronograma = blocoCronograma;
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

        [HttpPut("culto/{cultoId:long}/reordenar")]
        [Authorize(Roles = "ADMIN,GESTAO_CULTO")]
        public async Task<IActionResult> Reordenar(long cultoId, [FromBody] CronogramaReordenarRequest dto)
        {
            var culto = await _db.Cultos.FirstOrDefaultAsync(x => x.Id == cultoId);
            if (culto == null)
            {
                return NotFound(new { mensagem = "Culto não encontrado." });
            }

            var blocos = dto.Blocos ?? Array.Empty<CronogramaBlocoOrdemRequest>();
            var etapas = await _db.EtapasCulto
                .Include(x => x.StatusEtapa)
                .Where(x => x.CultoId == cultoId)
                .ToListAsync();

            var idsEsperados = etapas.Select(x => x.Id).ToHashSet();
            var idsRecebidos = new List<long>();
            var ordensPorBloco = new List<(string Bloco, long[] EtapaIds)>();

            foreach (var blocoRequest in blocos)
            {
                var bloco = NormalizarBlocoCronograma(blocoRequest.BlocoCronograma).ToUpperInvariant();
                var ids = blocoRequest.EtapaIds ?? Array.Empty<long>();
                if (ids.Length == 0)
                {
                    return BadRequest(new { mensagem = $"O bloco '{bloco}' não possui etapas." });
                }

                if (ordensPorBloco.Any(x => x.Bloco == bloco))
                {
                    return BadRequest(new { mensagem = $"O bloco '{bloco}' foi enviado mais de uma vez." });
                }

                ordensPorBloco.Add((bloco, ids));
                idsRecebidos.AddRange(ids);
            }

            if (idsRecebidos.Count != idsRecebidos.Distinct().Count())
            {
                return BadRequest(new { mensagem = "Uma etapa não pode aparecer mais de uma vez." });
            }

            if (idsRecebidos.Count != idsEsperados.Count || !idsEsperados.SetEquals(idsRecebidos))
            {
                return BadRequest(new { mensagem = "A organização deve conter todas as etapas do culto." });
            }

            if (etapas.Any(x => x.DuracaoMinutos <= 0))
            {
                return BadRequest(new { mensagem = "Todas as etapas precisam ter duração maior que zero." });
            }

            if (etapas.Any(x => x.StatusEtapa.Codigo == "EM_ANDAMENTO" || x.StatusEtapa.Codigo == "CONCLUIDA"))
            {
                return Conflict(new { mensagem = "Não é possível reorganizar um cronograma que já está em execução ou concluído." });
            }

            var etapaPorId = etapas.ToDictionary(x => x.Id);
            await using var transacao = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
            try
            {
                foreach (var ordem in ordensPorBloco)
                {
                    var etapasDoBloco = ordem.EtapaIds.Select(id => etapaPorId[id]).ToList();
                    var horarioAtual = etapasDoBloco.Select(x => x.HorarioInicio).Min();

                    for (var index = 0; index < etapasDoBloco.Count; index++)
                    {
                        var etapa = etapasDoBloco[index];
                        etapa.BlocoCronograma = ordem.Bloco;
                        etapa.Sequencia = index + 1;
                        etapa.HorarioInicio = horarioAtual;
                        etapa.HorarioFimCalculado = horarioAtual.AddMinutes(etapa.DuracaoMinutos);
                        etapa.AtualizadoEm = DateTime.UtcNow;
                        horarioAtual = etapa.HorarioFimCalculado.Value;
                    }
                }

                await _db.SaveChangesAsync();
                await transacao.CommitAsync();
                return Ok(new { mensagem = "Cronograma reorganizado com sucesso." });
            }
            catch
            {
                await transacao.RollbackAsync();
                throw;
            }
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
                    e.BlocoCronograma,
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
                etapa.BlocoCronograma,
                etapa.Descricao,
                etapa.MinisterioResponsavelId,
                etapa.MinisterioResponsavelNome,
                etapa.AtrasoMinutos,
                etapa.StatusEtapaId,
                AcoesMinisterio = acoes
            };
        }

        private static string NormalizarBlocoCronograma(string? valor)
        {
            var texto = (valor ?? string.Empty).Trim();
            return string.IsNullOrWhiteSpace(texto) ? "PRINCIPAL" : texto;
        }

        private async Task<bool> ExisteConflitoSequenciaPorBloco(long cultoId, int sequencia, string blocoCronograma, long? etapaIgnoradaId)
        {
            var blocoNormalizado = (blocoCronograma ?? string.Empty).Trim().ToLower();

            return await _db.EtapasCulto
                .AsNoTracking()
                .Where(x => x.CultoId == cultoId)
                .Where(x => etapaIgnoradaId == null || x.Id != etapaIgnoradaId.Value)
                .Where(x => x.Sequencia == sequencia)
                .AnyAsync(x => ((x.BlocoCronograma ?? "PRINCIPAL").Trim().ToLower()) == blocoNormalizado);
        }
    }
}
