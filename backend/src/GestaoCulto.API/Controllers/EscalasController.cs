using System;
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
        public long VoluntarioId { get; set; }
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
                    e.VoluntarioId,
                    VoluntarioNome = _db.Voluntarios.Where(v => v.Id == e.VoluntarioId).Select(v => v.Nome).FirstOrDefault(),
                    e.MinisterioId,
                    MinisterioNome = _db.Ministerios.Where(m => m.Id == e.MinisterioId).Select(m => m.Nome).FirstOrDefault(),
                    e.Funcao,
                    e.HorarioPrevisto,
                    e.PresencaStatusId,
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

            var entity = new Escala
            {
                CultoId = dto.CultoId,
                EtapaCultoId = dto.EtapaCultoId,
                VoluntarioId = dto.VoluntarioId,
                MinisterioId = dto.MinisterioId,
                Funcao = funcaoNormalizada,
                HorarioPrevisto = null,
                PresencaStatusId = dto.PresencaStatusId,
                Observacoes = dto.Observacoes,
                CriadoEm = DateTime.UtcNow
            };

            _db.Escalas.Add(entity);
            await _db.SaveChangesAsync();
            return Ok(entity);
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

            entity.CultoId = dto.CultoId;
            entity.EtapaCultoId = dto.EtapaCultoId;
            entity.VoluntarioId = dto.VoluntarioId;
            entity.MinisterioId = dto.MinisterioId;
            entity.Funcao = funcaoNormalizada;
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
    }
}
