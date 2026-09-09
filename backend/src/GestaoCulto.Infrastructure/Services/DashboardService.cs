using System;
using System.Linq;
using System.Threading.Tasks;
using GestaoCulto.Application.DTOs.Dashboard;
using GestaoCulto.Application.Interfaces;
using GestaoCulto.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GestaoCulto.Infrastructure.Services
{
    public class DashboardService : IDashboardService
    {
        private readonly GestaoCultoDbContext _db;

        public DashboardService(GestaoCultoDbContext db)
        {
            _db = db;
        }

        public async Task<DashboardResumoDto> ObterResumoAsync()
        {
            var hoje = DateTime.UtcNow.Date;

            var aniversariantes = await _db.Voluntarios
                .AsNoTracking()
                .Where(v => v.Ativo && v.DataNascimento.HasValue)
                .Select(v => new DashboardAniversarianteDto
                {
                    VoluntarioId = v.Id,
                    Nome = v.Nome,
                    DataNascimento = v.DataNascimento!.Value
                })
                .ToListAsync();

            aniversariantes = aniversariantes
                .Where(x => x.DataNascimento.Month == hoje.Month)
                .OrderBy(x => x.DataNascimento.Day)
                .ThenBy(x => x.Nome)
                .ToList();

            var proximoCulto = await _db.Cultos
                .AsNoTracking()
                .Where(c => c.DataCulto >= hoje)
                .OrderBy(c => c.DataCulto)
                .ThenBy(c => c.HorarioInicio)
                .FirstOrDefaultAsync();

            var cultoId = proximoCulto?.Id;

            return new DashboardResumoDto
            {
                ProximoCultoId = cultoId,
                ProximoCultoNome = proximoCulto?.Nome,
                VoluntariosEscalados = cultoId == null ? 0 : await _db.Escalas.CountAsync(e => e.CultoId == cultoId.Value),
                TarefasPendentes = cultoId == null ? 0 : await _db.EtapasCulto.CountAsync(e => e.CultoId == cultoId.Value && e.StatusEtapaId == 1),
                ConvidadosRegistrados = cultoId == null ? 0 : await _db.Convidados.CountAsync(c => c.CultoId == cultoId.Value),
                NovosConvertidos = cultoId == null
                    ? 0
                    : await _db.NovosConvertidos.CountAsync(n => _db.Convidados.Any(c => c.Id == n.ConvidadoId && c.CultoId == cultoId.Value)),
                AlertasOperacionais = cultoId == null
                    ? 0
                    : await _db.EtapasCulto.CountAsync(e => e.CultoId == cultoId.Value && e.AtrasoMinutos > 0),
                Aniversariantes = aniversariantes
            };
        }

        public async Task<DashboardEstatisticasDto> ObterEstatisticasAsync(DateTime? inicio, DateTime? fim, int limite)
        {
            var hoje = DateTime.UtcNow.Date;
            var periodoInicio = (inicio ?? new DateTime(hoje.Year, 1, 1)).Date;
            var periodoFim = (fim ?? hoje).Date;
            var limiteFim = periodoFim.AddDays(1);

            if (limiteFim > hoje)
            {
                limiteFim = hoje;
                periodoFim = hoje.AddDays(-1);
            }

            var cultos = _db.Cultos
                .AsNoTracking()
                .Where(c => c.DataCulto >= periodoInicio && c.DataCulto < limiteFim && c.StatusCulto.Codigo == "ATIVO");

            var servicos = await _db.Escalas
                .AsNoTracking()
                .Where(e => e.VoluntarioId.HasValue
                    && e.PresencaStatus.Codigo == "CONFIRMADO"
                    && cultos.Select(c => c.Id).Contains(e.CultoId))
                .Select(e => new { VoluntarioId = e.VoluntarioId.GetValueOrDefault(), e.CultoId })
                .Distinct()
                .GroupBy(e => e.VoluntarioId)
                .Select(g => new { VoluntarioId = g.Key, QuantidadeCultos = g.Count() })
                .OrderByDescending(x => x.QuantidadeCultos)
                .ThenBy(x => x.VoluntarioId)
                .Take(limite)
                .ToListAsync();

            var voluntarioIds = servicos.Select(x => x.VoluntarioId).ToList();
            var voluntarios = await _db.Voluntarios
                .AsNoTracking()
                .Where(v => voluntarioIds.Contains(v.Id))
                .ToDictionaryAsync(v => v.Id);

            var musicas = await _db.RepertoriosCultoItens
                .AsNoTracking()
                .Where(i => cultos.Select(c => c.Id).Contains(i.RepertorioCulto.CultoId))
                .Select(i => new { i.MusicaId, CultoId = i.RepertorioCulto.CultoId })
                .Distinct()
                .GroupBy(i => i.MusicaId)
                .Select(g => new { MusicaId = g.Key, QuantidadeCultos = g.Count() })
                .OrderByDescending(x => x.QuantidadeCultos)
                .ThenBy(x => x.MusicaId)
                .Take(limite)
                .ToListAsync();

            var musicaIds = musicas.Select(x => x.MusicaId).ToList();
            var musicasCadastradas = await _db.Musicas
                .AsNoTracking()
                .Where(m => musicaIds.Contains(m.Id))
                .ToDictionaryAsync(m => m.Id);

            return new DashboardEstatisticasDto
            {
                PeriodoInicio = periodoInicio,
                PeriodoFim = periodoFim,
                Voluntarios = servicos
                    .Where(x => voluntarios.ContainsKey(x.VoluntarioId))
                    .Select(x => new DashboardVoluntarioRankingDto
                    {
                        VoluntarioId = x.VoluntarioId,
                        Nome = voluntarios[x.VoluntarioId].Nome,
                        QuantidadeCultos = x.QuantidadeCultos,
                        Ativo = voluntarios[x.VoluntarioId].Ativo
                    })
                    .OrderByDescending(x => x.QuantidadeCultos)
                    .ThenBy(x => x.Nome)
                    .ToList(),
                Musicas = musicas
                    .Where(x => musicasCadastradas.ContainsKey(x.MusicaId))
                    .Select(x => new DashboardMusicaRankingDto
                    {
                        MusicaId = x.MusicaId,
                        Titulo = musicasCadastradas[x.MusicaId].Titulo,
                        ArtistaBanda = musicasCadastradas[x.MusicaId].ArtistaBanda,
                        QuantidadeCultos = x.QuantidadeCultos,
                        Ativo = musicasCadastradas[x.MusicaId].Ativo
                    })
                    .OrderByDescending(x => x.QuantidadeCultos)
                    .ThenBy(x => x.Titulo)
                    .ToList()
            };
        }
    }
}
