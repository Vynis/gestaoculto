using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using GestaoCulto.Domain.Entities;
using GestaoCulto.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GestaoCulto.API.Controllers
{
    public class EscalaRequest
    {
        public long CultoId { get; set; }
        public long? EtapaCultoId { get; set; }
        public List<long>? EtapaCultoIds { get; set; }
        public long? VoluntarioId { get; set; }
        public string? VoluntarioAvulsoNome { get; set; }
        public string? VoluntarioAvulsoTelefone { get; set; }
        public long? MinisterioId { get; set; }
        public string Funcao { get; set; } = string.Empty;
        public long PresencaStatusId { get; set; }
        public string? Observacoes { get; set; }
    }

    [ApiController]
    [Authorize]
    [Route("api/escalas")]
    public class EscalasController : ControllerBase
    {
        private readonly GestaoCultoDbContext _db;

        public EscalasController(GestaoCultoDbContext db)
        {
            _db = db;
        }

        [HttpGet("culto/{cultoId:long}")]
        public async Task<IActionResult> ListarPorCulto(long cultoId)
        {
            var lista = await _db.Escalas
                .AsNoTracking()
                .Where(e => e.CultoId == cultoId)
                .OrderBy(e => e.HorarioPrevisto)
                .Select(e => new
                {
                    e.Id,
                    e.CultoId,
                    e.EtapaCultoId,
                    e.BlocoCronograma,
                    e.VoluntarioId,
                    VoluntarioAvulsoNome = e.VoluntarioAvulsoNome,
                    VoluntarioAvulsoTelefone = e.VoluntarioAvulsoTelefone,
                    VoluntarioNome = e.VoluntarioId.HasValue
                        ? _db.Voluntarios.Where(v => v.Id == e.VoluntarioId.Value).Select(v => v.Nome).FirstOrDefault()
                        : e.VoluntarioAvulsoNome,
                    e.MinisterioId,
                    MinisterioNome = _db.Ministerios.Where(m => m.Id == e.MinisterioId).Select(m => m.Nome).FirstOrDefault(),
                    e.Funcao,
                    e.HorarioPrevisto,
                    e.PresencaStatusId,
                    e.PodeGerenciarRepertorio,
                    PresencaStatusNome = _db.PresencaEscalaStatus.Where(s => s.Id == e.PresencaStatusId).Select(s => s.Nome).FirstOrDefault(),
                    e.ConfirmadoEm,
                    e.Observacoes
                })
                .ToListAsync();

            return Ok(lista);
        }

        [HttpPost]
        [Authorize(Roles = "ADMIN,GESTAO_CULTO,LIDER_MINISTERIO")]
        public async Task<IActionResult> Criar([FromBody] EscalaRequest dto)
        {
            var funcaoNormalizada = (dto.Funcao ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(funcaoNormalizada))
            {
                return BadRequest(new { mensagem = "Informe a função da escala." });
            }

            var erroFuncao = await ValidarFuncaoPorMinisterio(dto.MinisterioId, funcaoNormalizada);
            if (!string.IsNullOrWhiteSpace(erroFuncao))
            {
                return BadRequest(new { mensagem = erroFuncao });
            }

            var erroMinisterio = await ValidarMinisterioPermitidoParaLider(dto.MinisterioId);
            if (!string.IsNullOrWhiteSpace(erroMinisterio))
            {
                return Forbid();
            }

            var erroEtapa = await ValidarEtapaDoCulto(dto.CultoId, dto.EtapaCultoId);
            if (!string.IsNullOrWhiteSpace(erroEtapa))
            {
                return BadRequest(new { mensagem = erroEtapa });
            }

            var blocoCronograma = await ObterBlocoCronogramaDaFuncao(dto.MinisterioId, funcaoNormalizada);
            var podeGerenciarRepertorio = await PodeGerenciarRepertorioDaFuncao(dto.MinisterioId, funcaoNormalizada);

            var voluntarioIdNormalizado = dto.VoluntarioId.HasValue && dto.VoluntarioId.Value > 0
                ? dto.VoluntarioId
                : null;
            var voluntarioAvulsoNome = (dto.VoluntarioAvulsoNome ?? string.Empty).Trim();
            var voluntarioAvulsoTelefone = (dto.VoluntarioAvulsoTelefone ?? string.Empty).Trim();

            if (!voluntarioIdNormalizado.HasValue && string.IsNullOrWhiteSpace(voluntarioAvulsoNome))
            {
                return BadRequest(new { mensagem = "Selecione um voluntário cadastrado ou informe o nome do voluntário avulso." });
            }

            if (voluntarioIdNormalizado.HasValue)
            {
                voluntarioAvulsoNome = string.Empty;
                voluntarioAvulsoTelefone = string.Empty;
            }

            var etapaIds = (dto.EtapaCultoIds ?? new List<long>())
                .Where(x => x > 0)
                .Distinct()
                .ToList();

            if (!etapaIds.Any() && dto.EtapaCultoId.HasValue && dto.EtapaCultoId.Value > 0)
            {
                etapaIds.Add(dto.EtapaCultoId.Value);
            }

            if (!etapaIds.Any())
            {
                etapaIds.Add(0);
            }

            var entities = etapaIds.Select(etapaId => new Escala
            {
                CultoId = dto.CultoId,
                EtapaCultoId = etapaId > 0 ? etapaId : (long?)null,
                VoluntarioId = voluntarioIdNormalizado,
                VoluntarioAvulsoNome = string.IsNullOrWhiteSpace(voluntarioAvulsoNome) ? null : voluntarioAvulsoNome,
                VoluntarioAvulsoTelefone = string.IsNullOrWhiteSpace(voluntarioAvulsoTelefone) ? null : voluntarioAvulsoTelefone,
                MinisterioId = dto.MinisterioId,
                Funcao = funcaoNormalizada,
                BlocoCronograma = blocoCronograma,
                PodeGerenciarRepertorio = podeGerenciarRepertorio,
                HorarioPrevisto = null,
                PresencaStatusId = dto.PresencaStatusId,
                Observacoes = dto.Observacoes,
                CriadoEm = DateTime.UtcNow
            }).ToList();

            _db.Escalas.AddRange(entities);
            await _db.SaveChangesAsync();

            if (entities.Count == 1)
            {
                return Ok(entities[0]);
            }

            return Ok(new
            {
                mensagem = $"{entities.Count} escalas criadas com sucesso.",
                quantidade = entities.Count
            });
        }

        [HttpPut("{id:long}")]
        [Authorize(Roles = "ADMIN,GESTAO_CULTO,LIDER_MINISTERIO")]
        public async Task<IActionResult> Atualizar(long id, [FromBody] EscalaRequest dto)
        {
            var entity = await _db.Escalas.FirstOrDefaultAsync(x => x.Id == id);
            if (entity == null)
            {
                return NotFound(new { mensagem = "Escala não encontrada." });
            }

            var funcaoNormalizada = (dto.Funcao ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(funcaoNormalizada))
            {
                return BadRequest(new { mensagem = "Informe a função da escala." });
            }

            var erroFuncao = await ValidarFuncaoPorMinisterio(dto.MinisterioId, funcaoNormalizada);
            if (!string.IsNullOrWhiteSpace(erroFuncao))
            {
                return BadRequest(new { mensagem = erroFuncao });
            }

            var erroMinisterio = await ValidarMinisterioPermitidoParaLider(dto.MinisterioId);
            if (!string.IsNullOrWhiteSpace(erroMinisterio))
            {
                return Forbid();
            }

            var erroEtapa = await ValidarEtapaDoCulto(dto.CultoId, dto.EtapaCultoId);
            if (!string.IsNullOrWhiteSpace(erroEtapa))
            {
                return BadRequest(new { mensagem = erroEtapa });
            }

            var blocoCronograma = await ObterBlocoCronogramaDaFuncao(dto.MinisterioId, funcaoNormalizada);

            var voluntarioIdNormalizado = dto.VoluntarioId.HasValue && dto.VoluntarioId.Value > 0
                ? dto.VoluntarioId
                : null;
            var voluntarioAvulsoNome = (dto.VoluntarioAvulsoNome ?? string.Empty).Trim();
            var voluntarioAvulsoTelefone = (dto.VoluntarioAvulsoTelefone ?? string.Empty).Trim();

            if (!voluntarioIdNormalizado.HasValue && string.IsNullOrWhiteSpace(voluntarioAvulsoNome))
            {
                return BadRequest(new { mensagem = "Selecione um voluntário cadastrado ou informe o nome do voluntário avulso." });
            }

            if (voluntarioIdNormalizado.HasValue)
            {
                voluntarioAvulsoNome = string.Empty;
                voluntarioAvulsoTelefone = string.Empty;
            }

            entity.CultoId = dto.CultoId;
            entity.EtapaCultoId = dto.EtapaCultoId;
            entity.VoluntarioId = voluntarioIdNormalizado;
            entity.VoluntarioAvulsoNome = string.IsNullOrWhiteSpace(voluntarioAvulsoNome) ? null : voluntarioAvulsoNome;
            entity.VoluntarioAvulsoTelefone = string.IsNullOrWhiteSpace(voluntarioAvulsoTelefone) ? null : voluntarioAvulsoTelefone;
            entity.MinisterioId = dto.MinisterioId;
            entity.Funcao = funcaoNormalizada;
            entity.BlocoCronograma = blocoCronograma;
            entity.PodeGerenciarRepertorio = await PodeGerenciarRepertorioDaFuncao(dto.MinisterioId, funcaoNormalizada);
            entity.PresencaStatusId = dto.PresencaStatusId;
            entity.Observacoes = dto.Observacoes;
            entity.AtualizadoEm = DateTime.UtcNow;

            await _db.SaveChangesAsync();
            return Ok(entity);
        }

        [HttpPost("{id:long}/confirmar")]
        [Authorize(Roles = "ADMIN,GESTAO_CULTO,LIDER_MINISTERIO,VOLUNTARIO")]
        public async Task<IActionResult> ConfirmarPresenca(long id)
        {
            var escala = await _db.Escalas.FirstOrDefaultAsync(x => x.Id == id);
            if (escala == null)
            {
                return NotFound(new { mensagem = "Escala não encontrada." });
            }

            if (User.IsInRole("VOLUNTARIO"))
            {
                var sub = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
                if (!long.TryParse(sub, out var usuarioId))
                {
                    return Unauthorized(new { mensagem = "Usuário inválido para confirmar presença." });
                }

                var voluntario = await _db.Voluntarios
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x => x.UsuarioId == usuarioId && x.Ativo);

                if (voluntario == null || voluntario.Id != escala.VoluntarioId)
                {
                    return Forbid();
                }
            }

            escala.PresencaStatusId = 2;
            escala.ConfirmadoEm = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            return Ok(new { mensagem = "Presença confirmada com sucesso." });
        }

        [HttpPost("{id:long}/cancelar-confirmacao")]
        [Authorize(Roles = "ADMIN,GESTAO_CULTO,LIDER_MINISTERIO")]
        public async Task<IActionResult> CancelarConfirmacao(long id)
        {
            var escala = await _db.Escalas.FirstOrDefaultAsync(x => x.Id == id);
            if (escala == null)
            {
                return NotFound(new { mensagem = "Escala não encontrada." });
            }

            escala.PresencaStatusId = 1;
            escala.ConfirmadoEm = null;
            escala.AtualizadoEm = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            return Ok(new { mensagem = "Confirmação cancelada com sucesso." });
        }

        [HttpPost("confirmar-todas")]
        [Authorize(Roles = "ADMIN,GESTAO_CULTO,LIDER_MINISTERIO")]
        public async Task<IActionResult> ConfirmarTodas([FromQuery] long cultoId, [FromQuery] long? ministerioId)
        {
            if (cultoId <= 0)
            {
                return BadRequest(new { mensagem = "Informe o culto para confirmar as escalas." });
            }

            var query = _db.Escalas.Where(x => x.CultoId == cultoId);
            if (ministerioId.HasValue && ministerioId.Value > 0)
            {
                query = query.Where(x => x.MinisterioId == ministerioId.Value);
            }

            var escalas = await query.ToListAsync();
            if (!escalas.Any())
            {
                return Ok(new { mensagem = "Nenhuma escala encontrada para confirmação.", total = 0 });
            }

            foreach (var escala in escalas)
            {
                escala.PresencaStatusId = 2;
                escala.ConfirmadoEm = DateTime.UtcNow;
                escala.AtualizadoEm = DateTime.UtcNow;
            }

            await _db.SaveChangesAsync();
            return Ok(new { mensagem = $"{escalas.Count} escalas confirmadas com sucesso.", total = escalas.Count });
        }

        [HttpDelete("{id:long}")]
        [Authorize(Roles = "ADMIN,GESTAO_CULTO,LIDER_MINISTERIO")]
        public async Task<IActionResult> Excluir(long id)
        {
            var entity = await _db.Escalas.FirstOrDefaultAsync(x => x.Id == id);
            if (entity == null)
            {
                return NotFound(new { mensagem = "Escala não encontrada." });
            }

            _db.Escalas.Remove(entity);
            await _db.SaveChangesAsync();

            return Ok(new { mensagem = "Escala excluída com sucesso." });
        }

        [HttpDelete("culto/{cultoId:long}")]
        [Authorize(Roles = "ADMIN,GESTAO_CULTO,LIDER_MINISTERIO")]
        public async Task<IActionResult> ExcluirPorCulto(long cultoId)
        {
            var escalas = await _db.Escalas.Where(x => x.CultoId == cultoId).ToListAsync();
            if (!escalas.Any())
            {
                return Ok(new { mensagem = "Nenhuma escala encontrada para este culto.", removidas = 0 });
            }

            _db.Escalas.RemoveRange(escalas);
            await _db.SaveChangesAsync();

            return Ok(new { mensagem = "Escalas do culto excluídas com sucesso.", removidas = escalas.Count });
        }

        private async Task<string?> ValidarFuncaoPorMinisterio(long? ministerioId, string funcao)
        {
            if (!ministerioId.HasValue || ministerioId.Value <= 0)
            {
                return null;
            }

            var funcoesAtivas = await _db.MinisteriosFuncoesPadrao
                .AsNoTracking()
                .Where(x => x.MinisterioId == ministerioId.Value && x.Ativo)
                .Select(x => x.Nome)
                .ToListAsync();

            if (!funcoesAtivas.Any())
            {
                return null;
            }

            var existe = funcoesAtivas.Any(x => string.Equals((x ?? string.Empty).Trim(), funcao.Trim(), StringComparison.OrdinalIgnoreCase));
            return existe
                ? null
                : "A função informada não está cadastrada para a equipe selecionada.";
        }

        private async Task<string?> ValidarMinisterioPermitidoParaLider(long? ministerioId)
        {
            if (User.IsInRole("ADMIN") || User.IsInRole("GESTAO_CULTO") || !User.IsInRole("LIDER_MINISTERIO"))
            {
                return null;
            }

            if (!ministerioId.HasValue || ministerioId.Value <= 0)
            {
                return "Informe o ministério da escala.";
            }

            var sub = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
            if (!long.TryParse(sub, out var usuarioId))
            {
                return "Usuário inválido.";
            }

            var permitido = await _db.MinisteriosLideres
                .AsNoTracking()
                .AnyAsync(x => x.UsuarioId == usuarioId && x.MinisterioId == ministerioId.Value);

            return permitido ? null : "Você não pode lançar escala para este ministério.";
        }

        private async Task<string?> ValidarEtapaDoCulto(long cultoId, long? etapaCultoId)
        {
            if (!etapaCultoId.HasValue || etapaCultoId.Value <= 0)
            {
                return null;
            }

            var existe = await _db.EtapasCulto
                .AsNoTracking()
                .AnyAsync(x => x.Id == etapaCultoId.Value && x.CultoId == cultoId);

            return existe ? null : "A etapa selecionada não pertence ao culto informado.";
        }

        private async Task<string> ObterBlocoCronogramaDaFuncao(long? ministerioId, string funcao)
        {
            if (!ministerioId.HasValue || ministerioId.Value <= 0)
            {
                return "SOMENTE_EQUIPE";
            }

            var bloco = await _db.MinisteriosFuncoesPadrao
                .AsNoTracking()
                .Where(x => x.MinisterioId == ministerioId.Value && x.Ativo && x.Nome.ToLower() == funcao.ToLower())
                .Select(x => x.BlocoCronograma)
                .FirstOrDefaultAsync();

            return NormalizarBlocoCronograma(bloco);
        }

        private async Task<bool> PodeGerenciarRepertorioDaFuncao(long? ministerioId, string funcao)
        {
            if (!ministerioId.HasValue || ministerioId.Value <= 0)
            {
                return false;
            }

            return await _db.MinisteriosFuncoesPadrao
                .AsNoTracking()
                .Where(x => x.MinisterioId == ministerioId.Value
                    && x.Ativo
                    && x.Ministerio.Codigo == "LOUVOR"
                    && x.Nome.ToLower() == funcao.ToLower())
                .Select(x => x.PodeGerenciarRepertorio)
                .FirstOrDefaultAsync();
        }

        private static string NormalizarBlocoCronograma(string? valor)
        {
            var texto = (valor ?? string.Empty).Trim().ToUpperInvariant();
            return texto switch
            {
                "PRINCIPAL" => "PRINCIPAL",
                "LOUNGE" => "LOUNGE",
                "ADICIONAL" => "ADICIONAL",
                "SOMENTE_EQUIPE" => "SOMENTE_EQUIPE",
                _ => "SOMENTE_EQUIPE"
            };
        }
    }
}
