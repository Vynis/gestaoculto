using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using GestaoCulto.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GestaoCulto.API.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/relatorios")]
    public class RelatoriosController : ControllerBase
    {
        private class EscalaRelatorioItem
        {
            public long Id { get; set; }
            public long CultoId { get; set; }
            public long? EtapaCultoId { get; set; }
            public string Funcao { get; set; } = string.Empty;
            public long VoluntarioId { get; set; }
            public string? VoluntarioNome { get; set; }
            public long? MinisterioId { get; set; }
            public string? MinisterioNome { get; set; }
            public long PresencaStatusId { get; set; }
            public string? PresencaStatusNome { get; set; }
            public DateTime? HorarioPrevisto { get; set; }
            public DateTime? ConfirmadoEm { get; set; }
            public string? Observacoes { get; set; }
        }

        private class CultoRelatorioItem
        {
            public long Id { get; set; }
            public string Nome { get; set; } = string.Empty;
            public string TipoCulto { get; set; } = string.Empty;
            public DateTime DataCulto { get; set; }
            public TimeSpan HorarioInicio { get; set; }
            public TimeSpan? HorarioFimPrevisto { get; set; }
            public string? ObservacoesGerais { get; set; }
            public int TotalVisitantes { get; set; }
            public int TotalNovosConvertidos { get; set; }
            public long StatusCultoId { get; set; }
            public string? StatusCultoNome { get; set; }
        }

        private readonly GestaoCultoDbContext _db;

        public RelatoriosController(GestaoCultoDbContext db)
        {
            _db = db;
        }

        [HttpGet("cultos")]
        [AllowAnonymous]
        public async Task<IActionResult> ListarCultosParaRelatorio()
        {
            var cultos = await _db.Cultos
                .AsNoTracking()
                .OrderByDescending(x => x.DataCulto)
                .ThenByDescending(x => x.HorarioInicio)
                .Select(x => new
                {
                    x.Id,
                    x.Nome,
                    x.TipoCulto,
                    x.DataCulto,
                    x.HorarioInicio,
                    x.HorarioFimPrevisto,
                    x.StatusCultoId,
                    x.ObservacoesGerais,
                    x.TotalVisitantes,
                    x.TotalNovosConvertidos
                })
                .ToListAsync();

            return Ok(cultos);
        }

        [HttpGet("culto-dia")]
        [AllowAnonymous]
        public async Task<IActionResult> RelatorioCultoDia([FromQuery] string? data)
        {
            if (string.IsNullOrWhiteSpace(data) || !DateTime.TryParseExact(data, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var dataCulto))
            {
                return BadRequest(new { mensagem = "Informe a data no formato yyyy-MM-dd." });
            }

            var inicioDia = dataCulto.Date;
            var fimDia = inicioDia.AddDays(1);

            var cultos = await _db.Cultos
                .AsNoTracking()
                .Where(c => c.DataCulto >= inicioDia && c.DataCulto < fimDia)
                .OrderBy(c => c.HorarioInicio)
                .Select(c => new CultoRelatorioItem
                {
                    Id = c.Id,
                    Nome = c.Nome,
                    TipoCulto = c.TipoCulto,
                    DataCulto = c.DataCulto,
                    HorarioInicio = c.HorarioInicio,
                    HorarioFimPrevisto = c.HorarioFimPrevisto,
                    ObservacoesGerais = c.ObservacoesGerais,
                    TotalVisitantes = c.TotalVisitantes,
                    TotalNovosConvertidos = c.TotalNovosConvertidos,
                    StatusCultoId = c.StatusCultoId,
                    StatusCultoNome = _db.StatusCultos.Where(s => s.Id == c.StatusCultoId).Select(s => s.Nome).FirstOrDefault()
                })
                .ToListAsync();

            if (!cultos.Any())
            {
                return Ok(new
                {
                    data = dataCulto.ToString("yyyy-MM-dd"),
                    quantidadeCultos = 0,
                    cultos = new List<object>()
                });
            }

            return Ok(await MontarRelatorio(cultos, dataCulto.ToString("yyyy-MM-dd")));
        }

        [HttpGet("culto/{cultoId:long}")]
        [AllowAnonymous]
        public async Task<IActionResult> RelatorioCultoPorId(long cultoId)
        {
            var cultos = await _db.Cultos
                .AsNoTracking()
                .Where(c => c.Id == cultoId)
                .OrderBy(c => c.HorarioInicio)
                .Select(c => new CultoRelatorioItem
                {
                    Id = c.Id,
                    Nome = c.Nome,
                    TipoCulto = c.TipoCulto,
                    DataCulto = c.DataCulto,
                    HorarioInicio = c.HorarioInicio,
                    HorarioFimPrevisto = c.HorarioFimPrevisto,
                    ObservacoesGerais = c.ObservacoesGerais,
                    TotalVisitantes = c.TotalVisitantes,
                    TotalNovosConvertidos = c.TotalNovosConvertidos,
                    StatusCultoId = c.StatusCultoId,
                    StatusCultoNome = _db.StatusCultos.Where(s => s.Id == c.StatusCultoId).Select(s => s.Nome).FirstOrDefault()
                })
                .ToListAsync();

            if (!cultos.Any())
            {
                return NotFound(new { mensagem = "Culto não encontrado para relatório." });
            }

            var dataRef = cultos[0].DataCulto.ToString("yyyy-MM-dd");
            return Ok(await MontarRelatorio(cultos, dataRef));
        }

        private async Task<object> MontarRelatorio(List<CultoRelatorioItem> cultos, string dataRef)
        {

            var cultoIds = cultos.Select(c => c.Id).ToList();

            var etapas = await _db.EtapasCulto
                .AsNoTracking()
                .Where(e => cultoIds.Contains(e.CultoId))
                .OrderBy(e => e.CultoId)
                .ThenBy(e => e.Sequencia)
                .Select(e => new
                {
                    e.Id,
                    e.CultoId,
                    e.Sequencia,
                    e.HorarioInicio,
                    e.HorarioFimCalculado,
                    e.DuracaoMinutos,
                    e.Atividade,
                    e.Descricao,
                    e.MinisterioResponsavelId,
                    MinisterioResponsavelNome = _db.Ministerios.Where(m => m.Id == e.MinisterioResponsavelId).Select(m => m.Nome).FirstOrDefault(),
                    e.StatusEtapaId,
                    StatusEtapaNome = _db.StatusEtapas.Where(s => s.Id == e.StatusEtapaId).Select(s => s.Nome).FirstOrDefault(),
                    e.AtrasoMinutos
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
                .OrderBy(x => x.CultoId)
                .ThenBy(x => x.Sequencia)
                .ThenBy(x => x.Id)
                .ToList();

            var etapaIds = etapas.Select(x => (long)x.Id).ToList();
            var acoesEtapa = await _db.EtapasCultoMinisteriosAcoes
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

            var mapaAcoesEtapa = acoesEtapa
                .GroupBy(x => x.EtapaCultoId)
                .ToDictionary(g => g.Key, g => (object)g.ToList());

            var escalas = await _db.Escalas
                .AsNoTracking()
                .Where(e => cultoIds.Contains(e.CultoId))
                .OrderBy(e => e.CultoId)
                .ThenBy(e => e.Funcao)
                .ThenBy(e => e.VoluntarioId)
                .Select(e => new EscalaRelatorioItem
                {
                    Id = e.Id,
                    CultoId = e.CultoId,
                    EtapaCultoId = e.EtapaCultoId,
                    Funcao = e.Funcao,
                    VoluntarioId = e.VoluntarioId,
                    VoluntarioNome = _db.Voluntarios.Where(v => v.Id == e.VoluntarioId).Select(v => v.Nome).FirstOrDefault(),
                    MinisterioId = e.MinisterioId,
                    MinisterioNome = _db.Ministerios.Where(m => m.Id == e.MinisterioId).Select(m => m.Nome).FirstOrDefault(),
                    PresencaStatusId = e.PresencaStatusId,
                    PresencaStatusNome = _db.PresencaEscalaStatus.Where(s => s.Id == e.PresencaStatusId).Select(s => s.Nome).FirstOrDefault(),
                    HorarioPrevisto = e.HorarioPrevisto,
                    ConfirmadoEm = e.ConfirmadoEm,
                    Observacoes = e.Observacoes
                })
                .ToListAsync();

            var repertorios = await _db.RepertoriosCulto
                .AsNoTracking()
                .Where(x => cultoIds.Contains(x.CultoId))
                .Select(x => new
                {
                    x.Id,
                    x.CultoId,
                    x.Observacoes
                })
                .ToListAsync();

            var repertorioIds = repertorios.Select(x => x.Id).ToList();
            var repertorioItens = await _db.RepertoriosCultoItens
                .AsNoTracking()
                .Where(x => repertorioIds.Contains(x.RepertorioCultoId))
                .OrderBy(x => x.RepertorioCultoId)
                .ThenBy(x => x.Ordem)
                .Select(x => new
                {
                    x.Id,
                    x.RepertorioCultoId,
                    x.MusicaId,
                    MusicaTitulo = !string.IsNullOrWhiteSpace(x.MusicaTitulo)
                        ? x.MusicaTitulo
                        : _db.Musicas.Where(m => m.Id == x.MusicaId).Select(m => m.Titulo).FirstOrDefault(),
                    MusicaTom = !string.IsNullOrWhiteSpace(x.MusicaTom)
                        ? x.MusicaTom
                        : _db.Musicas.Where(m => m.Id == x.MusicaId).Select(m => m.Tom).FirstOrDefault(),
                    MusicaLinkCifra = !string.IsNullOrWhiteSpace(x.MusicaLinkCifra)
                        ? x.MusicaLinkCifra
                        : _db.Musicas.Where(m => m.Id == x.MusicaId).Select(m => m.LinkCifra).FirstOrDefault(),
                    MusicaLinkVideo = !string.IsNullOrWhiteSpace(x.MusicaLinkVideo)
                        ? x.MusicaLinkVideo
                        : _db.Musicas.Where(m => m.Id == x.MusicaId).Select(m => m.LinkVideo).FirstOrDefault(),
                    MusicaObservacoes = !string.IsNullOrWhiteSpace(x.MusicaObservacoes)
                        ? x.MusicaObservacoes
                        : _db.Musicas.Where(m => m.Id == x.MusicaId).Select(m => m.Observacoes).FirstOrDefault(),
                    x.EtapaCultoId,
                    EtapaAtividade = _db.EtapasCulto.Where(e => e.Id == x.EtapaCultoId).Select(e => e.Atividade).FirstOrDefault(),
                    x.Ordem,
                    x.Responsavel,
                    x.Observacoes
                })
                .ToListAsync();

            var etapasComAcoes = etapas.Select(x => new
            {
                x.Id,
                x.CultoId,
                x.Sequencia,
                x.HorarioInicio,
                x.HorarioFimCalculado,
                x.DuracaoMinutos,
                x.Atividade,
                x.Descricao,
                x.MinisterioResponsavelId,
                x.MinisterioResponsavelNome,
                x.StatusEtapaId,
                x.StatusEtapaNome,
                x.AtrasoMinutos,
                AcoesMinisterio = mapaAcoesEtapa.ContainsKey(x.Id) ? mapaAcoesEtapa[x.Id] : Array.Empty<object>()
            }).ToList();

            var etapasPorCulto = etapasComAcoes
                .GroupBy(e => e.CultoId)
                .ToDictionary(g => g.Key, g => g.Cast<object>().ToList());

            var escalasPorCulto = escalas
                .GroupBy(e => e.CultoId)
                .ToDictionary(g => g.Key, g => g.ToList());

            var mapaRepertorioPorCulto = repertorios
                .ToDictionary(x => x.CultoId, x => x);

            var mapaRepertorioItens = repertorioItens
                .GroupBy(x => x.RepertorioCultoId)
                .ToDictionary(g => g.Key, g => g.Cast<object>().ToList());

            var resultadoCultos = cultos.Select(culto =>
            {
                var escalasDoCulto = escalasPorCulto.ContainsKey(culto.Id)
                    ? escalasPorCulto[culto.Id]
                    : new List<EscalaRelatorioItem>();

                var repertorio = mapaRepertorioPorCulto.ContainsKey(culto.Id)
                    ? mapaRepertorioPorCulto[culto.Id]
                    : null;

                var repertorioDoCulto = repertorio == null
                    ? new
                    {
                        id = 0L,
                        cultoId = culto.Id,
                        observacoes = (string?)null,
                        itens = new List<object>()
                    }
                    : new
                    {
                        id = repertorio.Id,
                        cultoId = repertorio.CultoId,
                        observacoes = repertorio.Observacoes,
                        itens = mapaRepertorioItens.ContainsKey(repertorio.Id) ? mapaRepertorioItens[repertorio.Id] : new List<object>()
                    };

                var totalEscalados = escalasDoCulto.Count;
                var totalConfirmados = escalasDoCulto.Count(x => x.PresencaStatusId == 2);
                var totalPendentes = escalasDoCulto.Count(x => x.PresencaStatusId == 1);

                var equipes = escalasDoCulto
                    .GroupBy(x => string.IsNullOrWhiteSpace(x.MinisterioNome) ? "Sem equipe" : x.MinisterioNome)
                    .Select(g => new
                    {
                        nome = g.Key,
                        total = g.Count()
                    })
                    .OrderByDescending(x => x.total)
                    .ThenBy(x => x.nome)
                    .ToList();

                return new
                {
                    culto = culto,
                    resumo = new
                    {
                        totalEtapas = etapasPorCulto.ContainsKey(culto.Id) ? etapasPorCulto[culto.Id].Count : 0,
                        totalEscalados,
                        totalConfirmados,
                        totalPendentes,
                        equipes
                    },
                    cronograma = etapasPorCulto.ContainsKey(culto.Id) ? etapasPorCulto[culto.Id] : new List<object>(),
                    voluntarios = escalasDoCulto,
                    repertorio = repertorioDoCulto
                };
            }).ToList();

            return new
            {
                data = dataRef,
                quantidadeCultos = resultadoCultos.Count,
                cultos = resultadoCultos
            };
        }
    }
}
