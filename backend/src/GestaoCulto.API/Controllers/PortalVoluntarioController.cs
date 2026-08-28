using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using GestaoCulto.Application.Interfaces;
using GestaoCulto.Domain.Entities;
using GestaoCulto.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GestaoCulto.API.Controllers
{
    public class VoluntarioMeusDadosRequest
    {
        public string Nome { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string? Telefone { get; set; }
        public string? Observacoes { get; set; }
        public string? RestricoesIndisponibilidade { get; set; }
        public string? NovaSenha { get; set; }
    }

    public class VoluntarioCompromissoDto
    {
        public long Id { get; set; }
        public long CultoId { get; set; }
        public string CultoNome { get; set; } = string.Empty;
        public DateTime DataCulto { get; set; }
        public TimeSpan HorarioInicio { get; set; }
        public long? MinisterioId { get; set; }
        public string? MinisterioNome { get; set; }
        public string Funcao { get; set; } = string.Empty;
        public long? EtapaCultoId { get; set; }
        public string? EtapaAtividade { get; set; }
        public string? EtapaBlocoCronograma { get; set; }
        public string? Observacoes { get; set; }
        public long PresencaStatusId { get; set; }
        public string? PresencaStatusNome { get; set; }
        public bool PodeGerenciarRepertorio { get; set; }
    }

    public class VoluntarioMinisterioResumoDto
    {
        public long MinisterioId { get; set; }
        public string MinisterioNome { get; set; } = string.Empty;
        public string MinisterioCodigo { get; set; } = string.Empty;
        public bool Principal { get; set; }
    }

    public class VoluntarioRepertorioListaItemDto
    {
        public long CultoId { get; set; }
        public string CultoNome { get; set; } = string.Empty;
        public DateTime DataCulto { get; set; }
        public string HorarioInicio { get; set; } = string.Empty;
        public string StatusCultoNome { get; set; } = string.Empty;
        public int TotalMusicas { get; set; }
        public bool TemRepertorio { get; set; }
        public bool PodeGerenciar { get; set; }
        public long? EscalaGerenciavelId { get; set; }
        public string? Observacoes { get; set; }
    }

    public class VoluntarioRepertorioDetalheDto
    {
        public long CultoId { get; set; }
        public string CultoNome { get; set; } = string.Empty;
        public DateTime DataCulto { get; set; }
        public string HorarioInicio { get; set; } = string.Empty;
        public string StatusCultoNome { get; set; } = string.Empty;
        public bool PodeVisualizar { get; set; }
        public bool PodeGerenciar { get; set; }
        public long? EscalaGerenciavelId { get; set; }
        public RepertorioLouvorDto? Repertorio { get; set; }
    }

    public class RepertorioLouvorDto
    {
        public long Id { get; set; }
        public long CultoId { get; set; }
        public string? Observacoes { get; set; }
        public List<RepertorioLouvorItemDto> Itens { get; set; } = new List<RepertorioLouvorItemDto>();
    }

    public class RepertorioLouvorItemDto
    {
        public long Id { get; set; }
        public long MusicaId { get; set; }
        public int Ordem { get; set; }
        public string? MusicaTitulo { get; set; }
        public string? MusicaArtistaBanda { get; set; }
        public string? MusicaTom { get; set; }
        public string? MusicaLinkCifra { get; set; }
        public string? MusicaLinkVideo { get; set; }
        public string? MusicaObservacoes { get; set; }
        public long? EtapaCultoId { get; set; }
        public string? EtapaAtividade { get; set; }
        public string? Responsavel { get; set; }
        public string? Observacoes { get; set; }
    }

    public class VoluntarioDisponibilidadeRequest
    {
        public bool Disponivel { get; set; }
        public List<long> MinisterioIds { get; set; } = new List<long>();
        public string? Observacao { get; set; }
    }

    [ApiController]
    [Authorize(Roles = "VOLUNTARIO")]
    [Route("api/portal-voluntario")]
    public class PortalVoluntarioController : ControllerBase
    {
        private readonly GestaoCultoDbContext _db;
        private readonly IPasswordHasher _passwordHasher;
        private readonly IDisponibilidadeVoluntarioService _disponibilidadeService;
        private readonly IRepertorioPermissionService _repertorioPermissionService;

        public PortalVoluntarioController(
            GestaoCultoDbContext db,
            IPasswordHasher passwordHasher,
            IDisponibilidadeVoluntarioService disponibilidadeService,
            IRepertorioPermissionService repertorioPermissionService)
        {
            _db = db;
            _passwordHasher = passwordHasher;
            _disponibilidadeService = disponibilidadeService;
            _repertorioPermissionService = repertorioPermissionService;
        }

        [HttpGet("escala/{escalaId:long}/repertorio")]
        public async Task<IActionResult> ObterRepertorioDaEscala(long escalaId)
        {
            var usuarioId = ObterUsuarioIdLogado();
            if (!usuarioId.HasValue || !await _repertorioPermissionService.PodeGerenciarAsync(usuarioId.Value, escalaId))
            {
                return Forbid();
            }

            var escala = await _db.Escalas.AsNoTracking().FirstOrDefaultAsync(x => x.Id == escalaId);
            if (escala == null) return NotFound(new { mensagem = "Escala não encontrada." });
            return await ObterRepertorio(escala.CultoId);
        }

        [HttpGet("repertorios")]
        public async Task<IActionResult> ListarRepertorios()
        {
            var usuarioId = ObterUsuarioIdLogado();
            if (!usuarioId.HasValue || !await _repertorioPermissionService.EhMembroLouvorAsync(usuarioId.Value))
            {
                return Forbid();
            }

            var cultos = await _db.Cultos
                .AsNoTracking()
                .Join(_db.StatusCultos.AsNoTracking(),
                    culto => culto.StatusCultoId,
                    status => status.Id,
                    (culto, status) => new
                    {
                        culto.Id,
                        culto.Nome,
                        culto.DataCulto,
                        culto.HorarioInicio,
                        StatusCultoNome = status.Nome,
                        StatusCultoCodigo = status.Codigo
                    })
                .ToListAsync();

            cultos = cultos
                .OrderByDescending(x => x.DataCulto >= DateTime.Today)
                .ThenBy(x => x.DataCulto >= DateTime.Today ? x.DataCulto : DateTime.MinValue)
                .ThenByDescending(x => x.DataCulto < DateTime.Today)
                .ThenByDescending(x => x.DataCulto)
                .ToList();

            var itens = new List<VoluntarioRepertorioListaItemDto>();
            foreach (var culto in cultos)
            {
                var repertorio = await _db.RepertoriosCulto.AsNoTracking().FirstOrDefaultAsync(x => x.CultoId == culto.Id);
                var totalMusicas = repertorio == null
                    ? 0
                    : await _db.RepertoriosCultoItens.AsNoTracking().CountAsync(x => x.RepertorioCultoId == repertorio.Id);
                var escalaGerenciavelId = await _repertorioPermissionService.ObterEscalaGerenciavelAsync(usuarioId.Value, culto.Id);
                itens.Add(new VoluntarioRepertorioListaItemDto
                {
                    CultoId = culto.Id,
                    CultoNome = culto.Nome,
                    DataCulto = culto.DataCulto,
                    HorarioInicio = culto.HorarioInicio.ToString(@"hh\:mm"),
                    StatusCultoNome = culto.StatusCultoNome,
                    TemRepertorio = repertorio != null,
                    TotalMusicas = totalMusicas,
                    PodeGerenciar = escalaGerenciavelId.HasValue,
                    EscalaGerenciavelId = escalaGerenciavelId,
                    Observacoes = repertorio?.Observacoes
                });
            }

            return Ok(itens);
        }

        [HttpGet("repertorios/culto/{cultoId:long}")]
        public async Task<IActionResult> ObterRepertorioDoCulto(long cultoId)
        {
            var usuarioId = ObterUsuarioIdLogado();
            if (!usuarioId.HasValue || !await _repertorioPermissionService.EhMembroLouvorAsync(usuarioId.Value))
            {
                return Forbid();
            }

            var culto = await _db.Cultos
                .AsNoTracking()
                .Join(_db.StatusCultos.AsNoTracking(),
                    c => c.StatusCultoId,
                    s => s.Id,
                    (c, s) => new { c, s })
                .Where(x => x.c.Id == cultoId)
                .Select(x => new
                {
                    x.c.Id,
                    x.c.Nome,
                    x.c.DataCulto,
                    x.c.HorarioInicio,
                    StatusCultoNome = x.s.Nome
                })
                .FirstOrDefaultAsync();

            if (culto == null)
            {
                return NotFound(new { mensagem = "Culto não encontrado." });
            }

            var escalaGerenciavelId = await _repertorioPermissionService.ObterEscalaGerenciavelAsync(usuarioId.Value, cultoId);
            var repertorio = await _db.RepertoriosCulto.AsNoTracking().FirstOrDefaultAsync(x => x.CultoId == cultoId);
            RepertorioLouvorDto? repertorioDto = null;
            if (repertorio != null)
            {
                var itens = await _db.RepertoriosCultoItens
                    .AsNoTracking()
                    .Where(x => x.RepertorioCultoId == repertorio.Id)
                    .OrderBy(x => x.Ordem)
                    .Select(x => new RepertorioLouvorItemDto
                    {
                        Id = x.Id,
                        MusicaId = x.MusicaId,
                        Ordem = x.Ordem,
                        MusicaTitulo = x.MusicaTitulo,
                        MusicaArtistaBanda = x.MusicaArtistaBanda,
                        MusicaTom = x.MusicaTom,
                        MusicaLinkCifra = x.MusicaLinkCifra,
                        MusicaLinkVideo = x.MusicaLinkVideo,
                        MusicaObservacoes = x.MusicaObservacoes,
                        EtapaCultoId = x.EtapaCultoId,
                        EtapaAtividade = _db.EtapasCulto.Where(e => e.Id == x.EtapaCultoId).Select(e => e.Atividade).FirstOrDefault(),
                        Responsavel = x.Responsavel,
                        Observacoes = x.Observacoes
                    })
                    .ToListAsync();

                repertorioDto = new RepertorioLouvorDto
                {
                    Id = repertorio.Id,
                    CultoId = repertorio.CultoId,
                    Observacoes = repertorio.Observacoes,
                    Itens = itens
                };
            }

            return Ok(new VoluntarioRepertorioDetalheDto
            {
                CultoId = culto.Id,
                CultoNome = culto.Nome,
                DataCulto = culto.DataCulto,
                HorarioInicio = culto.HorarioInicio.ToString(@"hh\:mm"),
                StatusCultoNome = culto.StatusCultoNome,
                PodeVisualizar = true,
                PodeGerenciar = escalaGerenciavelId.HasValue,
                EscalaGerenciavelId = escalaGerenciavelId,
                Repertorio = repertorioDto
            });
        }

        [HttpGet("repertorios/musicas")]
        public async Task<IActionResult> ListarMusicasLouvor([FromQuery] string? busca)
        {
            var usuarioId = ObterUsuarioIdLogado();
            if (!usuarioId.HasValue || !await _repertorioPermissionService.EhMembroLouvorAsync(usuarioId.Value))
            {
                return Forbid();
            }

            var query = _db.Musicas.AsNoTracking().Where(x => x.Ativo);
            if (!string.IsNullOrWhiteSpace(busca))
            {
                var termo = busca.Trim().ToLower();
                query = query.Where(x => x.Titulo.ToLower().Contains(termo) || x.ArtistaBanda.ToLower().Contains(termo));
            }

            return Ok(await query
                .OrderBy(x => x.Titulo)
                .Select(x => new { x.Id, x.Titulo, x.ArtistaBanda, x.Tom, x.LinkCifra, x.LinkVideo, x.Observacoes, x.Ativo })
                .ToListAsync());
        }

        [HttpGet("escala/{escalaId:long}/musicas")]
        public async Task<IActionResult> ListarMusicasDaEscala(long escalaId, [FromQuery] string? busca)
        {
            var usuarioId = ObterUsuarioIdLogado();
            if (!usuarioId.HasValue || !await _repertorioPermissionService.PodeGerenciarAsync(usuarioId.Value, escalaId)) return Forbid();
            var query = _db.Musicas.AsNoTracking().Where(x => x.Ativo);
            if (!string.IsNullOrWhiteSpace(busca))
            {
                var termo = busca.Trim().ToLower();
                query = query.Where(x => x.Titulo.ToLower().Contains(termo) || x.ArtistaBanda.ToLower().Contains(termo));
            }
            return Ok(await query.OrderBy(x => x.Titulo).Select(x => new { x.Id, x.Titulo, x.ArtistaBanda, x.Tom, x.LinkCifra, x.LinkVideo, x.Observacoes }).ToListAsync());
        }

        [HttpPost("escala/{escalaId:long}/musicas")]
        public async Task<IActionResult> CadastrarMusicaDaEscala(long escalaId, [FromBody] MusicaRequest request)
        {
            var usuarioId = ObterUsuarioIdLogado();
            if (!usuarioId.HasValue || !await _repertorioPermissionService.PodeGerenciarAsync(usuarioId.Value, escalaId)) return Forbid();
            var titulo = (request.Titulo ?? string.Empty).Trim();
            var artista = (request.ArtistaBanda ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(titulo) || string.IsNullOrWhiteSpace(artista)) return BadRequest(new { mensagem = "Informe título e artista/banda da música." });
            if (await _db.Musicas.AnyAsync(x => x.Titulo.ToLower() == titulo.ToLower() && x.ArtistaBanda.ToLower() == artista.ToLower())) return BadRequest(new { mensagem = "Música já cadastrada." });
            var musica = new Musica { Titulo = titulo, ArtistaBanda = artista, Tom = request.Tom, LinkCifra = request.LinkCifra, LinkVideo = request.LinkVideo, Observacoes = request.Observacoes, Ativo = true, CriadoEm = DateTime.UtcNow };
            _db.Musicas.Add(musica);
            await _db.SaveChangesAsync();
            return Ok(new { musica.Id, musica.Titulo, musica.ArtistaBanda, musica.Tom, musica.LinkCifra, musica.LinkVideo, musica.Observacoes });
        }

        [HttpPut("escala/{escalaId:long}/repertorio")]
        public async Task<IActionResult> SalvarRepertorioDaEscala(long escalaId, [FromBody] RepertorioRequest request)
        {
            var usuarioId = ObterUsuarioIdLogado();
            if (!usuarioId.HasValue || !await _repertorioPermissionService.PodeGerenciarAsync(usuarioId.Value, escalaId)) return Forbid();
            var escala = await _db.Escalas.FirstOrDefaultAsync(x => x.Id == escalaId);
            if (escala == null) return NotFound(new { mensagem = "Escala não encontrada." });
            if (request.CultoId != escala.CultoId) return BadRequest(new { mensagem = "O repertório deve pertencer ao culto da escala." });
            var repertorio = await _db.RepertoriosCulto.FirstOrDefaultAsync(x => x.CultoId == request.CultoId);
            if (repertorio == null) { repertorio = new RepertorioCulto { CultoId = request.CultoId, CriadoEm = DateTime.UtcNow }; _db.RepertoriosCulto.Add(repertorio); await _db.SaveChangesAsync(); }
            var itens = (request.Itens ?? new List<RepertorioItemRequest>()).Where(x => x.MusicaId > 0).OrderBy(x => x.Ordem).ToList();
            var musicaIds = itens.Select(x => x.MusicaId).Distinct().ToList();
            if (await _db.Musicas.CountAsync(x => musicaIds.Contains(x.Id) && x.Ativo) != musicaIds.Count) return BadRequest(new { mensagem = "Uma ou mais músicas não estão disponíveis." });
            var etapaIds = itens.Where(x => x.EtapaCultoId.HasValue).Select(x => x.EtapaCultoId!.Value).Distinct().ToList();
            if (etapaIds.Any() && await _db.EtapasCulto.CountAsync(x => etapaIds.Contains(x.Id) && x.CultoId == escala.CultoId) != etapaIds.Count)
            {
                return BadRequest(new { mensagem = "Uma ou mais etapas não pertencem ao culto." });
            }

            using var transacao = await _db.Database.BeginTransactionAsync();
            repertorio.Observacoes = request.Observacoes;
            repertorio.AtualizadoEm = DateTime.UtcNow;
            var atuais = await _db.RepertoriosCultoItens.Where(x => x.RepertorioCultoId == repertorio.Id).ToListAsync();
            _db.RepertoriosCultoItens.RemoveRange(atuais);
            for (var i = 0; i < itens.Count; i++) { var item = itens[i]; _db.RepertoriosCultoItens.Add(new RepertorioCultoItem { RepertorioCultoId = repertorio.Id, MusicaId = item.MusicaId, MusicaTitulo = item.MusicaTitulo, MusicaArtistaBanda = item.MusicaArtistaBanda, MusicaTom = item.MusicaTom, MusicaLinkCifra = item.MusicaLinkCifra, MusicaLinkVideo = item.MusicaLinkVideo, MusicaObservacoes = item.MusicaObservacoes, EtapaCultoId = item.EtapaCultoId, Ordem = i + 1, Responsavel = item.Responsavel, Observacoes = item.Observacoes, CriadoEm = DateTime.UtcNow }); }
            await _db.SaveChangesAsync();
            await transacao.CommitAsync();
            return Ok(new { mensagem = "Repertório salvo com sucesso." });
        }

        private async Task<IActionResult> ObterRepertorio(long cultoId)
        {
            var repertorio = await _db.RepertoriosCulto.AsNoTracking().FirstOrDefaultAsync(x => x.CultoId == cultoId);
            if (repertorio == null) return Ok(new { cultoId, observacoes = (string?)null, itens = new List<object>() });
            var itens = await _db.RepertoriosCultoItens.AsNoTracking().Where(x => x.RepertorioCultoId == repertorio.Id).OrderBy(x => x.Ordem).Select(x => new { x.Id, x.MusicaId, MusicaTitulo = _db.Musicas.Where(m => m.Id == x.MusicaId).Select(m => m.Titulo).FirstOrDefault(), MusicaArtistaBanda = _db.Musicas.Where(m => m.Id == x.MusicaId).Select(m => m.ArtistaBanda).FirstOrDefault(), x.MusicaTom, x.MusicaLinkCifra, x.MusicaLinkVideo, x.MusicaObservacoes, x.EtapaCultoId, x.Ordem, x.Responsavel, x.Observacoes }).ToListAsync();
            return Ok(new { repertorio.Id, repertorio.CultoId, repertorio.Observacoes, itens });
        }

        [HttpGet("painel")]
        public async Task<IActionResult> Painel()
        {
            var voluntario = await ObterVoluntarioLogado();
            if (voluntario == null)
            {
                return Unauthorized(new { mensagem = "Voluntário não vinculado ao usuário autenticado." });
            }

            var hoje = DateTime.Today;
            var compromissos = await ConsultarEscalas(voluntario.Id, hoje, hoje.AddMonths(3));
            var ministerios = await ListarMinisterios(voluntario.Id);

            return Ok(new
            {
                voluntario = new { voluntario.Id, voluntario.Nome, voluntario.Email, voluntario.Telefone },
                proximosCompromissos = compromissos.Take(5).ToList(),
                ministerios,
                totalProximosCompromissos = compromissos.Count,
                pendentesConfirmacao = compromissos.Count(x => x.PresencaStatusId == 1)
            });
        }

        [HttpGet("calendario")]
        public async Task<IActionResult> Calendario([FromQuery] int ano, [FromQuery] int mes)
        {
            if (ano < 2000 || ano > 2100 || mes < 1 || mes > 12)
            {
                return BadRequest(new { mensagem = "Parâmetros de calendário inválidos." });
            }

            var voluntario = await ObterVoluntarioLogado();
            if (voluntario == null)
            {
                return Unauthorized(new { mensagem = "Voluntário não vinculado ao usuário autenticado." });
            }

            var inicio = new DateTime(ano, mes, 1);
            var fim = inicio.AddMonths(1).AddDays(-1);
            var compromissos = await ConsultarEscalas(voluntario.Id, inicio, fim);

            var dias = compromissos
                .GroupBy(x => x.DataCulto.Date)
                .Select(g => new
                {
                    data = g.Key,
                    total = g.Count(),
                    itens = g.ToList()
                })
                .OrderBy(x => x.data)
                .ToList();

            return Ok(new { ano, mes, dias });
        }

        [HttpGet("cultos-planejados")]
        public async Task<IActionResult> CultosPlanejados([FromQuery] int ano, [FromQuery] int mes)
        {
            if (ano < 2000 || ano > 2100 || mes < 1 || mes > 12)
            {
                return BadRequest(new { mensagem = "Parâmetros inválidos para consulta dos cultos." });
            }

            var voluntario = await ObterVoluntarioLogado();
            if (voluntario == null)
            {
                return Unauthorized(new { mensagem = "Voluntário não vinculado ao usuário autenticado." });
            }

            var inicio = new DateTime(ano, mes, 1);
            var fim = inicio.AddMonths(1).AddDays(-1);
            var statusCultoAtivoId = await ObterStatusCultoAtivoId();

            var cultos = await _db.Cultos
                .AsNoTracking()
                .Where(x => x.DataCulto >= inicio && x.DataCulto <= fim && x.StatusCultoId == statusCultoAtivoId)
                .OrderBy(x => x.DataCulto)
                .ThenBy(x => x.HorarioInicio)
                .Select(x => new
                {
                    x.Id,
                    x.Nome,
                    x.DataCulto,
                    x.HorarioInicio,
                    x.StatusCultoId,
                    StatusCultoNome = _db.StatusCultos.Where(s => s.Id == x.StatusCultoId).Select(s => s.Nome).FirstOrDefault()
                })
                .ToListAsync();

            var cultoIds = cultos.Select(x => x.Id).ToList();
            var disponibilidades = await _db.DisponibilidadesCultoVoluntarios
                .AsNoTracking()
                .Where(x => x.VoluntarioId == voluntario.Id && cultoIds.Contains(x.CultoId))
                .Select(x => new
                {
                    x.Id,
                    x.CultoId,
                    x.StatusDisponibilidadeId,
                    StatusCodigo = _db.StatusDisponibilidadeVoluntarios.Where(s => s.Id == x.StatusDisponibilidadeId).Select(s => s.Codigo).FirstOrDefault(),
                    StatusNome = _db.StatusDisponibilidadeVoluntarios.Where(s => s.Id == x.StatusDisponibilidadeId).Select(s => s.Nome).FirstOrDefault(),
                    x.Observacao,
                    x.RespondidoEm
                })
                .ToListAsync();

            var disponibilidadeIds = disponibilidades.Select(x => x.Id).ToList();
            var ministeriosDisponibilidade = await _db.DisponibilidadesCultoVoluntariosMinisterios
                .AsNoTracking()
                .Where(x => disponibilidadeIds.Contains(x.DisponibilidadeCultoVoluntarioId))
                .Select(x => new
                {
                    x.DisponibilidadeCultoVoluntarioId,
                    x.MinisterioId,
                    MinisterioNome = _db.Ministerios.Where(m => m.Id == x.MinisterioId).Select(m => m.Nome).FirstOrDefault()
                })
                .ToListAsync();

            var escalasCultoIds = await _db.Escalas
                .AsNoTracking()
                .Where(x => x.VoluntarioId == voluntario.Id && cultoIds.Contains(x.CultoId))
                .Select(x => x.CultoId)
                .Distinct()
                .ToListAsync();

            var hoje = DateTime.Today;
            var resposta = cultos.Select(culto =>
            {
                var disp = disponibilidades.FirstOrDefault(x => x.CultoId == culto.Id);
                var ministerios = disp == null
                    ? new List<object>()
                    : ministeriosDisponibilidade
                        .Where(x => x.DisponibilidadeCultoVoluntarioId == disp.Id)
                        .Select(x => (object)new { x.MinisterioId, x.MinisterioNome })
                        .ToList();
                var escalado = escalasCultoIds.Contains(culto.Id);

                var statusCodigo = escalado
                    ? "ESCALADO"
                    : disp?.StatusCodigo ?? "NAO_RESPONDIDO";

                var statusNome = escalado
                    ? "Escalado"
                    : disp?.StatusNome ?? "Não respondido";

                return new
                {
                    culto.Id,
                    culto.Nome,
                    culto.DataCulto,
                    culto.HorarioInicio,
                    culto.StatusCultoId,
                    culto.StatusCultoNome,
                    statusDisponibilidadeCodigo = statusCodigo,
                    statusDisponibilidadeNome = statusNome,
                    respondidoEm = disp?.RespondidoEm,
                    observacao = disp?.Observacao,
                    ministerios,
                    escalado,
                    podeResponder = culto.DataCulto.Date >= hoje
                };
            }).ToList();

            return Ok(new { ano, mes, cultos = resposta });
        }

        [HttpGet("culto/{cultoId:long}/disponibilidade")]
        public async Task<IActionResult> MinhaDisponibilidadePorCulto(long cultoId)
        {
            var voluntario = await ObterVoluntarioLogado();
            if (voluntario == null)
            {
                return Unauthorized(new { mensagem = "Voluntário não vinculado ao usuário autenticado." });
            }

            var statusCultoAtivoId = await ObterStatusCultoAtivoId();
            var culto = await _db.Cultos.AsNoTracking().FirstOrDefaultAsync(x => x.Id == cultoId && x.StatusCultoId == statusCultoAtivoId);
            if (culto == null)
            {
                return NotFound(new { mensagem = "Culto não encontrado." });
            }

            var ministeriosPermitidos = await _db.MinisteriosVoluntarios
                .AsNoTracking()
                .Where(x => x.VoluntarioId == voluntario.Id)
                .Select(x => new
                {
                    x.MinisterioId,
                    MinisterioNome = _db.Ministerios.Where(m => m.Id == x.MinisterioId).Select(m => m.Nome).FirstOrDefault(),
                    x.Principal
                })
                .OrderByDescending(x => x.Principal)
                .ThenBy(x => x.MinisterioNome)
                .ToListAsync();

            var disponibilidade = await _db.DisponibilidadesCultoVoluntarios
                .AsNoTracking()
                .Where(x => x.CultoId == cultoId && x.VoluntarioId == voluntario.Id)
                .Select(x => new
                {
                    x.Id,
                    x.StatusDisponibilidadeId,
                    StatusCodigo = _db.StatusDisponibilidadeVoluntarios.Where(s => s.Id == x.StatusDisponibilidadeId).Select(s => s.Codigo).FirstOrDefault(),
                    StatusNome = _db.StatusDisponibilidadeVoluntarios.Where(s => s.Id == x.StatusDisponibilidadeId).Select(s => s.Nome).FirstOrDefault(),
                    x.Observacao,
                    x.RespondidoEm
                })
                .FirstOrDefaultAsync();

            var ministerioIdsSelecionados = disponibilidade == null
                ? new List<long>()
                : await _db.DisponibilidadesCultoVoluntariosMinisterios
                    .AsNoTracking()
                    .Where(x => x.DisponibilidadeCultoVoluntarioId == disponibilidade.Id)
                    .Select(x => x.MinisterioId)
                    .ToListAsync();

            return Ok(new
            {
                culto = new { culto.Id, culto.Nome, culto.DataCulto, culto.HorarioInicio },
                podeResponder = culto.DataCulto.Date >= DateTime.Today,
                ministeriosPermitidos,
                disponibilidade = disponibilidade == null
                    ? null
                    : new
                    {
                        disponibilidade.StatusDisponibilidadeId,
                        disponibilidade.StatusCodigo,
                        disponibilidade.StatusNome,
                        disponibilidade.Observacao,
                        disponibilidade.RespondidoEm,
                        ministerioIds = ministerioIdsSelecionados
                    }
            });
        }

        [HttpPut("culto/{cultoId:long}/disponibilidade")]
        public async Task<IActionResult> InformarDisponibilidade(long cultoId, [FromBody] VoluntarioDisponibilidadeRequest request)
        {
            var voluntario = await ObterVoluntarioLogado();
            if (voluntario == null)
            {
                return Unauthorized(new { mensagem = "Voluntário não vinculado ao usuário autenticado." });
            }

            var ministerioIds = (request.MinisterioIds ?? new List<long>())
                .Where(x => x > 0)
                .Distinct()
                .ToList();
            var statusCultoAtivoId = await ObterStatusCultoAtivoId();
            var cultoExiste = await _db.Cultos
                .AsNoTracking()
                .AnyAsync(x => x.Id == cultoId && x.StatusCultoId == statusCultoAtivoId);
            if (!cultoExiste)
            {
                return NotFound(new { mensagem = "Culto não encontrado." });
            }

            await _disponibilidadeService.SalvarAsync(
                voluntario.Id,
                cultoId,
                request.Disponivel,
                ministerioIds,
                request.Observacao);

            return Ok(new { mensagem = "Disponibilidade registrada com sucesso." });
        }

        [HttpGet("minha-escala")]
        public async Task<IActionResult> MinhaEscala([FromQuery] DateTime? inicio, [FromQuery] DateTime? fim)
        {
            var voluntario = await ObterVoluntarioLogado();
            if (voluntario == null)
            {
                return Unauthorized(new { mensagem = "Voluntário não vinculado ao usuário autenticado." });
            }

            var dataInicio = inicio?.Date ?? DateTime.Today.AddMonths(-1);
            var dataFim = fim?.Date ?? DateTime.Today.AddMonths(3);
            if (dataFim < dataInicio)
            {
                return BadRequest(new { mensagem = "Período inválido." });
            }

            var compromissos = await ConsultarEscalas(voluntario.Id, dataInicio, dataFim);
            return Ok(compromissos);
        }

        [HttpGet("minha-escala/{escalaId:long}/detalhe")]
        public async Task<IActionResult> DetalheEscala(long escalaId)
        {
            var voluntario = await ObterVoluntarioLogado();
            if (voluntario == null)
            {
                return Unauthorized(new { mensagem = "Voluntário não vinculado ao usuário autenticado." });
            }

            var item = await ConsultarEscalas(voluntario.Id, DateTime.MinValue, DateTime.MaxValue)
                .ContinueWith(task => task.Result.FirstOrDefault(x => x.Id == escalaId));

            if (item == null)
            {
                return NotFound(new { mensagem = "Escala não encontrada para o voluntário." });
            }

            var meusMinisterios = await ListarMinisterioIds(voluntario.Id);

            var colegas = await _db.Escalas
                .AsNoTracking()
                .Where(x => x.CultoId == item.CultoId
                            && x.VoluntarioId != voluntario.Id
                            && x.MinisterioId.HasValue
                            && meusMinisterios.Contains(x.MinisterioId.Value))
                .OrderBy(x => x.MinisterioId)
                .ThenBy(x => x.VoluntarioId)
                .Select(x => new
                {
                    x.VoluntarioId,
                    VoluntarioNome = _db.Voluntarios.Where(v => v.Id == x.VoluntarioId).Select(v => v.Nome).FirstOrDefault(),
                    x.MinisterioId,
                    MinisterioNome = _db.Ministerios.Where(m => m.Id == x.MinisterioId).Select(m => m.Nome).FirstOrDefault(),
                    x.Funcao,
                    x.PresencaStatusId,
                    PresencaStatusNome = _db.PresencaEscalaStatus.Where(s => s.Id == x.PresencaStatusId).Select(s => s.Nome).FirstOrDefault()
                })
                .ToListAsync();

            object? repertorioLouvor = null;
            var ehLouvor = await VoluntarioEhMinisterioLouvor(voluntario.Id);
            if (ehLouvor)
            {
                var repertorio = await _db.RepertoriosCulto
                    .AsNoTracking()
                    .Where(x => x.CultoId == item.CultoId)
                    .Select(x => new
                    {
                        x.Id,
                        x.CultoId,
                        x.Observacoes
                    })
                    .FirstOrDefaultAsync();

                if (repertorio != null)
                {
                    var itens = await _db.RepertoriosCultoItens
                        .AsNoTracking()
                        .Where(x => x.RepertorioCultoId == repertorio.Id)
                        .OrderBy(x => x.Ordem)
                        .ThenBy(x => x.Id)
                        .Select(x => new
                        {
                            x.Id,
                            x.Ordem,
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
                            x.Responsavel,
                            x.Observacoes
                        })
                        .ToListAsync();

                    repertorioLouvor = new
                    {
                        repertorio.Id,
                        repertorio.CultoId,
                        repertorio.Observacoes,
                        Itens = itens
                    };
                }
            }

            return Ok(new { item, colegasMesmoMinisterio = colegas, repertorioLouvor });
        }

        [HttpPost("minha-escala/{escalaId:long}/confirmar")]
        public async Task<IActionResult> ConfirmarMinhaPresenca(long escalaId)
        {
            var voluntario = await ObterVoluntarioLogado();
            if (voluntario == null)
            {
                return Unauthorized(new { mensagem = "Voluntário não vinculado ao usuário autenticado." });
            }

            var escala = await _db.Escalas.FirstOrDefaultAsync(x => x.Id == escalaId && x.VoluntarioId == voluntario.Id);
            if (escala == null)
            {
                return NotFound(new { mensagem = "Escala não encontrada para o voluntário." });
            }

            escala.PresencaStatusId = 2;
            escala.ConfirmadoEm = DateTime.UtcNow;
            escala.AtualizadoEm = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            return Ok(new { mensagem = "Presença confirmada com sucesso." });
        }

        [HttpGet("meus-ministerios")]
        public async Task<IActionResult> MeusMinisterios()
        {
            var voluntario = await ObterVoluntarioLogado();
            if (voluntario == null)
            {
                return Unauthorized(new { mensagem = "Voluntário não vinculado ao usuário autenticado." });
            }

            return Ok(await ListarMinisterios(voluntario.Id));
        }

        [HttpGet("colegas-ministerio")]
        public async Task<IActionResult> ColegasMinisterio()
        {
            var voluntario = await ObterVoluntarioLogado();
            if (voluntario == null)
            {
                return Unauthorized(new { mensagem = "Voluntário não vinculado ao usuário autenticado." });
            }

            var ministerioIds = await ListarMinisterioIds(voluntario.Id);
            if (!ministerioIds.Any())
            {
                return Ok(Array.Empty<object>());
            }

            var colegas = await _db.MinisteriosVoluntarios
                .AsNoTracking()
                .Where(x => ministerioIds.Contains(x.MinisterioId) && x.VoluntarioId != voluntario.Id)
                .OrderBy(x => x.MinisterioId)
                .ThenBy(x => x.VoluntarioId)
                .Select(x => new
                {
                    x.VoluntarioId,
                    VoluntarioNome = _db.Voluntarios.Where(v => v.Id == x.VoluntarioId).Select(v => v.Nome).FirstOrDefault(),
                    VoluntarioTelefone = _db.Voluntarios.Where(v => v.Id == x.VoluntarioId).Select(v => v.Telefone).FirstOrDefault(),
                    VoluntarioEmail = _db.Voluntarios.Where(v => v.Id == x.VoluntarioId).Select(v => v.Email).FirstOrDefault(),
                    x.MinisterioId,
                    MinisterioNome = _db.Ministerios.Where(m => m.Id == x.MinisterioId).Select(m => m.Nome).FirstOrDefault(),
                    Ativo = _db.Voluntarios.Where(v => v.Id == x.VoluntarioId).Select(v => v.Ativo).FirstOrDefault()
                })
                .ToListAsync();

            return Ok(colegas);
        }

        [HttpGet("meus-dados")]
        public async Task<IActionResult> MeusDados()
        {
            var voluntario = await ObterVoluntarioLogado();
            if (voluntario == null)
            {
                return Unauthorized(new { mensagem = "Voluntário não vinculado ao usuário autenticado." });
            }

            return Ok(new
            {
                voluntario.Id,
                voluntario.Nome,
                voluntario.Email,
                voluntario.Telefone,
                voluntario.Observacoes,
                voluntario.RestricoesIndisponibilidade
            });
        }

        [HttpPut("meus-dados")]
        public async Task<IActionResult> AtualizarMeusDados([FromBody] VoluntarioMeusDadosRequest request)
        {
            var voluntario = await ObterVoluntarioLogado();
            if (voluntario == null)
            {
                return Unauthorized(new { mensagem = "Voluntário não vinculado ao usuário autenticado." });
            }

            var nome = LimparTexto(request.Nome);
            var email = NormalizarEmail(request.Email);
            if (string.IsNullOrWhiteSpace(nome) || string.IsNullOrWhiteSpace(email))
            {
                return BadRequest(new { mensagem = "Nome e e-mail são obrigatórios." });
            }

            var usuario = voluntario.UsuarioId.HasValue
                ? await _db.Usuarios.FirstOrDefaultAsync(x => x.Id == voluntario.UsuarioId.Value)
                : null;

            if (usuario == null)
            {
                return BadRequest(new { mensagem = "Usuário vinculado não encontrado." });
            }

            var emailJaExiste = await _db.Usuarios.AnyAsync(x => x.Email == email && x.Id != usuario.Id);
            if (emailJaExiste)
            {
                return BadRequest(new { mensagem = "Este e-mail já está em uso." });
            }

            voluntario.Nome = nome;
            voluntario.Email = email;
            voluntario.Telefone = LimparTexto(request.Telefone);
            voluntario.Observacoes = LimparTexto(request.Observacoes);
            voluntario.RestricoesIndisponibilidade = LimparTexto(request.RestricoesIndisponibilidade);
            voluntario.AtualizadoEm = DateTime.UtcNow;

            usuario.Nome = nome;
            usuario.Email = email;
            usuario.Telefone = LimparTexto(request.Telefone);
            usuario.AtualizadoEm = DateTime.UtcNow;

            if (!string.IsNullOrWhiteSpace(request.NovaSenha) && request.NovaSenha.Trim().Length >= 6)
            {
                usuario.SenhaHash = _passwordHasher.Hash(request.NovaSenha.Trim());
            }

            await _db.SaveChangesAsync();
            return Ok(new { mensagem = "Dados atualizados com sucesso." });
        }

        private async Task<List<VoluntarioCompromissoDto>> ConsultarEscalas(long voluntarioId, DateTime inicio, DateTime fim)
        {
            var statusCultoAtivoId = await ObterStatusCultoAtivoId();

            return await _db.Escalas
                .AsNoTracking()
                .Where(x => x.VoluntarioId == voluntarioId)
                .Select(x => new VoluntarioCompromissoDto
                {
                    Id = x.Id,
                    CultoId = x.CultoId,
                    CultoNome = _db.Cultos.Where(c => c.Id == x.CultoId).Select(c => c.Nome).FirstOrDefault() ?? string.Empty,
                    DataCulto = _db.Cultos.Where(c => c.Id == x.CultoId).Select(c => c.DataCulto).FirstOrDefault(),
                    HorarioInicio = _db.Cultos.Where(c => c.Id == x.CultoId).Select(c => c.HorarioInicio).FirstOrDefault(),
                    MinisterioId = x.MinisterioId,
                    MinisterioNome = _db.Ministerios.Where(m => m.Id == x.MinisterioId).Select(m => m.Nome).FirstOrDefault(),
                    Funcao = x.Funcao,
                    EtapaCultoId = x.EtapaCultoId,
                    EtapaAtividade = _db.EtapasCulto.Where(e => e.Id == x.EtapaCultoId).Select(e => e.Atividade).FirstOrDefault(),
                    EtapaBlocoCronograma = _db.EtapasCulto.Where(e => e.Id == x.EtapaCultoId).Select(e => e.BlocoCronograma).FirstOrDefault(),
                    Observacoes = x.Observacoes,
                     PresencaStatusId = x.PresencaStatusId,
                     PodeGerenciarRepertorio = x.PodeGerenciarRepertorio,
                     PresencaStatusNome = _db.PresencaEscalaStatus.Where(s => s.Id == x.PresencaStatusId).Select(s => s.Nome).FirstOrDefault()
                })
                .Where(x => x.DataCulto >= inicio && x.DataCulto <= fim
                    && _db.Cultos.Where(c => c.Id == x.CultoId).Select(c => c.StatusCultoId).FirstOrDefault() == statusCultoAtivoId)
                .OrderBy(x => x.DataCulto)
                .ThenBy(x => x.HorarioInicio)
                .ToListAsync();
        }

        private async Task<long> ObterStatusCultoAtivoId()
        {
            var statusAtivoId = await _db.StatusCultos
                .Where(x => x.Codigo == "ATIVO")
                .Select(x => x.Id)
                .FirstOrDefaultAsync();

            return statusAtivoId > 0
                ? statusAtivoId
                : await _db.StatusCultos.OrderBy(x => x.Ordem).Select(x => x.Id).FirstOrDefaultAsync();
        }

        private async Task<List<VoluntarioMinisterioResumoDto>> ListarMinisterios(long voluntarioId)
        {
            return await _db.MinisteriosVoluntarios
                .AsNoTracking()
                .Where(x => x.VoluntarioId == voluntarioId)
                .OrderByDescending(x => x.Principal)
                .ThenBy(x => x.MinisterioId)
                .Select(x => new VoluntarioMinisterioResumoDto
                {
                    MinisterioId = x.MinisterioId,
                    MinisterioNome = _db.Ministerios.Where(m => m.Id == x.MinisterioId).Select(m => m.Nome).FirstOrDefault() ?? string.Empty,
                    MinisterioCodigo = _db.Ministerios.Where(m => m.Id == x.MinisterioId).Select(m => m.Codigo).FirstOrDefault() ?? string.Empty,
                    Principal = x.Principal
                })
                .ToListAsync();
        }

        private async Task<List<long>> ListarMinisterioIds(long voluntarioId)
        {
            return await _db.MinisteriosVoluntarios
                .AsNoTracking()
                .Where(x => x.VoluntarioId == voluntarioId)
                .Select(x => x.MinisterioId)
                .Distinct()
                .ToListAsync();
        }

        private async Task<bool> VoluntarioEhMinisterioLouvor(long voluntarioId)
        {
            return await _db.MinisteriosVoluntarios
                .AsNoTracking()
                .Where(x => x.VoluntarioId == voluntarioId)
                .Join(_db.Ministerios,
                    vm => vm.MinisterioId,
                    m => m.Id,
                    (vm, m) => m.Codigo)
                .AnyAsync(codigo => codigo == "LOUVOR");
        }

        private async Task<Voluntario?> ObterVoluntarioLogado()
        {
            var usuarioId = ObterUsuarioIdLogado();
            if (!usuarioId.HasValue)
            {
                return null;
            }

            return await _db.Voluntarios.FirstOrDefaultAsync(x => x.UsuarioId == usuarioId.Value && x.Ativo);
        }

        private long? ObterUsuarioIdLogado()
        {
            var sub = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
            return long.TryParse(sub, out var id) ? id : (long?)null;
        }

        private static string NormalizarEmail(string? email)
        {
            return (email ?? string.Empty).Trim().ToLowerInvariant();
        }

        private static string? LimparTexto(string? valor)
        {
            if (string.IsNullOrWhiteSpace(valor))
            {
                return null;
            }

            return valor.Trim();
        }
    }
}
