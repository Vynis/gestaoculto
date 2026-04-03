using System;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace GestaoCulto.API.Middleware
{
    public class GlobalExceptionMiddleware
    {
        private readonly RequestDelegate _next;

        public GlobalExceptionMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
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
                detalhes = status == HttpStatusCode.InternalServerError ? "Erro inesperado ao processar a solicitação." : null
            };

            await context.Response.WriteAsync(JsonSerializer.Serialize(payload));
        }
    }
}
