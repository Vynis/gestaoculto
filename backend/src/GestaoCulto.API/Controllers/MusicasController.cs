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
    public class MusicaRequest
    {
        public string Titulo { get; set; } = string.Empty;
        public string ArtistaBanda { get; set; } = string.Empty;
        public string? Tom { get; set; }
        public string? LinkCifra { get; set; }
        public string? LinkVideo { get; set; }
        public string? Observacoes { get; set; }
        public bool Ativo { get; set; } = true;
    }

    [ApiController]
    [Authorize]
    [Route("api/musicas")]
    public class MusicasController : ControllerBase
    {
        private readonly GestaoCultoDbContext _db;

        public MusicasController(GestaoCultoDbContext db)
        {
            _db = db;
        }

        [HttpGet]
        public async Task<IActionResult> Listar([FromQuery] string? busca, [FromQuery] bool? ativo)
        {
            var query = _db.Musicas.AsNoTracking();

            if (!string.IsNullOrWhiteSpace(busca))
            {
                var termo = busca.Trim().ToLower();
                query = query.Where(x => x.Titulo.ToLower().Contains(termo) || x.ArtistaBanda.ToLower().Contains(termo));
            }

            if (ativo.HasValue)
            {
                query = query.Where(x => x.Ativo == ativo.Value);
            }

            var lista = await query
                .OrderBy(x => x.Titulo)
                .Select(x => new
                {
                    x.Id,
                    x.Titulo,
                    x.ArtistaBanda,
                    x.Tom,
                    x.LinkCifra,
                    x.LinkVideo,
                    x.Observacoes,
                    x.Ativo
                })
                .ToListAsync();

            return Ok(lista);
        }

        [HttpPost]
        [Authorize(Roles = "ADMIN,GESTAO_CULTO,LIDER_MINISTERIO")]
        public async Task<IActionResult> Criar([FromBody] MusicaRequest dto)
        {
            var entity = new Musica
            {
                Titulo = dto.Titulo,
                ArtistaBanda = dto.ArtistaBanda,
                Tom = dto.Tom,
                LinkCifra = dto.LinkCifra,
                LinkVideo = dto.LinkVideo,
                Observacoes = dto.Observacoes,
                Ativo = dto.Ativo,
                CriadoEm = DateTime.UtcNow
            };

            _db.Musicas.Add(entity);
            await _db.SaveChangesAsync();
            return Ok(new { mensagem = "Música cadastrada com sucesso.", id = entity.Id });
        }

        [HttpPut("{id:long}")]
        [Authorize(Roles = "ADMIN,GESTAO_CULTO,LIDER_MINISTERIO")]
        public async Task<IActionResult> Atualizar(long id, [FromBody] MusicaRequest dto)
        {
            var entity = await _db.Musicas.FirstOrDefaultAsync(x => x.Id == id);
            if (entity == null)
            {
                return NotFound(new { mensagem = "Música não encontrada." });
            }

            entity.Titulo = dto.Titulo;
            entity.ArtistaBanda = dto.ArtistaBanda;
            entity.Tom = dto.Tom;
            entity.LinkCifra = dto.LinkCifra;
            entity.LinkVideo = dto.LinkVideo;
            entity.Observacoes = dto.Observacoes;
            entity.Ativo = dto.Ativo;
            entity.AtualizadoEm = DateTime.UtcNow;

            await _db.SaveChangesAsync();
            return Ok(new { mensagem = "Música atualizada com sucesso." });
        }

        [HttpDelete("{id:long}")]
        [Authorize(Roles = "ADMIN,GESTAO_CULTO,LIDER_MINISTERIO")]
        public async Task<IActionResult> Excluir(long id)
        {
            var entity = await _db.Musicas.FirstOrDefaultAsync(x => x.Id == id);
            if (entity == null)
            {
                return NotFound(new { mensagem = "Música não encontrada." });
            }

            _db.Musicas.Remove(entity);
            await _db.SaveChangesAsync();
            return Ok(new { mensagem = "Música excluída com sucesso." });
        }
    }
}
