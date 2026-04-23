using System;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Mvc;

namespace GestaoCulto.API.Controllers
{
    [ApiController]
    [Route("api/debug")]
    public class DebugController : ControllerBase
    {
        private readonly IWebHostEnvironment _environment;
        private readonly IConfiguration _configuration;

        public DebugController(IWebHostEnvironment environment, IConfiguration configuration)
        {
            _environment = environment;
            _configuration = configuration;
        }

        [AllowAnonymous]
        [HttpGet("erro-sentry")]
        public IActionResult ErroSentry()
        {
            if (!string.Equals(_environment.EnvironmentName, "Development", StringComparison.OrdinalIgnoreCase))
            {
                return NotFound();
            }

            throw new Exception("Teste manual de erro para Sentry.");
        }

        [AllowAnonymous]
        [HttpGet("erro-sentry-temp")]
        public IActionResult ErroSentryTemporario()
        {
            var endpointHabilitado = _configuration.GetValue<bool>("Debug:EnableErrorTestEndpoint");
            if (!endpointHabilitado)
            {
                return NotFound();
            }

            var tokenEsperado = (_configuration["Debug:ErrorTestToken"] ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(tokenEsperado))
            {
                return NotFound();
            }

            if (!Request.Headers.TryGetValue("X-Debug-Token", out var tokenRecebidoValores))
            {
                return NotFound();
            }

            var tokenRecebido = tokenRecebidoValores.FirstOrDefault()?.Trim() ?? string.Empty;
            if (!string.Equals(tokenRecebido, tokenEsperado, StringComparison.Ordinal))
            {
                return NotFound();
            }

            throw new Exception("Teste temporario de erro para Sentry em producao.");
        }
    }
}
