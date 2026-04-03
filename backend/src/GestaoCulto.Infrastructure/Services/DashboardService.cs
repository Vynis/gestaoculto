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
                    : await _db.EtapasCulto.CountAsync(e => e.CultoId == cultoId.Value && e.AtrasoMinutos > 0)
            };
        }
    }
}
