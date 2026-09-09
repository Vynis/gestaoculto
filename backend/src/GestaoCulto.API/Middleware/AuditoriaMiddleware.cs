using System;
using System.Linq;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.Tasks;
using GestaoCulto.Domain.Entities;
using GestaoCulto.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace GestaoCulto.API.Middleware
{
    public class AuditoriaMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<AuditoriaMiddleware> _logger;

        public AuditoriaMiddleware(RequestDelegate next, ILogger<AuditoriaMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task Invoke(HttpContext context, GestaoCultoDbContext db)
        {
            if (!DeveAuditar(context.Request.Method))
            {
                await _next(context);
                return;
            }

            await _next(context);

            try
            {
                await RegistrarAsync(context, db);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Não foi possível registrar auditoria da requisição {Metodo} {Caminho}.", context.Request.Method, context.Request.Path);
            }
        }

        private static bool DeveAuditar(string metodo)
        {
            return HttpMethods.IsPost(metodo)
                || HttpMethods.IsPut(metodo)
                || HttpMethods.IsPatch(metodo)
                || HttpMethods.IsDelete(metodo);
        }

        private static async Task RegistrarAsync(HttpContext context, GestaoCultoDbContext db)
        {
            var caminho = context.Request.Path.Value ?? string.Empty;
            var segmentos = caminho.Split('/', StringSplitOptions.RemoveEmptyEntries);
            var tabela = segmentos.Length >= 2 && string.Equals(segmentos[0], "api", StringComparison.OrdinalIgnoreCase)
                ? segmentos[1]
                : (segmentos.FirstOrDefault() ?? "http");
            var entidadeId = segmentos.FirstOrDefault(x => long.TryParse(x, out _)) ?? "-";
            var acao = ObterAcao(context.Request.Method, caminho);
            var usuarioId = ObterUsuarioId(context);

            db.Auditorias.Add(new Auditoria
            {
                Tabela = Limitar(tabela, 100) ?? "http",
                EntidadeId = Limitar(entidadeId, 50) ?? "-",
                Acao = acao,
                UsuarioId = usuarioId,
                IpOrigem = Limitar(context.Connection.RemoteIpAddress?.ToString(), 45),
                UserAgent = Limitar(context.Request.Headers["User-Agent"].FirstOrDefault(), 255),
                DadosNovos = JsonSerializer.Serialize(new
                {
                    metodo = context.Request.Method,
                    caminho,
                    status = context.Response.StatusCode,
                    sucesso = context.Response.StatusCode < 400
                }),
                CriadoEm = DateTime.UtcNow
            });

            await db.SaveChangesAsync();
        }

        private static string ObterAcao(string metodo, string caminho)
        {
            if (caminho.EndsWith("/login", StringComparison.OrdinalIgnoreCase))
            {
                return "LOGIN";
            }

            if (caminho.EndsWith("/logout", StringComparison.OrdinalIgnoreCase))
            {
                return "LOGOUT";
            }

            return HttpMethods.IsPost(metodo) ? "CREATE" : HttpMethods.IsDelete(metodo) ? "DELETE" : "UPDATE";
        }

        private static long? ObterUsuarioId(HttpContext context)
        {
            var valor = context.User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? context.User.FindFirstValue("sub");
            return long.TryParse(valor, out var usuarioId) ? (long?)usuarioId : null;
        }

        private static string? Limitar(string? valor, int tamanho)
        {
            if (string.IsNullOrWhiteSpace(valor))
            {
                return null;
            }

            return valor.Length <= tamanho ? valor : valor.Substring(0, tamanho);
        }
    }
}
