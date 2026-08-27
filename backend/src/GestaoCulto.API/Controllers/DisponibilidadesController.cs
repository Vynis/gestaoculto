using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using GestaoCulto.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GestaoCulto.API.Controllers
{
    [ApiController]
    [Authorize(Roles = "ADMIN,GESTAO_CULTO,LIDER_MINISTERIO")]
    [Route("api/disponibilidades")]
    public class DisponibilidadesController : ControllerBase
    {
        private readonly GestaoCultoDbContext _db;

        public DisponibilidadesController(GestaoCultoDbContext db)
        {
            _db = db;
        }

        [HttpGet]
        public async Task<IActionResult> Listar(
            [FromQuery] long? cultoId,
            [FromQuery] long? ministerioId,
            [FromQuery] long? statusDisponibilidadeId)
        {
            var ministeriosPermitidos = await ObterMinisteriosPermitidosParaUsuario();

            var query = _db.DisponibilidadesCultoVoluntarios
                .AsNoTracking()
                .AsQueryable();

            if (cultoId.HasValue && cultoId.Value > 0)
            {
                query = query.Where(x => x.CultoId == cultoId.Value);
            }

            if (statusDisponibilidadeId.HasValue && statusDisponibilidadeId.Value > 0)
            {
                query = query.Where(x => x.StatusDisponibilidadeId == statusDisponibilidadeId.Value);
            }

            var itens = await query
                .OrderByDescending(x => x.RespondidoEm)
                .Select(x => new
                {
                    x.Id,
                    x.CultoId,
                    CultoNome = _db.Cultos.Where(c => c.Id == x.CultoId).Select(c => c.Nome).FirstOrDefault(),
                    DataCulto = _db.Cultos.Where(c => c.Id == x.CultoId).Select(c => c.DataCulto).FirstOrDefault(),
                    HorarioInicio = _db.Cultos.Where(c => c.Id == x.CultoId).Select(c => c.HorarioInicio).FirstOrDefault(),
                    x.VoluntarioId,
                    VoluntarioNome = _db.Voluntarios.Where(v => v.Id == x.VoluntarioId).Select(v => v.Nome).FirstOrDefault(),
                    x.StatusDisponibilidadeId,
                    StatusNome = _db.StatusDisponibilidadeVoluntarios.Where(s => s.Id == x.StatusDisponibilidadeId).Select(s => s.Nome).FirstOrDefault(),
                    x.Observacao,
                    x.RespondidoEm
                })
                .ToListAsync();

            var idsDisponibilidade = itens.Select(x => x.Id).ToList();
            var ministerios = await _db.DisponibilidadesCultoVoluntariosMinisterios
                .AsNoTracking()
                .Where(x => idsDisponibilidade.Contains(x.DisponibilidadeCultoVoluntarioId))
                .Select(x => new
                {
                    x.DisponibilidadeCultoVoluntarioId,
                    x.MinisterioId,
                    MinisterioNome = _db.Ministerios.Where(m => m.Id == x.MinisterioId).Select(m => m.Nome).FirstOrDefault()
                })
                .ToListAsync();

            if (ministeriosPermitidos != null)
            {
                ministerios = ministerios.Where(x => ministeriosPermitidos.Contains(x.MinisterioId)).ToList();
            }

            var disponibilidadeIdsPermitidos = ministerios
                .Select(x => x.DisponibilidadeCultoVoluntarioId)
                .Distinct()
                .ToHashSet();

            HashSet<long>? voluntariosPermitidos = null;
            if (ministeriosPermitidos != null)
            {
                var voluntariosPermitidosLista = await _db.MinisteriosVoluntarios
                    .AsNoTracking()
                    .Where(x => ministeriosPermitidos.Contains(x.MinisterioId))
                    .Select(x => x.VoluntarioId)
                    .Distinct()
                    .ToListAsync();
                voluntariosPermitidos = voluntariosPermitidosLista.ToHashSet();
            }

            var cultoIds = itens.Select(x => x.CultoId).Distinct().ToList();
            var voluntarioIds = itens.Select(x => x.VoluntarioId).Distinct().ToList();
            var escalas = await _db.Escalas
                .AsNoTracking()
                .Where(x => cultoIds.Contains(x.CultoId) && x.VoluntarioId.HasValue && voluntarioIds.Contains(x.VoluntarioId.Value))
                .Select(x => new { x.CultoId, VoluntarioId = x.VoluntarioId!.Value })
                .Distinct()
                .ToListAsync();
            var chavesEscala = escalas
                .Select(x => $"{x.CultoId}-{x.VoluntarioId}")
                .ToHashSet();

            var lista = itens
                .Where(x => ministeriosPermitidos == null
                            || disponibilidadeIdsPermitidos.Contains(x.Id)
                            || (voluntariosPermitidos != null && voluntariosPermitidos.Contains(x.VoluntarioId)))
                .Select(item =>
                {
                    var ministeriosItem = ministerios
                        .Where(x => x.DisponibilidadeCultoVoluntarioId == item.Id)
                        .Select(x => new { x.MinisterioId, x.MinisterioNome })
                        .ToList();

                    var escalado = chavesEscala.Contains($"{item.CultoId}-{item.VoluntarioId}");

                    return new
                    {
                        item.Id,
                        item.CultoId,
                        item.CultoNome,
                        item.DataCulto,
                        item.HorarioInicio,
                        item.VoluntarioId,
                        item.VoluntarioNome,
                        item.StatusDisponibilidadeId,
                        item.StatusNome,
                        item.Observacao,
                        item.RespondidoEm,
                        Ministerios = ministeriosItem,
                        Escalado = escalado
                    };
                })
                .Where(x => !ministerioId.HasValue || ministerioId.Value <= 0 || x.Ministerios.Any(m => m.MinisterioId == ministerioId.Value))
                .ToList();

            return Ok(lista);
        }

        [HttpGet("status")]
        public async Task<IActionResult> ListarStatus()
        {
            var lista = await _db.StatusDisponibilidadeVoluntarios
                .AsNoTracking()
                .OrderBy(x => x.Ordem)
                .Select(x => new { x.Id, x.Nome, x.Codigo, x.CorHex, x.Ordem })
                .ToListAsync();

            return Ok(lista);
        }

        private async Task<long[]?> ObterMinisteriosPermitidosParaUsuario()
        {
            var ehAdminOuGestao = User.IsInRole("ADMIN") || User.IsInRole("GESTAO_CULTO");
            if (ehAdminOuGestao)
            {
                return null;
            }

            if (!User.IsInRole("LIDER_MINISTERIO"))
            {
                return Array.Empty<long>();
            }

            var sub = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
            if (!long.TryParse(sub, out var usuarioId))
            {
                return Array.Empty<long>();
            }

            var ministerios = await _db.MinisteriosLideres
                .AsNoTracking()
                .Where(x => x.UsuarioId == usuarioId)
                .Select(x => x.MinisterioId)
                .Distinct()
                .ToArrayAsync();

            return ministerios;
        }
    }
}
