using System;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Sentry;

namespace GestaoCulto.API.Middleware
{
    public class GlobalExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<GlobalExceptionMiddleware> _logger;

        public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task Invoke(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Erro nao tratado. TraceId={TraceId} Metodo={Metodo} Caminho={Caminho} Usuario={Usuario}",
                    context.TraceIdentifier,
                    context.Request.Method,
                    context.Request.Path,
                    context.User?.Identity?.Name ?? "anonimo");

                SentrySdk.CaptureException(ex);
                await HandleException(context, ex);
            }
        }

        private static async Task HandleException(HttpContext context, Exception ex)
        {
            var status = ex is InvalidOperationException ? HttpStatusCode.BadRequest : HttpStatusCode.InternalServerError;
            context.Response.StatusCode = (int)status;
            context.Response.ContentType = "application/json";

            var payload = new
            {
                sucesso = false,
                mensagem = ex.Message,
                detalhes = status == HttpStatusCode.InternalServerError ? "Erro inesperado ao processar a solicitacao." : null,
                traceId = context.TraceIdentifier
            };

            await context.Response.WriteAsync(JsonSerializer.Serialize(payload));
        }
    }
}
