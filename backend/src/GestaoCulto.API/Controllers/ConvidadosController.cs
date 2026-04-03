using System.Linq;
using System.Threading.Tasks;
using GestaoCulto.Application.DTOs.Convidados;
using GestaoCulto.Domain.Entities;
using GestaoCulto.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GestaoCulto.API.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/convidados")]
    public class ConvidadosController : ControllerBase
    {
        private readonly GestaoCultoDbContext _db;

        public ConvidadosController(GestaoCultoDbContext db)
        {
            _db = db;
        }

        [HttpGet]
        public async Task<IActionResult> Listar([FromQuery] long? cultoId)
        {
            var query = _db.Convidados.AsNoTracking();
            if (cultoId.HasValue)
            {
                query = query.Where(c => c.CultoId == cultoId.Value);
            }

            var lista = await query
                .OrderByDescending(c => c.CriadoEm)
                .Select(c => new ConvidadoDto
                {
                    Id = c.Id,
                    CultoId = c.CultoId,
                    Nome = c.Nome,
                    Telefone = c.Telefone,
                    QuemConvidou = c.QuemConvidou,
                    PrimeiraVezIgreja = c.PrimeiraVezIgreja,
                    Observacoes = c.Observacoes,
                    StatusAcompanhamento = c.StatusAcompanhamento
                })
                .ToListAsync();

            return Ok(lista);
        }

        [HttpPost]
        [Authorize(Roles = "ADMIN,GESTAO_CULTO,RECEPCAO_DADOS")]
        public async Task<IActionResult> Criar([FromBody] ConvidadoDto dto)
        {
            var entity = new Convidado
            {
                CultoId = dto.CultoId,
                Nome = dto.Nome,
                Telefone = dto.Telefone,
                QuemConvidou = dto.QuemConvidou,
                PrimeiraVezIgreja = dto.PrimeiraVezIgreja,
                Observacoes = dto.Observacoes,
                StatusAcompanhamento = dto.StatusAcompanhamento,
                CriadoEm = System.DateTime.UtcNow
            };

            _db.Convidados.Add(entity);
            await _db.SaveChangesAsync();
            dto.Id = entity.Id;

            return CreatedAtAction(nameof(Listar), new { id = entity.Id }, dto);
        }

        [HttpPut("{id:long}")]
        [Authorize(Roles = "ADMIN,GESTAO_CULTO,RECEPCAO_DADOS")]
        public async Task<IActionResult> Atualizar(long id, [FromBody] ConvidadoDto dto)
        {
            var entity = await _db.Convidados.FirstOrDefaultAsync(x => x.Id == id);
            if (entity == null)
            {
                return NotFound(new { mensagem = "Convidado não encontrado." });
            }

            entity.CultoId = dto.CultoId;
            entity.Nome = dto.Nome;
            entity.Telefone = dto.Telefone;
            entity.QuemConvidou = dto.QuemConvidou;
            entity.PrimeiraVezIgreja = dto.PrimeiraVezIgreja;
            entity.Observacoes = dto.Observacoes;
            entity.StatusAcompanhamento = dto.StatusAcompanhamento;
            entity.AtualizadoEm = System.DateTime.UtcNow;

            await _db.SaveChangesAsync();
            dto.Id = entity.Id;
            return Ok(dto);
        }
    }
}
