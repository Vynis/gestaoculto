using System.Threading.Tasks;
using GestaoCulto.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GestaoCulto.API.Controllers
{
    [ApiController]
    [Authorize(Roles = "ADMIN,GESTAO_CULTO")]
    [Route("api/voluntarios/{voluntarioId:long}/telegram")]
    public class VoluntariosTelegramController : ControllerBase
    {
        private readonly ITelegramService _telegramService;

        public VoluntariosTelegramController(ITelegramService telegramService)
        {
            _telegramService = telegramService;
        }

        [HttpPost("vinculo")]
        public async Task<IActionResult> GerarVinculo(long voluntarioId)
        {
            return Ok(await _telegramService.GerarVinculoAsync(voluntarioId));
        }

        [HttpGet("status")]
        public async Task<IActionResult> ObterStatus(long voluntarioId)
        {
            return Ok(await _telegramService.ObterStatusAsync(voluntarioId));
        }

        [HttpDelete("vinculo")]
        public async Task<IActionResult> Desvincular(long voluntarioId)
        {
            await _telegramService.DesvincularAsync(voluntarioId);
            return NoContent();
        }
    }
}
