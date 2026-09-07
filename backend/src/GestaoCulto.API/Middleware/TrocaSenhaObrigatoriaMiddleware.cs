using System.Security.Claims;
using System.Text.Json;
using System.Threading.Tasks;
using GestaoCulto.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace GestaoCulto.API.Middleware
{
    public class TrocaSenhaObrigatoriaMiddleware
    {
        private readonly RequestDelegate _next;

        public TrocaSenhaObrigatoriaMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext context, GestaoCultoDbContext db)
        {
            if (context.User.Identity?.IsAuthenticated == true
                && !context.Request.Path.StartsWithSegments("/api/auth/trocar-senha-obrigatoria"))
            {
                var sub = context.User.FindFirstValue(ClaimTypes.NameIdentifier)
                    ?? context.User.FindFirstValue("sub");

                if (long.TryParse(sub, out var usuarioId)
                    && await db.Usuarios.AsNoTracking().AnyAsync(x => x.Id == usuarioId && x.DeveTrocarSenha))
                {
                    context.Response.StatusCode = StatusCodes.Status403Forbidden;
                    context.Response.ContentType = "application/json";
                    await context.Response.WriteAsync(JsonSerializer.Serialize(new
                    {
                        codigo = "TROCA_SENHA_OBRIGATORIA",
                        mensagem = "Defina uma nova senha para continuar."
                    }));
                    return;
                }
            }

            await _next(context);
        }
    }
}
