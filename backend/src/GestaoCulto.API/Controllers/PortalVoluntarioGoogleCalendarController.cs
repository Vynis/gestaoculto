using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using GestaoCulto.Domain.Entities;
using GestaoCulto.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace GestaoCulto.API.Controllers
{
    public class GoogleCalendarSelecionarRequest
    {
        public string CalendarId { get; set; } = string.Empty;
    }

    [ApiController]
    [Authorize(Roles = "VOLUNTARIO")]
    [Route("api/portal-voluntario/google-calendar")]
    public class PortalVoluntarioGoogleCalendarController : ControllerBase
    {
        private readonly GestaoCultoDbContext _db;
        private readonly IConfiguration _configuration;
        private const string GoogleOauthAuthUrl = "https://accounts.google.com/o/oauth2/v2/auth";
        private const string GoogleOauthTokenUrl = "https://oauth2.googleapis.com/token";
        private const string GoogleCalendarApiBase = "https://www.googleapis.com/calendar/v3";

        public PortalVoluntarioGoogleCalendarController(GestaoCultoDbContext db, IConfiguration configuration)
        {
            _db = db;
            _configuration = configuration;
        }

        [HttpGet("status")]
        public async Task<IActionResult> Status()
        {
            var voluntario = await ObterVoluntarioLogado();
            if (voluntario == null)
            {
                return Unauthorized(new { mensagem = "Voluntário não vinculado ao usuário autenticado." });
            }

            var conexao = await _db.VoluntariosGoogleCalendarConexoes
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.VoluntarioId == voluntario.Id && x.Ativo);

            if (conexao == null)
            {
                return Ok(new
                {
                    conectado = false,
                    googleEmail = (string?)null,
                    calendarioGoogleId = (string?)null,
                    calendarioGoogleNome = (string?)null,
                    ultimoSyncEm = (DateTime?)null,
                    ultimoErroSync = (string?)null
                });
            }

            return Ok(new
            {
                conectado = true,
                conexao.GoogleEmail,
                conexao.CalendarioGoogleId,
                conexao.CalendarioGoogleNome,
                conexao.UltimoSyncEm,
                conexao.UltimoErroSync
            });
        }

        [HttpPost("connect")]
        public async Task<IActionResult> Connect()
        {
            var voluntario = await ObterVoluntarioLogado();
            if (voluntario == null)
            {
                return Unauthorized(new { mensagem = "Voluntário não vinculado ao usuário autenticado." });
            }

            var clientId = (_configuration["GoogleCalendar:ClientId"] ?? _configuration["GoogleAuth:ClientId"] ?? string.Empty).Trim();
            var redirectUri = (_configuration["GoogleCalendar:RedirectUri"] ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(clientId) || string.IsNullOrWhiteSpace(redirectUri))
            {
                return BadRequest(new { mensagem = "Integração Google Calendar não configurada no servidor." });
            }

            var state = GerarStateOauth(voluntario.Id);
            var scope = string.Join(" ", new[]
            {
                "openid",
                "email",
                "profile",
                "https://www.googleapis.com/auth/calendar.events",
                "https://www.googleapis.com/auth/calendar.calendarlist.readonly"
            });

            var query = new Dictionary<string, string>
            {
                ["client_id"] = clientId,
                ["redirect_uri"] = redirectUri,
                ["response_type"] = "code",
                ["scope"] = scope,
                ["access_type"] = "offline",
                ["prompt"] = "consent",
                ["include_granted_scopes"] = "true",
                ["state"] = state
            };

            var authUrl = $"{GoogleOauthAuthUrl}?{MontarQueryString(query)}";
            return Ok(new { authUrl });
        }

        [AllowAnonymous]
        [HttpGet("callback")]
        public async Task<IActionResult> Callback([FromQuery] string? code, [FromQuery] string? state, [FromQuery] string? error = null)
        {
            if (!string.IsNullOrWhiteSpace(error))
            {
                return Redirect(MontarUrlFrontendRetorno(false, "A autorização do Google foi cancelada."));
            }

            if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(state))
            {
                return Redirect(MontarUrlFrontendRetorno(false, "Resposta inválida da autorização Google."));
            }

            var voluntarioId = ValidarStateOauth(state);
            if (!voluntarioId.HasValue)
            {
                return Redirect(MontarUrlFrontendRetorno(false, "Sessão de autorização expirada. Tente novamente."));
            }

            var token = await TrocarCodePorToken(code);
            if (token == null || string.IsNullOrWhiteSpace(token.AccessToken))
            {
                return Redirect(MontarUrlFrontendRetorno(false, "Não foi possível concluir a autenticação com Google."));
            }

            var userInfo = await ObterUsuarioGoogle(token.AccessToken);
            if (userInfo == null || string.IsNullOrWhiteSpace(userInfo.Value.Email))
            {
                return Redirect(MontarUrlFrontendRetorno(false, "Não foi possível obter os dados da conta Google."));
            }

            var conexao = await _db.VoluntariosGoogleCalendarConexoes.FirstOrDefaultAsync(x => x.VoluntarioId == voluntarioId.Value);
            if (conexao == null)
            {
                conexao = new VoluntarioGoogleCalendarConexao
                {
                    VoluntarioId = voluntarioId.Value,
                    CriadoEm = DateTime.UtcNow
                };
                _db.VoluntariosGoogleCalendarConexoes.Add(conexao);
            }

            conexao.GoogleEmail = userInfo.Value.Email;
            conexao.GoogleSub = userInfo.Value.Sub;
            conexao.AccessToken = token.AccessToken;
            if (!string.IsNullOrWhiteSpace(token.RefreshToken))
            {
                conexao.RefreshToken = token.RefreshToken;
            }
            conexao.AccessTokenExpiraEm = token.ExpiresInSeconds.HasValue
                ? DateTime.UtcNow.AddSeconds(token.ExpiresInSeconds.Value)
                : (DateTime?)null;
            conexao.Ativo = true;
            conexao.AtualizadoEm = DateTime.UtcNow;
            conexao.UltimoErroSync = null;

            await _db.SaveChangesAsync();
            return Redirect(MontarUrlFrontendRetorno(true, "Conta Google conectada com sucesso."));
        }

        [HttpGet("calendars")]
        public async Task<IActionResult> Calendars()
        {
            var voluntario = await ObterVoluntarioLogado();
            if (voluntario == null)
            {
                return Unauthorized(new { mensagem = "Voluntário não vinculado ao usuário autenticado." });
            }

            var conexao = await _db.VoluntariosGoogleCalendarConexoes.FirstOrDefaultAsync(x => x.VoluntarioId == voluntario.Id && x.Ativo);
            if (conexao == null)
            {
                return BadRequest(new { mensagem = "Conecte sua conta Google antes de listar calendários." });
            }

            var accessToken = await GarantirAccessTokenValido(conexao);
            if (string.IsNullOrWhiteSpace(accessToken))
            {
                return BadRequest(new { mensagem = "Não foi possível renovar o acesso ao Google Calendar. Reconecte sua conta." });
            }

            var calendarios = await ListarCalendariosGoogle(accessToken);
            return Ok(calendarios);
        }

        [HttpPost("create-calendar")]
        public async Task<IActionResult> CreateCalendar()
        {
            var voluntario = await ObterVoluntarioLogado();
            if (voluntario == null)
            {
                return Unauthorized(new { mensagem = "Voluntário não vinculado ao usuário autenticado." });
            }

            var conexao = await _db.VoluntariosGoogleCalendarConexoes.FirstOrDefaultAsync(x => x.VoluntarioId == voluntario.Id && x.Ativo);
            if (conexao == null)
            {
                return BadRequest(new { mensagem = "Conecte sua conta Google antes de criar calendário." });
            }

            var accessToken = await GarantirAccessTokenValido(conexao);
            if (string.IsNullOrWhiteSpace(accessToken))
            {
                return BadRequest(new { mensagem = "Não foi possível renovar o acesso ao Google Calendar. Reconecte sua conta." });
            }

            var calendarioCriado = await CriarCalendarioGoogle(accessToken, "Gestão Culto", "America/Sao_Paulo");
            if (calendarioCriado == null || string.IsNullOrWhiteSpace(calendarioCriado.Id))
            {
                return BadRequest(new { mensagem = "Não foi possível criar o calendário no Google." });
            }

            conexao.CalendarioGoogleId = calendarioCriado.Id;
            conexao.CalendarioGoogleNome = calendarioCriado.Summary;
            conexao.AtualizadoEm = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            return Ok(new { mensagem = "Calendário criado e selecionado com sucesso.", calendarioGoogleId = conexao.CalendarioGoogleId, calendarioGoogleNome = conexao.CalendarioGoogleNome });
        }

        [HttpPost("select-calendar")]
        public async Task<IActionResult> SelectCalendar([FromBody] GoogleCalendarSelecionarRequest request)
        {
            var voluntario = await ObterVoluntarioLogado();
            if (voluntario == null)
            {
                return Unauthorized(new { mensagem = "Voluntário não vinculado ao usuário autenticado." });
            }

            var calendarId = (request?.CalendarId ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(calendarId))
            {
                return BadRequest(new { mensagem = "Selecione um calendário válido." });
            }

            var conexao = await _db.VoluntariosGoogleCalendarConexoes.FirstOrDefaultAsync(x => x.VoluntarioId == voluntario.Id && x.Ativo);
            if (conexao == null)
            {
                return BadRequest(new { mensagem = "Conecte sua conta Google antes de escolher calendário." });
            }

            var accessToken = await GarantirAccessTokenValido(conexao);
            if (string.IsNullOrWhiteSpace(accessToken))
            {
                return BadRequest(new { mensagem = "Não foi possível renovar o acesso ao Google Calendar. Reconecte sua conta." });
            }

            var calendarios = await ListarCalendariosGoogle(accessToken);
            var selecionado = calendarios.FirstOrDefault(x => string.Equals(x.Id, calendarId, StringComparison.OrdinalIgnoreCase));
            if (string.IsNullOrWhiteSpace(selecionado.Id))
            {
                return BadRequest(new { mensagem = "Calendário informado não foi encontrado na sua conta Google." });
            }

            conexao.CalendarioGoogleId = selecionado.Id;
            conexao.CalendarioGoogleNome = selecionado.Summary;
            conexao.AtualizadoEm = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            return Ok(new { mensagem = "Calendário selecionado com sucesso." });
        }

        [HttpPost("sync")]
        public async Task<IActionResult> Sync()
        {
            var voluntario = await ObterVoluntarioLogado();
            if (voluntario == null)
            {
                return Unauthorized(new { mensagem = "Voluntário não vinculado ao usuário autenticado." });
            }

            var conexao = await _db.VoluntariosGoogleCalendarConexoes.FirstOrDefaultAsync(x => x.VoluntarioId == voluntario.Id && x.Ativo);
            if (conexao == null)
            {
                return BadRequest(new { mensagem = "Conecte sua conta Google antes de sincronizar." });
            }

            if (string.IsNullOrWhiteSpace(conexao.CalendarioGoogleId))
            {
                return BadRequest(new { mensagem = "Selecione um calendário de destino antes de sincronizar." });
            }

            var accessToken = await GarantirAccessTokenValido(conexao);
            if (string.IsNullOrWhiteSpace(accessToken))
            {
                return BadRequest(new { mensagem = "Não foi possível renovar o acesso ao Google Calendar. Reconecte sua conta." });
            }

            var inicio = DateTime.Today;
            var fim = inicio.AddMonths(12);
            var statusCultoAtivoId = await ObterStatusCultoAtivoId();

            var escalas = await _db.Escalas
                .AsNoTracking()
                .Where(x => x.VoluntarioId == voluntario.Id)
                .Select(x => new
                {
                    x.Id,
                    x.CultoId,
                    CultoNome = _db.Cultos.Where(c => c.Id == x.CultoId).Select(c => c.Nome).FirstOrDefault(),
                    DataCulto = _db.Cultos.Where(c => c.Id == x.CultoId).Select(c => c.DataCulto).FirstOrDefault(),
                    HorarioInicio = _db.Cultos.Where(c => c.Id == x.CultoId).Select(c => c.HorarioInicio).FirstOrDefault(),
                    StatusCultoId = _db.Cultos.Where(c => c.Id == x.CultoId).Select(c => c.StatusCultoId).FirstOrDefault(),
                    MinisterioNome = _db.Ministerios.Where(m => m.Id == x.MinisterioId).Select(m => m.Nome).FirstOrDefault(),
                    x.Funcao,
                    x.Observacoes,
                    EtapaAtividade = _db.EtapasCulto.Where(e => e.Id == x.EtapaCultoId).Select(e => e.Atividade).FirstOrDefault(),
                    DuracaoEtapa = _db.EtapasCulto.Where(e => e.Id == x.EtapaCultoId).Select(e => (int?)e.DuracaoMinutos).FirstOrDefault()
                })
                .Where(x => x.StatusCultoId == statusCultoAtivoId && x.DataCulto >= inicio && x.DataCulto <= fim)
                .OrderBy(x => x.DataCulto)
                .ThenBy(x => x.HorarioInicio)
                .ToListAsync();

            var escalaIds = escalas.Select(x => x.Id).ToHashSet();
            var mapaEventos = await _db.VoluntariosGoogleCalendarEventos
                .Where(x => x.VoluntarioId == voluntario.Id)
                .ToDictionaryAsync(x => x.EscalaId, x => x);

            var sincronizados = 0;
            var falhas = 0;

            foreach (var escala in escalas)
            {
                try
                {
                    var inicioEvento = escala.DataCulto.Date.Add(escala.HorarioInicio);
                    var fimEvento = inicioEvento.AddMinutes(Math.Max(15, escala.DuracaoEtapa ?? 90));
                    var resumo = (escala.CultoNome ?? "Culto").Trim();
                    var equipe = string.IsNullOrWhiteSpace(escala.MinisterioNome) ? "Equipe não informada" : escala.MinisterioNome.Trim();
                    var funcao = string.IsNullOrWhiteSpace(escala.Funcao) ? "Função não informada" : escala.Funcao.Trim();
                    var etapa = string.IsNullOrWhiteSpace(escala.EtapaAtividade) ? "-" : escala.EtapaAtividade.Trim();
                    var observacoes = string.IsNullOrWhiteSpace(escala.Observacoes) ? "-" : escala.Observacoes.Trim();

                    var body = new
                    {
                        summary = $"{resumo} - {equipe}",
                        description = $"Função: {funcao}\nEtapa: {etapa}\nObservações: {observacoes}",
                        start = new { dateTime = inicioEvento.ToString("yyyy-MM-dd'T'HH:mm:ss"), timeZone = "America/Sao_Paulo" },
                        end = new { dateTime = fimEvento.ToString("yyyy-MM-dd'T'HH:mm:ss"), timeZone = "America/Sao_Paulo" }
                    };

                    if (mapaEventos.TryGetValue(escala.Id, out var eventoExistente)
                        && string.Equals(eventoExistente.CalendarioGoogleId, conexao.CalendarioGoogleId, StringComparison.OrdinalIgnoreCase))
                    {
                        var patchUrl = $"{GoogleCalendarApiBase}/calendars/{Uri.EscapeDataString(conexao.CalendarioGoogleId)}/events/{Uri.EscapeDataString(eventoExistente.EventoGoogleId)}";
                        var okPatch = await EnviarJsonGoogle(accessToken, patchUrl, HttpMethod.Patch, body);
                        if (!okPatch)
                        {
                            var novoEventoId = await CriarEventoGoogle(accessToken, conexao.CalendarioGoogleId, body);
                            if (string.IsNullOrWhiteSpace(novoEventoId))
                            {
                                falhas += 1;
                                continue;
                            }

                            eventoExistente.EventoGoogleId = novoEventoId;
                        }

                        eventoExistente.UltimaSincronizacaoEm = DateTime.UtcNow;
                        eventoExistente.UltimoErroSync = null;
                        eventoExistente.AtualizadoEm = DateTime.UtcNow;
                    }
                    else
                    {
                        if (eventoExistente != null)
                        {
                            var deleteAntigoUrl = $"{GoogleCalendarApiBase}/calendars/{Uri.EscapeDataString(eventoExistente.CalendarioGoogleId)}/events/{Uri.EscapeDataString(eventoExistente.EventoGoogleId)}";
                            await EnviarGoogle(accessToken, deleteAntigoUrl, HttpMethod.Delete);
                        }

                        var eventoId = await CriarEventoGoogle(accessToken, conexao.CalendarioGoogleId, body);
                        if (string.IsNullOrWhiteSpace(eventoId))
                        {
                            falhas += 1;
                            continue;
                        }

                        if (eventoExistente == null)
                        {
                            eventoExistente = new VoluntarioGoogleCalendarEvento
                            {
                                VoluntarioId = voluntario.Id,
                                EscalaId = escala.Id,
                                CriadoEm = DateTime.UtcNow
                            };
                            _db.VoluntariosGoogleCalendarEventos.Add(eventoExistente);
                            mapaEventos[escala.Id] = eventoExistente;
                        }

                        eventoExistente.CalendarioGoogleId = conexao.CalendarioGoogleId;
                        eventoExistente.EventoGoogleId = eventoId;
                        eventoExistente.UltimaSincronizacaoEm = DateTime.UtcNow;
                        eventoExistente.UltimoErroSync = null;
                        eventoExistente.AtualizadoEm = DateTime.UtcNow;
                    }

                    sincronizados += 1;
                }
                catch (Exception ex)
                {
                    falhas += 1;
                    if (mapaEventos.TryGetValue(escala.Id, out var eventoErro))
                    {
                        eventoErro.UltimoErroSync = LimitarTexto(ex.Message, 1000);
                        eventoErro.AtualizadoEm = DateTime.UtcNow;
                    }
                }
            }

            var eventosObsoletos = mapaEventos.Values.Where(x => !escalaIds.Contains(x.EscalaId)).ToList();
            foreach (var item in eventosObsoletos)
            {
                var deleteUrl = $"{GoogleCalendarApiBase}/calendars/{Uri.EscapeDataString(item.CalendarioGoogleId)}/events/{Uri.EscapeDataString(item.EventoGoogleId)}";
                await EnviarGoogle(accessToken, deleteUrl, HttpMethod.Delete);
            }

            if (eventosObsoletos.Any())
            {
                _db.VoluntariosGoogleCalendarEventos.RemoveRange(eventosObsoletos);
            }

            conexao.UltimoSyncEm = DateTime.UtcNow;
            conexao.UltimoErroSync = falhas > 0 ? $"Sincronização concluída com {falhas} falha(s)." : null;
            conexao.AtualizadoEm = DateTime.UtcNow;

            await _db.SaveChangesAsync();
            return Ok(new
            {
                mensagem = falhas > 0
                    ? $"Sincronização concluída com {sincronizados} evento(s) atualizado(s) e {falhas} falha(s)."
                    : $"Sincronização concluída com {sincronizados} evento(s).",
                sincronizados,
                falhas
            });
        }

        [HttpPost("disconnect")]
        public async Task<IActionResult> Disconnect()
        {
            var voluntario = await ObterVoluntarioLogado();
            if (voluntario == null)
            {
                return Unauthorized(new { mensagem = "Voluntário não vinculado ao usuário autenticado." });
            }

            var conexao = await _db.VoluntariosGoogleCalendarConexoes.FirstOrDefaultAsync(x => x.VoluntarioId == voluntario.Id);
            if (conexao == null)
            {
                return Ok(new { mensagem = "Integração já estava desconectada." });
            }

            var eventos = await _db.VoluntariosGoogleCalendarEventos.Where(x => x.VoluntarioId == voluntario.Id).ToListAsync();
            if (eventos.Any())
            {
                _db.VoluntariosGoogleCalendarEventos.RemoveRange(eventos);
            }

            _db.VoluntariosGoogleCalendarConexoes.Remove(conexao);
            await _db.SaveChangesAsync();

            return Ok(new { mensagem = "Integração com Google Agenda removida com sucesso." });
        }

        private async Task<Voluntario?> ObterVoluntarioLogado()
        {
            var usuarioIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? User.FindFirstValue("nameid")
                ?? User.FindFirstValue("sub");

            if (!long.TryParse(usuarioIdClaim, out var usuarioId))
            {
                return null;
            }

            return await _db.Voluntarios.FirstOrDefaultAsync(x => x.UsuarioId == usuarioId && x.Ativo);
        }

        private async Task<long> ObterStatusCultoAtivoId()
        {
            var statusAtivoId = await _db.StatusCultos
                .Where(x => x.Codigo == "ATIVO")
                .Select(x => x.Id)
                .FirstOrDefaultAsync();

            return statusAtivoId > 0
                ? statusAtivoId
                : await _db.StatusCultos.OrderBy(x => x.Ordem).Select(x => x.Id).FirstOrDefaultAsync();
        }

        private string GerarStateOauth(long voluntarioId)
        {
            var expiraEm = DateTimeOffset.UtcNow.AddMinutes(15).ToUnixTimeSeconds();
            var nonce = Guid.NewGuid().ToString("N");
            var payload = $"{voluntarioId}|{expiraEm}|{nonce}";
            var assinatura = AssinarTexto(payload);
            return $"{Base64UrlEncode(payload)}.{Base64UrlEncode(assinatura)}";
        }

        private long? ValidarStateOauth(string state)
        {
            try
            {
                var partes = (state ?? string.Empty).Split('.');
                if (partes.Length != 2)
                {
                    return null;
                }

                var payload = Base64UrlDecodeToString(partes[0]);
                var assinatura = Base64UrlDecode(partes[1]);
                var assinaturaEsperada = AssinarTexto(payload);
                if (!CryptographicOperations.FixedTimeEquals(assinatura, assinaturaEsperada))
                {
                    return null;
                }

                var campos = payload.Split('|');
                if (campos.Length < 3)
                {
                    return null;
                }

                if (!long.TryParse(campos[0], out var voluntarioId))
                {
                    return null;
                }

                if (!long.TryParse(campos[1], out var expiraUnix))
                {
                    return null;
                }

                if (DateTimeOffset.UtcNow.ToUnixTimeSeconds() > expiraUnix)
                {
                    return null;
                }

                return voluntarioId;
            }
            catch
            {
                return null;
            }
        }

        private byte[] AssinarTexto(string texto)
        {
            var chave = (_configuration["Jwt:Key"] ?? "gestao-culto").Trim();
            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(chave));
            return hmac.ComputeHash(Encoding.UTF8.GetBytes(texto));
        }

        private static string Base64UrlEncode(string texto)
        {
            return Base64UrlEncode(Encoding.UTF8.GetBytes(texto));
        }

        private static string Base64UrlEncode(byte[] bytes)
        {
            return Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
        }

        private static byte[] Base64UrlDecode(string texto)
        {
            var normalizado = (texto ?? string.Empty).Replace('-', '+').Replace('_', '/');
            switch (normalizado.Length % 4)
            {
                case 2:
                    normalizado += "==";
                    break;
                case 3:
                    normalizado += "=";
                    break;
            }

            return Convert.FromBase64String(normalizado);
        }

        private static string Base64UrlDecodeToString(string texto)
        {
            return Encoding.UTF8.GetString(Base64UrlDecode(texto));
        }

        private async Task<GoogleTokenResposta?> TrocarCodePorToken(string code)
        {
            var clientId = (_configuration["GoogleCalendar:ClientId"] ?? _configuration["GoogleAuth:ClientId"] ?? string.Empty).Trim();
            var clientSecret = (_configuration["GoogleCalendar:ClientSecret"] ?? string.Empty).Trim();
            var redirectUri = (_configuration["GoogleCalendar:RedirectUri"] ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(clientId) || string.IsNullOrWhiteSpace(clientSecret) || string.IsNullOrWhiteSpace(redirectUri))
            {
                return null;
            }

            using var http = new HttpClient();
            using var content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["code"] = code,
                ["client_id"] = clientId,
                ["client_secret"] = clientSecret,
                ["redirect_uri"] = redirectUri,
                ["grant_type"] = "authorization_code"
            });

            var response = await http.PostAsync(GoogleOauthTokenUrl, content);
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            return new GoogleTokenResposta
            {
                AccessToken = doc.RootElement.TryGetProperty("access_token", out var access) ? access.GetString() ?? string.Empty : string.Empty,
                RefreshToken = doc.RootElement.TryGetProperty("refresh_token", out var refresh) ? refresh.GetString() : null,
                ExpiresInSeconds = doc.RootElement.TryGetProperty("expires_in", out var exp) && exp.TryGetInt32(out var expInt) ? expInt : (int?)null
            };
        }

        private async Task<(string? Email, string? Sub)?> ObterUsuarioGoogle(string accessToken)
        {
            using var http = new HttpClient();
            using var request = new HttpRequestMessage(HttpMethod.Get, "https://www.googleapis.com/oauth2/v3/userinfo");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            var response = await http.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            var email = doc.RootElement.TryGetProperty("email", out var emailNode) ? emailNode.GetString() : null;
            var sub = doc.RootElement.TryGetProperty("sub", out var subNode) ? subNode.GetString() : null;
            return (email, sub);
        }

        private async Task<string?> GarantirAccessTokenValido(VoluntarioGoogleCalendarConexao conexao)
        {
            if (!string.IsNullOrWhiteSpace(conexao.AccessToken)
                && (!conexao.AccessTokenExpiraEm.HasValue || conexao.AccessTokenExpiraEm.Value > DateTime.UtcNow.AddMinutes(1)))
            {
                return conexao.AccessToken;
            }

            if (string.IsNullOrWhiteSpace(conexao.RefreshToken))
            {
                return null;
            }

            var clientId = (_configuration["GoogleCalendar:ClientId"] ?? _configuration["GoogleAuth:ClientId"] ?? string.Empty).Trim();
            var clientSecret = (_configuration["GoogleCalendar:ClientSecret"] ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(clientId) || string.IsNullOrWhiteSpace(clientSecret))
            {
                return null;
            }

            using var http = new HttpClient();
            using var content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["client_id"] = clientId,
                ["client_secret"] = clientSecret,
                ["refresh_token"] = conexao.RefreshToken,
                ["grant_type"] = "refresh_token"
            });

            var response = await http.PostAsync(GoogleOauthTokenUrl, content);
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            var novoAccessToken = doc.RootElement.TryGetProperty("access_token", out var accessNode) ? accessNode.GetString() : null;
            var expiresIn = doc.RootElement.TryGetProperty("expires_in", out var expNode) && expNode.TryGetInt32(out var exp)
                ? exp
                : 3600;

            if (string.IsNullOrWhiteSpace(novoAccessToken))
            {
                return null;
            }

            conexao.AccessToken = novoAccessToken;
            conexao.AccessTokenExpiraEm = DateTime.UtcNow.AddSeconds(expiresIn);
            conexao.AtualizadoEm = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            return conexao.AccessToken;
        }

        private async Task<List<GoogleCalendarItem>> ListarCalendariosGoogle(string accessToken)
        {
            var url = $"{GoogleCalendarApiBase}/users/me/calendarList?minAccessRole=writer";
            using var http = new HttpClient();
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            var response = await http.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                return new List<GoogleCalendarItem>();
            }

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            var lista = new List<GoogleCalendarItem>();

            if (doc.RootElement.TryGetProperty("items", out var items) && items.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in items.EnumerateArray())
                {
                    var id = item.TryGetProperty("id", out var idNode) ? idNode.GetString() ?? string.Empty : string.Empty;
                    var summary = item.TryGetProperty("summary", out var summaryNode) ? summaryNode.GetString() ?? string.Empty : string.Empty;
                    if (!string.IsNullOrWhiteSpace(id))
                    {
                        lista.Add(new GoogleCalendarItem
                        {
                            Id = id,
                            Summary = string.IsNullOrWhiteSpace(summary) ? id : summary
                        });
                    }
                }
            }

            return lista.OrderBy(x => x.Summary).ToList();
        }

        private async Task<GoogleCalendarItem?> CriarCalendarioGoogle(string accessToken, string nome, string timeZone)
        {
            var url = $"{GoogleCalendarApiBase}/calendars";
            var body = new { summary = nome, timeZone };
            var response = await EnviarGoogle(accessToken, url, HttpMethod.Post, body);
            if (response == null)
            {
                return null;
            }

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            var id = doc.RootElement.TryGetProperty("id", out var idNode) ? idNode.GetString() : null;
            var summary = doc.RootElement.TryGetProperty("summary", out var summaryNode) ? summaryNode.GetString() : null;
            if (string.IsNullOrWhiteSpace(id))
            {
                return null;
            }

            return new GoogleCalendarItem
            {
                Id = id,
                Summary = string.IsNullOrWhiteSpace(summary) ? id : summary
            };
        }

        private async Task<string?> CriarEventoGoogle(string accessToken, string calendarId, object body)
        {
            var url = $"{GoogleCalendarApiBase}/calendars/{Uri.EscapeDataString(calendarId)}/events";
            var response = await EnviarGoogle(accessToken, url, HttpMethod.Post, body);
            if (response == null)
            {
                return null;
            }

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            return doc.RootElement.TryGetProperty("id", out var idNode) ? idNode.GetString() : null;
        }

        private async Task<bool> EnviarJsonGoogle(string accessToken, string url, HttpMethod method, object body)
        {
            var response = await EnviarGoogle(accessToken, url, method, body);
            return response != null;
        }

        private async Task<HttpResponseMessage?> EnviarGoogle(string accessToken, string url, HttpMethod method, object? body = null)
        {
            using var http = new HttpClient();
            using var request = new HttpRequestMessage(method, url);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            if (body != null)
            {
                var json = JsonSerializer.Serialize(body);
                request.Content = new StringContent(json, Encoding.UTF8, "application/json");
            }

            var response = await http.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                response.Dispose();
                return null;
            }

            return response;
        }

        private string MontarUrlFrontendRetorno(bool sucesso, string mensagem)
        {
            var baseUrl = (_configuration["GoogleCalendar:FrontendCalendarioUrl"] ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(baseUrl))
            {
                var frontendBase = (_configuration["PasswordReset:FrontendBaseUrl"] ?? string.Empty).Trim().TrimEnd('/');
                baseUrl = string.IsNullOrWhiteSpace(frontendBase)
                    ? "http://localhost:4200/voluntario/calendario"
                    : $"{frontendBase}/voluntario/calendario";
            }

            var separador = baseUrl.Contains("?") ? "&" : "?";
            var status = sucesso ? "success" : "error";
            return $"{baseUrl}{separador}googleCalendarStatus={Uri.EscapeDataString(status)}&googleCalendarMessage={Uri.EscapeDataString(mensagem)}";
        }

        private static string MontarQueryString(Dictionary<string, string> parametros)
        {
            return string.Join("&", parametros.Select(x => $"{Uri.EscapeDataString(x.Key)}={Uri.EscapeDataString(x.Value)}"));
        }

        private static string LimitarTexto(string texto, int tamanho)
        {
            if (string.IsNullOrWhiteSpace(texto))
            {
                return string.Empty;
            }

            return texto.Length <= tamanho ? texto : texto.Substring(0, tamanho);
        }

        private class GoogleTokenResposta
        {
            public string AccessToken { get; set; } = string.Empty;
            public string? RefreshToken { get; set; }
            public int? ExpiresInSeconds { get; set; }
        }

        private class GoogleCalendarItem
        {
            public string Id { get; set; } = string.Empty;
            public string Summary { get; set; } = string.Empty;
        }
    }
}
