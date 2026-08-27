using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using GestaoCulto.Application.DTOs.Telegram;
using GestaoCulto.Application.Interfaces;
using GestaoCulto.Infrastructure.Telegram;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace GestaoCulto.API.Controllers
{
    [ApiController]
    [Route("api/telegram")]
    public class TelegramController : ControllerBase
    {
        private const string SecretHeader = "X-Telegram-Bot-Api-Secret-Token";
        private readonly ITelegramService _telegramService;
        private readonly TelegramOptions _options;

        public TelegramController(ITelegramService telegramService, IOptions<TelegramOptions> options)
        {
            _telegramService = telegramService;
            _options = options.Value;
        }

        [AllowAnonymous]
        [HttpPost("webhook")]
        [RequestSizeLimit(65536)]
        public async Task<IActionResult> Webhook([FromBody] TelegramUpdateDto update)
        {
            if (!_options.Enabled || string.IsNullOrWhiteSpace(_options.WebhookSecret))
            {
                return StatusCode(StatusCodes.Status503ServiceUnavailable);
            }

            var segredoRecebido = Request.Headers[SecretHeader].ToString();
            if (!SegredosIguais(_options.WebhookSecret, segredoRecebido))
            {
                return Unauthorized();
            }

            await _telegramService.ProcessarUpdateAsync(update);
            return Ok();
        }

        [Authorize(Roles = "ADMIN")]
        [HttpPost("configurar-webhook")]
        public async Task<IActionResult> ConfigurarWebhook()
        {
            await _telegramService.ConfigurarWebhookAsync();
            return Ok(new { mensagem = "Webhook configurado com sucesso." });
        }

        private static bool SegredosIguais(string esperado, string recebido)
        {
            var esperadoBytes = Encoding.UTF8.GetBytes(esperado);
            var recebidoBytes = Encoding.UTF8.GetBytes(recebido ?? string.Empty);
            return esperadoBytes.Length == recebidoBytes.Length
                && CryptographicOperations.FixedTimeEquals(esperadoBytes, recebidoBytes);
        }
    }
}
