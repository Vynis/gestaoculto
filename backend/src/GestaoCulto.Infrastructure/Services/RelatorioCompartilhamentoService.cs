using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using GestaoCulto.Application.DTOs.Relatorios;
using GestaoCulto.Application.Interfaces;
using GestaoCulto.Domain.Entities;
using GestaoCulto.Infrastructure.Persistence;
using GestaoCulto.Infrastructure.Telegram;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace GestaoCulto.Infrastructure.Services
{
    public class RelatorioCompartilhamentoOptions
    {
        public string FrontendBaseUrl { get; set; } = string.Empty;
        public int ExpirationHoursAfterCulto { get; set; } = 12;
    }

    public class RelatorioCompartilhamentoService : IRelatorioCompartilhamentoService
    {
        private readonly GestaoCultoDbContext _db;
        private readonly RelatorioCompartilhamentoOptions _options;
        private readonly TelegramOptions _telegramOptions;

        public RelatorioCompartilhamentoService(
            GestaoCultoDbContext db,
            IOptions<RelatorioCompartilhamentoOptions> options,
            IOptions<TelegramOptions> telegramOptions)
        {
            _db = db;
            _options = options.Value;
            _telegramOptions = telegramOptions.Value;
        }

        public async Task<RelatorioCompartilhadoLinkDto> GerarLinkAsync(long cultoId)
        {
            var voluntarioId = await _db.Escalas
                .AsNoTracking()
                .Where(x => x.CultoId == cultoId && x.VoluntarioId.HasValue)
                .Select(x => x.VoluntarioId!.Value)
                .Where(x => _db.Voluntarios.Any(v => v.Id == x && v.Ativo))
                .OrderBy(x => x)
                .FirstOrDefaultAsync();

            if (voluntarioId <= 0)
            {
                throw new InvalidOperationException("Não há um voluntário ativo escalado para este culto.");
            }

            return await GerarLinkAsync(cultoId, voluntarioId);
        }

        public async Task<RelatorioCompartilhadoLinkDto> GerarLinkAsync(long cultoId, long voluntarioId)
        {
            var culto = await _db.Cultos
                .AsNoTracking()
                .Where(x => x.Id == cultoId
                    && _db.StatusCultos.Any(s => s.Id == x.StatusCultoId && s.Codigo == "ATIVO")
                    && _db.Escalas.Any(e => e.CultoId == x.Id && e.VoluntarioId == voluntarioId)
                    && _db.Voluntarios.Any(v => v.Id == voluntarioId && v.Ativo))
                .Select(x => new
                {
                    x.DataCulto,
                    x.HorarioInicio,
                    x.HorarioFimPrevisto
                })
                .FirstOrDefaultAsync();

            if (culto == null)
            {
                throw new InvalidOperationException("Você não está escalado para este culto ou ele não está ativo.");
            }

            var frontendBaseUrl = ObterFrontendBaseUrlPublica();

            var horarioFim = culto.HorarioFimPrevisto ?? culto.HorarioInicio.Add(TimeSpan.FromHours(3));
            var fimLocal = culto.DataCulto.Date.Add(horarioFim);
            if (horarioFim <= culto.HorarioInicio)
            {
                fimLocal = fimLocal.AddDays(1);
            }

            var expiraEm = ConverterHorarioBrasiliaParaUtc(fimLocal)
                .AddHours(Math.Max(1, _options.ExpirationHoursAfterCulto));
            if (expiraEm <= DateTime.UtcNow)
            {
                throw new InvalidOperationException("O prazo para compartilhar o relatório deste culto terminou.");
            }

            var token = GerarToken();
            _db.RelatoriosCultoCompartilhamentos.Add(new RelatorioCultoCompartilhamento
            {
                CultoId = cultoId,
                VoluntarioId = voluntarioId,
                TokenHash = CalcularHash(token),
                ExpiraEm = expiraEm
            });
            await _db.SaveChangesAsync();

            return new RelatorioCompartilhadoLinkDto
            {
                Url = $"{frontendBaseUrl}/relatorio-culto-publico?t={Uri.EscapeDataString(token)}",
                ExpiraEm = expiraEm
            };
        }

        public async Task<long?> ObterCultoIdAsync(string token)
        {
            if (string.IsNullOrWhiteSpace(token) || token.Length < 20 || token.Length > 100)
            {
                return null;
            }

            var agora = DateTime.UtcNow;
            var tokenHash = CalcularHash(token);
            var compartilhamento = await _db.RelatoriosCultoCompartilhamentos
                .FirstOrDefaultAsync(x => x.TokenHash == tokenHash
                    && x.ExpiraEm > agora
                    && x.RevogadoEm == null
                    && _db.StatusCultos.Any(s => s.Id == x.Culto.StatusCultoId && s.Codigo == "ATIVO")
                    && _db.Voluntarios.Any(v => v.Id == x.VoluntarioId && v.Ativo)
                    && _db.Escalas.Any(e => e.CultoId == x.CultoId && e.VoluntarioId == x.VoluntarioId));

            if (compartilhamento == null)
            {
                return null;
            }

            compartilhamento.UltimoAcessoEm = agora;
            compartilhamento.AtualizadoEm = agora;
            await _db.SaveChangesAsync();
            return compartilhamento.CultoId;
        }

        private static DateTime ConverterHorarioBrasiliaParaUtc(DateTime horario)
        {
            var timezoneId = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                ? "E. South America Standard Time"
                : "America/Sao_Paulo";
            var timezone = TimeZoneInfo.FindSystemTimeZoneById(timezoneId);
            return TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(horario, DateTimeKind.Unspecified), timezone);
        }

        private string ObterFrontendBaseUrlPublica()
        {
            if (Uri.TryCreate(_options.FrontendBaseUrl, UriKind.Absolute, out var frontendUri)
                && !frontendUri.IsLoopback
                && (frontendUri.Scheme == Uri.UriSchemeHttps || frontendUri.Scheme == Uri.UriSchemeHttp))
            {
                return _options.FrontendBaseUrl.TrimEnd('/');
            }

            if (Uri.TryCreate(_telegramOptions.WebhookUrl, UriKind.Absolute, out var webhookUri)
                && !webhookUri.IsLoopback
                && webhookUri.Scheme == Uri.UriSchemeHttps)
            {
                return webhookUri.GetLeftPart(UriPartial.Authority).TrimEnd('/');
            }

            throw new InvalidOperationException(
                "Configure RelatorioCompartilhamento:FrontendBaseUrl com uma URL pública HTTPS.");
        }

        private static string GerarToken()
        {
            var bytes = new byte[32];
            using var generator = RandomNumberGenerator.Create();
            generator.GetBytes(bytes);
            return Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
        }

        private static string CalcularHash(string valor)
        {
            using var sha = SHA256.Create();
            var hash = sha.ComputeHash(Encoding.UTF8.GetBytes(valor));
            return BitConverter.ToString(hash).Replace("-", string.Empty).ToLowerInvariant();
        }
    }
}
