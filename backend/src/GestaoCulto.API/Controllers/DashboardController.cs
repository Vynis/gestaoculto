using System.Threading.Tasks;
using System;
using GestaoCulto.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GestaoCulto.API.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/dashboard")]
    public class DashboardController : ControllerBase
    {
        private readonly IDashboardService _dashboardService;

        public DashboardController(IDashboardService dashboardService)
        {
            _dashboardService = dashboardService;
        }

        [HttpGet("resumo")]
        public async Task<IActionResult> Resumo()
        {
            return Ok(await _dashboardService.ObterResumoAsync());
        }

        [HttpGet("estatisticas")]
        [Authorize(Roles = "ADMIN,GESTAO_CULTO,LIDER_MINISTERIO")]
        public async Task<IActionResult> Estatisticas([FromQuery] DateTime? inicio, [FromQuery] DateTime? fim, [FromQuery] int limite = 5)
        {
            if (limite < 1 || limite > 20)
            {
                return BadRequest(new { mensagem = "O limite deve estar entre 1 e 20." });
            }

            if (inicio.HasValue && fim.HasValue && inicio.Value.Date > fim.Value.Date)
            {
                return BadRequest(new { mensagem = "O período informado é inválido." });
            }

            return Ok(await _dashboardService.ObterEstatisticasAsync(inicio, fim, limite));
        }
    }
}
