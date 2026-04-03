using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using GestaoCulto.API.Middleware;
using GestaoCulto.API.Swagger;
using GestaoCulto.Infrastructure;
using GestaoCulto.Infrastructure.Persistence;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;

namespace GestaoCulto.API
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddInfrastructure(Configuration);

            services.AddCors(options =>
            {
                options.AddPolicy("DefaultCors", builder =>
                {
                    var origins = Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();
                    if (!origins.Any())
                    {
                        builder.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
                    }
                    else
                    {
                        builder.WithOrigins(origins).AllowAnyHeader().AllowAnyMethod();
                    }
                });
            });

            services.AddControllers()
                .AddJsonOptions(options =>
                {
                    options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
                });

            services.AddSwaggerGen(c =>
            {
                var swaggerTitle = Configuration["Swagger:Title"] ?? "GestaoCulto.API";
                var swaggerVersion = Configuration["Swagger:Version"] ?? "v1";
                var swaggerDescription = Configuration["Swagger:Description"] ?? "API de gestão de culto.";

                c.SwaggerDoc(swaggerVersion, new OpenApiInfo
                {
                    Title = swaggerTitle,
                    Version = swaggerVersion,
                    Description = swaggerDescription
                });

                c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Description = "Informe somente o token JWT. Exemplo: eyJhbGciOi...",
                    Name = "Authorization",
                    In = ParameterLocation.Header,
                    Type = SecuritySchemeType.Http,
                    Scheme = "bearer",
                    BearerFormat = "JWT"
                });

                c.OperationFilter<AuthorizeCheckOperationFilter>();

                var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
                var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
                if (File.Exists(xmlPath))
                {
                    c.IncludeXmlComments(xmlPath, includeControllerXmlComments: true);
                }
            });
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseSwagger(c => { c.RouteTemplate = "docs/{documentName}/swagger.json"; });
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/docs/v1/swagger.json", "Gestão de Culto API v1");
                c.RoutePrefix = "docs";
                c.DisplayRequestDuration();
            });

            app.UseRouting();
            app.UseCors("DefaultCors");
            app.UseMiddleware<GlobalExceptionMiddleware>();
            app.UseAuthentication();
            app.UseAuthorization();

            using (var scope = app.ApplicationServices.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<GestaoCultoDbContext>();
                db.Database.EnsureCreated();
                var seeder = scope.ServiceProvider.GetRequiredService<DbSeeder>();
                seeder.SeedAsync().GetAwaiter().GetResult();
            }

            app.UseEndpoints(endpoints => { endpoints.MapControllers(); });
        }
    }
}
