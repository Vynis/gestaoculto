using System;
using System.Linq;
using System.Threading.Tasks;
using GestaoCulto.Application.Interfaces;
using GestaoCulto.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GestaoCulto.Infrastructure.Services
{
    public class RepertorioPermissionService : IRepertorioPermissionService
    {
        private readonly GestaoCultoDbContext _db;

        public RepertorioPermissionService(GestaoCultoDbContext db)
        {
            _db = db;
        }

        public Task<bool> EhMembroLouvorAsync(long usuarioId)
        {
            return _db.MinisteriosVoluntarios.AsNoTracking()
                .Join(_db.Voluntarios.AsNoTracking(),
                    vm => vm.VoluntarioId,
                    v => v.Id,
                    (vm, v) => new { vm.MinisterioId, v.UsuarioId, VoluntarioAtivo = v.Ativo })
                .Join(_db.Ministerios.AsNoTracking(),
                    x => x.MinisterioId,
                    m => m.Id,
                    (x, m) => new { x.UsuarioId, x.VoluntarioAtivo, m.Codigo, m.Ativo })
                .AnyAsync(x => x.UsuarioId == usuarioId && x.VoluntarioAtivo && x.Ativo && x.Codigo == "LOUVOR");
        }

        public Task<bool> PodeGerenciarAsync(long usuarioId, long escalaId)
        {
            var agora = DateTime.Now;
            return _db.Escalas.AsNoTracking().AnyAsync(x =>
                x.Id == escalaId && x.Voluntario != null && x.Voluntario.Ativo && x.Voluntario.UsuarioId == usuarioId
                && x.PodeGerenciarRepertorio
                && x.Ministerio != null && x.Ministerio.Codigo == "LOUVOR"
                && x.Culto != null
                && x.Culto.DataCulto >= agora.Date
                && (x.Culto.DataCulto > agora.Date || x.Culto.HorarioInicio > agora.TimeOfDay)
                && _db.StatusCultos.Any(status => status.Id == x.Culto.StatusCultoId && status.Codigo == "ATIVO"));
        }

        public async Task<long?> ObterEscalaGerenciavelAsync(long usuarioId, long cultoId)
        {
            var agora = DateTime.Now;
            var escala = await _db.Escalas.AsNoTracking().FirstOrDefaultAsync(x =>
                x.CultoId == cultoId
                && x.Voluntario != null && x.Voluntario.Ativo && x.Voluntario.UsuarioId == usuarioId
                && x.PodeGerenciarRepertorio
                && x.Ministerio != null && x.Ministerio.Codigo == "LOUVOR"
                && x.Culto != null
                && x.Culto.DataCulto >= agora.Date
                && (x.Culto.DataCulto > agora.Date || x.Culto.HorarioInicio > agora.TimeOfDay)
                && _db.StatusCultos.Any(status => status.Id == x.Culto.StatusCultoId && status.Codigo == "ATIVO"));

            return escala?.Id;
        }
    }
}
