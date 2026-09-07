using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using GestaoCulto.API.Middleware;
using GestaoCulto.API.Swagger;
using GestaoCulto.API.Versioning;
using GestaoCulto.Infrastructure;
using GestaoCulto.Infrastructure.Persistence;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.FileProviders;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using Sentry.AspNetCore;

namespace GestaoCulto.API
{
    public class Startup
    {
        private readonly IWebHostEnvironment _environment;

        public Startup(IConfiguration configuration, IWebHostEnvironment environment)
        {
            Configuration = configuration;
            _environment = environment;
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            var buildVersionInfo = BuildVersionProvider.Load(Configuration, _environment.ContentRootPath, _environment.EnvironmentName);
            services.AddSingleton(buildVersionInfo);

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
                swaggerDescription = $"{swaggerDescription} (build {buildVersionInfo.Version}, commit {buildVersionInfo.Commit})";

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
            string? frontendDevelopmentPath = null;
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                frontendDevelopmentPath = Path.GetFullPath(Path.Combine(
                    env.ContentRootPath,
                    "..",
                    "..",
                    "..",
                    "frontend",
                    "dist",
                    "frontend"));

                if (Directory.Exists(frontendDevelopmentPath))
                {
                    var frontendFiles = new PhysicalFileProvider(frontendDevelopmentPath);
                    app.UseDefaultFiles(new DefaultFilesOptions { FileProvider = frontendFiles });
                    app.UseStaticFiles(new StaticFileOptions { FileProvider = frontendFiles });
                }
            }

            var basePath = Configuration["AppInfo:BasePath"];
            if (!string.IsNullOrWhiteSpace(basePath))
            {
                basePath = basePath.Trim();
                if (!basePath.StartsWith('/'))
                {
                    basePath = $"/{basePath}";
                }

                app.UsePathBase(basePath);
            }

            app.UseSwagger(c => { c.RouteTemplate = "docs/{documentName}/swagger.json"; });
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("v1/swagger.json", "Gestão de Culto API v1");
                c.RoutePrefix = "docs";
                c.DisplayRequestDuration();
            });

            app.UseRouting();
            app.UseSentryTracing();
            app.UseCors("DefaultCors");
            app.UseMiddleware<GlobalExceptionMiddleware>();
            app.UseAuthentication();
            app.UseMiddleware<TrocaSenhaObrigatoriaMiddleware>();
            app.UseAuthorization();

            using (var scope = app.ApplicationServices.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<GestaoCultoDbContext>();
                db.Database.EnsureCreated();
                var seeder = scope.ServiceProvider.GetRequiredService<DbSeeder>();
                seeder.SeedAsync().GetAwaiter().GetResult();
            }

            app.UseEndpoints(endpoints => { endpoints.MapControllers(); });

            if (!string.IsNullOrWhiteSpace(frontendDevelopmentPath)
                && File.Exists(Path.Combine(frontendDevelopmentPath, "index.html")))
            {
                var indexPath = Path.Combine(frontendDevelopmentPath, "index.html");
                app.Run(async context =>
                {
                    if (HttpMethods.IsGet(context.Request.Method)
                        && !context.Request.Path.StartsWithSegments("/api")
                        && !context.Request.Path.StartsWithSegments("/docs"))
                    {
                        context.Response.ContentType = "text/html; charset=utf-8";
                        await context.Response.SendFileAsync(indexPath);
                        return;
                    }

                    context.Response.StatusCode = 404;
                });
            }
        }
    }
}
