using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace GestaoCulto.API.Swagger
{
    public class AuthorizeCheckOperationFilter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            var hasAllowAnonymous = context.MethodInfo
                .GetCustomAttributes(true)
                .OfType<AllowAnonymousAttribute>()
                .Any();

            if (hasAllowAnonymous)
            {
                return;
            }

            var hasAuthorize = context.MethodInfo
                                   .GetCustomAttributes(true)
                                   .OfType<AuthorizeAttribute>()
                                   .Any()
                               || context.MethodInfo.DeclaringType != null
                               && context.MethodInfo.DeclaringType
                                   .GetCustomAttributes(true)
                                   .OfType<AuthorizeAttribute>()
                                   .Any();

            if (!hasAuthorize)
            {
                return;
            }

            operation.Responses.TryAdd("401", new OpenApiResponse { Description = "Não autorizado" });
            operation.Responses.TryAdd("403", new OpenApiResponse { Description = "Acesso negado" });

            operation.Security = operation.Security ?? new List<OpenApiSecurityRequirement>();
            operation.Security.Add(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "Bearer"
                        }
                    },
                    Array.Empty<string>()
                }
            });
        }
    }
}
