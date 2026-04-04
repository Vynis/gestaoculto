using System.Text;
using GestaoCulto.Application.Interfaces;
using GestaoCulto.Infrastructure.ExternalAuth;
using GestaoCulto.Infrastructure.Persistence;
using GestaoCulto.Infrastructure.Security;
using GestaoCulto.Infrastructure.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace GestaoCulto.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {
            var connectionString = configuration.GetConnectionString("DefaultConnection");
            services.AddDbContext<GestaoCultoDbContext>(options =>
                options.UseMySql(connectionString, mySqlOptions => { }));

            services.AddScoped<IAuthService, AuthService>();
            services.AddScoped<ICultoService, CultoService>();
            services.AddScoped<IDashboardService, DashboardService>();
            services.AddScoped<ITokenService, JwtTokenService>();
            services.AddScoped<IPasswordHasher, SimplePasswordHasher>();
            services.AddScoped<IGoogleTokenValidator, GoogleTokenValidator>();
            services.AddScoped<IEmailSender, SmtpEmailSender>();
            services.AddScoped<DbSeeder>();

            var key = Encoding.UTF8.GetBytes(configuration["Jwt:Key"]);
            services.AddAuthentication(x =>
                {
                    x.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                    x.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                })
                .AddJwtBearer(x =>
                {
                    x.RequireHttpsMetadata = false;
                    x.SaveToken = true;
                    x.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey = new SymmetricSecurityKey(key),
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidIssuer = configuration["Jwt:Issuer"],
                        ValidAudience = configuration["Jwt:Audience"],
                        ClockSkew = System.TimeSpan.Zero
                    };
                });

            return services;
        }
    }
}
