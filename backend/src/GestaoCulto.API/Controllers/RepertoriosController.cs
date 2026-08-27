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
    public class RepertorioItemRequest
    {
        public long MusicaId { get; set; }
        public string? MusicaTitulo { get; set; }
        public string? MusicaArtistaBanda { get; set; }
        public string? MusicaTom { get; set; }
        public string? MusicaLinkCifra { get; set; }
        public string? MusicaLinkVideo { get; set; }
        public string? MusicaObservacoes { get; set; }
        public long? EtapaCultoId { get; set; }
        public int Ordem { get; set; }
        public string? Responsavel { get; set; }
        public string? Observacoes { get; set; }
    }

    public class RepertorioRequest
    {
        public long CultoId { get; set; }
        public string? Observacoes { get; set; }
        public List<RepertorioItemRequest> Itens { get; set; } = new List<RepertorioItemRequest>();
    }

    [ApiController]
    [Authorize]
    [Route("api/repertorios")]
    public class RepertoriosController : ControllerBase
    {
        private readonly GestaoCultoDbContext _db;

        public RepertoriosController(GestaoCultoDbContext db)
        {
            _db = db;
        }

        [HttpGet("culto/{cultoId:long}")]
        public async Task<IActionResult> ObterPorCulto(long cultoId)
        {
            var repertorio = await _db.RepertoriosCulto
                .AsNoTracking()
                .Where(x => x.CultoId == cultoId)
                .Select(x => new
                {
                    x.Id,
                    x.CultoId,
                    x.Observacoes
                })
                .FirstOrDefaultAsync();

            if (repertorio == null)
            {
                return Ok(new { cultoId, observacoes = (string?)null, itens = new List<object>() });
            }

            var itens = await _db.RepertoriosCultoItens
                .AsNoTracking()
                .Where(x => x.RepertorioCultoId == repertorio.Id)
                .OrderBy(x => x.Ordem)
                .Select(x => new
                {
                    x.Id,
                    x.MusicaId,
                    MusicaTitulo = !string.IsNullOrWhiteSpace(x.MusicaTitulo)
                        ? x.MusicaTitulo
                        : _db.Musicas.Where(m => m.Id == x.MusicaId).Select(m => m.Titulo).FirstOrDefault(),
                    MusicaArtistaBanda = !string.IsNullOrWhiteSpace(x.MusicaArtistaBanda)
                        ? x.MusicaArtistaBanda
                        : _db.Musicas.Where(m => m.Id == x.MusicaId).Select(m => m.ArtistaBanda).FirstOrDefault(),
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

            return Ok(new
            {
                repertorio.Id,
                repertorio.CultoId,
                repertorio.Observacoes,
                Itens = itens
            });
        }

        [HttpPost]
        [Authorize(Roles = "ADMIN,GESTAO_CULTO,LIDER_MINISTERIO")]
        public async Task<IActionResult> Salvar([FromBody] RepertorioRequest dto)
        {
            var repertorio = await _db.RepertoriosCulto.FirstOrDefaultAsync(x => x.CultoId == dto.CultoId);
            if (repertorio == null)
            {
                repertorio = new RepertorioCulto
                {
                    CultoId = dto.CultoId,
                    Observacoes = dto.Observacoes,
                    CriadoEm = DateTime.UtcNow
                };
                _db.RepertoriosCulto.Add(repertorio);
                await _db.SaveChangesAsync();
            }
            else
            {
                repertorio.Observacoes = dto.Observacoes;
                repertorio.AtualizadoEm = DateTime.UtcNow;
                var itensAtuais = await _db.RepertoriosCultoItens.Where(x => x.RepertorioCultoId == repertorio.Id).ToListAsync();
                if (itensAtuais.Any())
                {
                    _db.RepertoriosCultoItens.RemoveRange(itensAtuais);
                    await _db.SaveChangesAsync();
                }
            }

            var itensNormalizados = (dto.Itens ?? new List<RepertorioItemRequest>())
                .Where(x => x.MusicaId > 0)
                .OrderBy(x => x.Ordem)
                .ToList();

            for (var i = 0; i < itensNormalizados.Count; i++)
            {
                var item = itensNormalizados[i];
                _db.RepertoriosCultoItens.Add(new RepertorioCultoItem
                {
                    RepertorioCultoId = repertorio.Id,
                    MusicaId = item.MusicaId,
                    MusicaTitulo = item.MusicaTitulo,
                    MusicaArtistaBanda = item.MusicaArtistaBanda,
                    MusicaTom = item.MusicaTom,
                    MusicaLinkCifra = item.MusicaLinkCifra,
                    MusicaLinkVideo = item.MusicaLinkVideo,
                    MusicaObservacoes = item.MusicaObservacoes,
                    EtapaCultoId = item.EtapaCultoId,
                    Ordem = i + 1,
                    Responsavel = item.Responsavel,
                    Observacoes = item.Observacoes,
                    CriadoEm = DateTime.UtcNow
                });
            }

            await _db.SaveChangesAsync();
            return Ok(new { mensagem = "Repertório salvo com sucesso." });
        }

        [HttpPost("duplicar-culto-anterior")]
        [Authorize(Roles = "ADMIN,GESTAO_CULTO,LIDER_MINISTERIO")]
        public async Task<IActionResult> DuplicarCultoAnterior([FromBody] long cultoId)
        {
            var cultoAtual = await _db.Cultos.AsNoTracking().FirstOrDefaultAsync(x => x.Id == cultoId);
            if (cultoAtual == null)
            {
                return BadRequest(new { mensagem = "Culto inválido." });
            }

            var cultoAnterior = await _db.Cultos
                .AsNoTracking()
                .Where(x => x.DataCulto < cultoAtual.DataCulto)
                .OrderByDescending(x => x.DataCulto)
                .ThenByDescending(x => x.Id)
                .FirstOrDefaultAsync();

            if (cultoAnterior == null)
            {
                return BadRequest(new { mensagem = "Não há culto anterior com repertório." });
            }

            var repertorioAnterior = await _db.RepertoriosCulto.AsNoTracking().FirstOrDefaultAsync(x => x.CultoId == cultoAnterior.Id);
            if (repertorioAnterior == null)
            {
                return BadRequest(new { mensagem = "Culto anterior não possui repertório." });
            }

            var itens = await _db.RepertoriosCultoItens
                .AsNoTracking()
                .Where(x => x.RepertorioCultoId == repertorioAnterior.Id)
                .OrderBy(x => x.Ordem)
                .Select(x => new RepertorioItemRequest
                {
                    MusicaId = x.MusicaId,
                    MusicaTitulo = x.MusicaTitulo,
                    MusicaArtistaBanda = x.MusicaArtistaBanda,
                    MusicaTom = x.MusicaTom,
                    MusicaLinkCifra = x.MusicaLinkCifra,
                    MusicaLinkVideo = x.MusicaLinkVideo,
                    MusicaObservacoes = x.MusicaObservacoes,
                    EtapaCultoId = null,
                    Ordem = x.Ordem,
                    Responsavel = x.Responsavel,
                    Observacoes = x.Observacoes
                })
                .ToListAsync();

            return await Salvar(new RepertorioRequest
            {
                CultoId = cultoId,
                Observacoes = repertorioAnterior.Observacoes,
                Itens = itens
            });
        }
    }
}
