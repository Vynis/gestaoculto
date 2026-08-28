using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using GestaoCulto.Application.DTOs.Relatorios;
using GestaoCulto.Application.DTOs.Telegram;
using GestaoCulto.Application.Interfaces;
using GestaoCulto.Domain.Entities;
using GestaoCulto.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GestaoCulto.Infrastructure.Telegram
{
    public class TelegramService : ITelegramService
    {
        private const string CallbackVincular = "vincular:";
        private const string CallbackDisponibilidade = "disp:";
        private const string CallbackRelatorio = "rel:c:";
        private const string CallbackConfirmarEscala = "esc:conf:";
        private const string CallbackRepertorio = "rp:";
        private readonly GestaoCultoDbContext _db;
        private readonly ITelegramBotClient _botClient;
        private readonly IDisponibilidadeVoluntarioService _disponibilidadeService;
        private readonly IRelatorioCompartilhamentoService _relatorioCompartilhamentoService;
        private readonly TelegramOptions _options;
        private readonly ILogger<TelegramService> _logger;

        public TelegramService(
            GestaoCultoDbContext db,
            ITelegramBotClient botClient,
            IDisponibilidadeVoluntarioService disponibilidadeService,
            IRelatorioCompartilhamentoService relatorioCompartilhamentoService,
            IOptions<TelegramOptions> options,
            ILogger<TelegramService> logger)
        {
            _db = db;
            _botClient = botClient;
            _disponibilidadeService = disponibilidadeService;
            _relatorioCompartilhamentoService = relatorioCompartilhamentoService;
            _options = options.Value;
            _logger = logger;
        }

        public async Task<TelegramVinculoDto> GerarVinculoAsync(long voluntarioId)
        {
            ValidarConfiguracao();

            using var transaction = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
            await _db.Database.ExecuteSqlRawAsync(
                "UPDATE voluntario SET id = id WHERE id = {0}",
                voluntarioId);

            var voluntario = await _db.Voluntarios
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == voluntarioId && x.Ativo);

            if (voluntario == null)
            {
                throw new InvalidOperationException("Voluntário ativo não encontrado.");
            }

            var agora = DateTime.UtcNow;
            var tokensAtivos = await _db.VoluntariosTelegramVinculoTokens
                .Where(x => x.VoluntarioId == voluntarioId
                    && x.UsadoEm == null
                    && x.RevogadoEm == null)
                .ToListAsync();

            foreach (var tokenAtivo in tokensAtivos)
            {
                tokenAtivo.RevogadoEm = agora;
                tokenAtivo.AtualizadoEm = agora;
            }

            var token = GerarToken();
            var expiraEm = agora.AddMinutes(Math.Max(5, _options.LinkExpirationMinutes));
            _db.VoluntariosTelegramVinculoTokens.Add(new VoluntarioTelegramVinculoToken
            {
                VoluntarioId = voluntarioId,
                TokenHash = CalcularHash(token),
                ExpiraEm = expiraEm
            });

            await _db.SaveChangesAsync();
            await transaction.CommitAsync();

            return new TelegramVinculoDto
            {
                Url = $"https://t.me/{_options.BotUsername}?start={token}",
                ExpiraEm = expiraEm
            };
        }

        public async Task<TelegramConexaoStatusDto> ObterStatusAsync(long voluntarioId)
        {
            var conexao = await _db.VoluntariosTelegramConexoes
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.VoluntarioId == voluntarioId);

            return new TelegramConexaoStatusDto
            {
                Vinculado = conexao != null,
                Ativo = conexao?.Ativo == true,
                Username = conexao?.TelegramUsername,
                VinculadoEm = conexao?.VinculadoEm,
                UltimaInteracaoEm = conexao?.UltimaInteracaoEm
            };
        }

        public async Task DesvincularAsync(long voluntarioId)
        {
            var conexao = await _db.VoluntariosTelegramConexoes
                .FirstOrDefaultAsync(x => x.VoluntarioId == voluntarioId && x.Ativo);

            var agora = DateTime.UtcNow;
            if (conexao != null)
            {
                conexao.Ativo = false;
                conexao.DesvinculadoEm = agora;
                conexao.AtualizadoEm = agora;
            }

            await RevogarTokensPendentesAsync(voluntarioId, agora);
            await _db.SaveChangesAsync();
        }

        public Task ConfigurarWebhookAsync()
        {
            ValidarConfiguracao();
            return _botClient.ConfigurarWebhookAsync();
        }

        public async Task ProcessarUpdateAsync(TelegramUpdateDto update)
        {
            if (update.UpdateId <= 0)
            {
                return;
            }

            var agora = DateTime.UtcNow;
            var claimId = Guid.NewGuid().ToString("N");
            var registro = new TelegramUpdateProcessado
            {
                TelegramUpdateId = update.UpdateId,
                RecebidoEm = agora,
                IniciadoEm = agora,
                Status = "PROCESSANDO",
                ClaimId = claimId
            };
            _db.TelegramUpdatesProcessados.Add(registro);

            try
            {
                await _db.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                _db.Entry(registro).State = EntityState.Detached;
                var limiteProcessamento = agora.AddMinutes(-5);
                var assumiuProcessamento = await _db.Database.ExecuteSqlRawAsync(@"
                    UPDATE telegram_update_processado
                    SET status = 'PROCESSANDO', claim_id = {1}, iniciado_em = {2}, concluido_em = NULL
                    WHERE telegram_update_id = {0}
                      AND status <> 'CONCLUIDO'
                      AND (status <> 'PROCESSANDO' OR iniciado_em IS NULL OR iniciado_em < {3})",
                    update.UpdateId,
                    claimId,
                    agora,
                    limiteProcessamento);

                if (assumiuProcessamento == 0)
                {
                    for (var tentativa = 0; tentativa < 5; tentativa++)
                    {
                        await Task.Delay(200);
                        var statusAtual = await _db.TelegramUpdatesProcessados
                            .AsNoTracking()
                            .Where(x => x.TelegramUpdateId == update.UpdateId)
                            .Select(x => x.Status)
                            .FirstOrDefaultAsync();
                        if (statusAtual == "CONCLUIDO")
                        {
                            return;
                        }

                        if (statusAtual == "ERRO")
                        {
                            break;
                        }
                    }

                    throw new TimeoutException("Outro processamento do update Telegram ainda não foi concluído.");
                }
            }

            try
            {
                if (update.CallbackQuery != null)
                {
                    await ProcessarCallbackAsync(update.CallbackQuery);
                }
                else if (update.Message != null)
                {
                    await ProcessarMensagemAsync(update.Message);
                }

                await _db.Database.ExecuteSqlRawAsync(@"
                    UPDATE telegram_update_processado
                    SET status = 'CONCLUIDO', concluido_em = {1}
                    WHERE telegram_update_id = {0} AND claim_id = {2}",
                    update.UpdateId,
                    DateTime.UtcNow,
                    claimId);
            }
            catch
            {
                await _db.Database.ExecuteSqlRawAsync(@"
                    UPDATE telegram_update_processado
                    SET status = 'ERRO'
                    WHERE telegram_update_id = {0} AND claim_id = {1}",
                    update.UpdateId,
                    claimId);
                throw;
            }
        }

        private async Task ProcessarMensagemAsync(TelegramMensagemDto mensagem)
        {
            if (mensagem.Chat.Type != "private" || mensagem.From == null || string.IsNullOrWhiteSpace(mensagem.Text))
            {
                return;
            }

            var partes = mensagem.Text.Trim().Split(new[] { ' ' }, 2, StringSplitOptions.RemoveEmptyEntries);
            var comando = partes[0].Split('@')[0].ToLowerInvariant();
            var parametro = partes.Length > 1 ? partes[1].Trim() : string.Empty;

            if (comando == "/start" && !string.IsNullOrWhiteSpace(parametro))
            {
                await IniciarVinculoAsync(mensagem.Chat.Id, parametro);
                return;
            }

            var conexao = await ObterConexaoAsync(mensagem.From.Id, mensagem.Chat.Id);
            if (conexao == null)
            {
                await _botClient.EnviarMensagemAsync(
                    mensagem.Chat.Id,
                    "Sua conta ainda não está vinculada. Solicite o link de ativação ao responsável pelas escalas.");
                return;
            }

            conexao.UltimaInteracaoEm = DateTime.UtcNow;
            conexao.AtualizadoEm = DateTime.UtcNow;

            switch (comando)
            {
                case "/start":
                case "/ajuda":
                    await _botClient.EnviarMensagemAsync(mensagem.Chat.Id, MontarAjuda());
                    break;
                case "/minhaescala":
                case "/proximas":
                    await EnviarProximasEscalasAsync(conexao.VoluntarioId, mensagem.Chat.Id);
                    break;
                case "/disponibilidade":
                    await EnviarDisponibilidadesAsync(conexao.VoluntarioId, mensagem.Chat.Id);
                    break;
                case "/relatorio":
                    await EnviarRelatoriosAsync(conexao.VoluntarioId, mensagem.Chat.Id);
                    break;
                case "/repertorio":
                    await ProcessarComandoRepertorioAsync(conexao.VoluntarioId, mensagem.Chat.Id, parametro);
                    break;
                case "/cancelar":
                    var cancelouEdicao = await CancelarEdicoesAsync(conexao.VoluntarioId);
                    await _botClient.EnviarMensagemAsync(
                        mensagem.Chat.Id,
                        cancelouEdicao
                            ? "Edição cancelada. Nenhuma alteração foi realizada."
                            : "Não há nenhuma edição em andamento.");
                    break;
                case "/desvincular":
                    conexao.Ativo = false;
                    conexao.DesvinculadoEm = DateTime.UtcNow;
                    await RevogarTokensPendentesAsync(conexao.VoluntarioId, DateTime.UtcNow);
                    await _db.SaveChangesAsync();
                    try
                    {
                        await _botClient.EnviarMensagemAsync(
                            mensagem.Chat.Id,
                            "Conta desvinculada. Para voltar a receber mensagens, solicite um novo link de ativação.");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Conta Telegram desvinculada, mas a confirmação não foi entregue.");
                    }
                    return;
                default:
                    await _botClient.EnviarMensagemAsync(mensagem.Chat.Id, MontarAjuda());
                    break;
            }

            await _db.SaveChangesAsync();
        }

        private async Task<bool> CancelarEdicoesAsync(long voluntarioId)
        {
            var agora = DateTime.UtcNow;
            var rascunhos = await _db.TelegramDisponibilidadeRascunhos
                .Where(x => x.VoluntarioId == voluntarioId && x.ExpiraEm > agora)
                .ToListAsync();

            if (!rascunhos.Any())
            {
                return false;
            }

            var rascunhoIds = rascunhos.Select(x => x.Id).ToList();
            var ministerios = await _db.TelegramDisponibilidadeRascunhosMinisterios
                .Where(x => rascunhoIds.Contains(x.TelegramDisponibilidadeRascunhoId))
                .ToListAsync();
            _db.TelegramDisponibilidadeRascunhosMinisterios.RemoveRange(ministerios);

            foreach (var rascunho in rascunhos)
            {
                rascunho.ExpiraEm = agora;
                rascunho.AtualizadoEm = agora;
            }

            return true;
        }

        private async Task IniciarVinculoAsync(long chatId, string token)
        {
            var vinculoToken = await ObterTokenValidoAsync(token);
            if (vinculoToken == null)
            {
                await _botClient.EnviarMensagemAsync(chatId, "Este link é inválido, expirou ou já foi utilizado.");
                return;
            }

            var voluntario = await _db.Voluntarios
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == vinculoToken.VoluntarioId && x.Ativo);

            if (voluntario == null)
            {
                await _botClient.EnviarMensagemAsync(chatId, "O cadastro associado a este link não está ativo.");
                return;
            }

            await _botClient.EnviarMensagemAsync(
                chatId,
                $"Olá, {voluntario.Nome}. Este bot será usado para consultar escalas, receber instruções e registrar suas respostas. Confirme para vincular sua conta do Telegram.",
                new[]
                {
                    new TelegramBotaoDto
                    {
                        Texto = "Vincular minha conta",
                        CallbackData = CallbackVincular + token
                    }
                });
        }

        private async Task ProcessarCallbackAsync(TelegramCallbackDto callback)
        {
            if (callback.Message?.Chat.Type != "private" || callback.Message.Chat.Id == 0)
            {
                await _botClient.ResponderCallbackAsync(callback.Id, "Use o bot em uma conversa privada.");
                return;
            }

            if (!string.IsNullOrWhiteSpace(callback.Data)
                && callback.Data.StartsWith(CallbackDisponibilidade, StringComparison.Ordinal))
            {
                await ProcessarDisponibilidadeCallbackAsync(callback);
                return;
            }

            if (!string.IsNullOrWhiteSpace(callback.Data)
                && callback.Data.StartsWith(CallbackRelatorio, StringComparison.Ordinal))
            {
                await ProcessarRelatorioCallbackAsync(callback);
                return;
            }

            if (!string.IsNullOrWhiteSpace(callback.Data)
                && callback.Data.StartsWith(CallbackConfirmarEscala, StringComparison.Ordinal))
            {
                await ProcessarConfirmarEscalaCallbackAsync(callback);
                return;
            }

            if (!string.IsNullOrWhiteSpace(callback.Data)
                && callback.Data.StartsWith(CallbackRepertorio, StringComparison.Ordinal))
            {
                await ProcessarRepertorioCallbackAsync(callback);
                return;
            }

            if (string.IsNullOrWhiteSpace(callback.Data) || !callback.Data.StartsWith(CallbackVincular, StringComparison.Ordinal))
            {
                await _botClient.ResponderCallbackAsync(callback.Id, "Ação inválida.");
                return;
            }

            var token = callback.Data.Substring(CallbackVincular.Length);
            var vinculoToken = await ObterTokenAsync(token);
            if (vinculoToken == null || vinculoToken.ExpiraEm <= DateTime.UtcNow || vinculoToken.RevogadoEm != null)
            {
                await _botClient.ResponderCallbackAsync(callback.Id, "Este link expirou ou já foi utilizado.");
                return;
            }

            using var transaction = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
            await _db.Database.ExecuteSqlRawAsync(
                "UPDATE voluntario_telegram_vinculo_token SET id = id WHERE id = {0}",
                vinculoToken.Id);
            await _db.Entry(vinculoToken).ReloadAsync();

            if (vinculoToken.ExpiraEm <= DateTime.UtcNow || vinculoToken.RevogadoEm != null)
            {
                await _botClient.ResponderCallbackAsync(callback.Id, "Este link expirou ou foi revogado.");
                return;
            }

            if (vinculoToken.UsadoEm != null)
            {
                var vinculoExistente = await _db.VoluntariosTelegramConexoes.AnyAsync(x =>
                    x.VoluntarioId == vinculoToken.VoluntarioId
                    && x.TelegramUserId == callback.From.Id
                    && x.Ativo);

                if (vinculoExistente)
                {
                    await _botClient.ResponderCallbackAsync(callback.Id, "Esta conta já está vinculada.");
                    return;
                }

                await _botClient.ResponderCallbackAsync(callback.Id, "Este link já foi utilizado.");
                return;
            }

            var voluntarioAtivo = await _db.Voluntarios
                .AnyAsync(x => x.Id == vinculoToken.VoluntarioId && x.Ativo);
            if (!voluntarioAtivo)
            {
                await _botClient.ResponderCallbackAsync(callback.Id, "O cadastro não está ativo.");
                return;
            }

            var conexaoDoUsuario = await _db.VoluntariosTelegramConexoes
                .FirstOrDefaultAsync(x => x.TelegramUserId == callback.From.Id);
            if (conexaoDoUsuario != null
                && conexaoDoUsuario.VoluntarioId != vinculoToken.VoluntarioId
                && conexaoDoUsuario.Ativo)
            {
                await _botClient.ResponderCallbackAsync(callback.Id, "Esta conta já está vinculada a outro cadastro.");
                return;
            }

            var conexao = await _db.VoluntariosTelegramConexoes
                .FirstOrDefaultAsync(x => x.VoluntarioId == vinculoToken.VoluntarioId);
            if (conexao != null && conexao.TelegramUserId != callback.From.Id && conexao.Ativo)
            {
                await _botClient.ResponderCallbackAsync(callback.Id, "Este voluntário já possui outra conta vinculada.");
                return;
            }

            if (conexaoDoUsuario != null && conexaoDoUsuario != conexao && !conexaoDoUsuario.Ativo)
            {
                _db.VoluntariosTelegramConexoes.Remove(conexaoDoUsuario);
            }

            var agora = DateTime.UtcNow;
            if (conexao == null)
            {
                conexao = new VoluntarioTelegramConexao
                {
                    VoluntarioId = vinculoToken.VoluntarioId,
                    TelegramUserId = callback.From.Id,
                    TelegramChatId = callback.Message.Chat.Id
                };
                _db.VoluntariosTelegramConexoes.Add(conexao);
            }

            conexao.TelegramChatId = callback.Message.Chat.Id;
            conexao.TelegramUserId = callback.From.Id;
            conexao.TelegramUsername = callback.From.Username;
            conexao.TelegramPrimeiroNome = callback.From.FirstName;
            conexao.Ativo = true;
            conexao.ConsentimentoEm = agora;
            conexao.ConsentimentoVersao = _options.ConsentimentoVersao;
            conexao.VinculadoEm = agora;
            conexao.UltimaInteracaoEm = agora;
            conexao.DesvinculadoEm = null;
            conexao.AtualizadoEm = agora;
            vinculoToken.UsadoEm = agora;
            vinculoToken.AtualizadoEm = agora;

            await _db.SaveChangesAsync();
            await transaction.CommitAsync();
            try
            {
                await _botClient.ResponderCallbackAsync(callback.Id, "Conta vinculada com sucesso.");
                await _botClient.EnviarMensagemAsync(callback.Message.Chat.Id, "Conta vinculada com sucesso.\n\n" + MontarAjuda());
            }
            catch (Exception ex)
            {
                // O vínculo já foi confirmado; uma falha de resposta não pode consumir o token novamente.
                _logger.LogWarning(ex, "Vínculo Telegram confirmado, mas a mensagem de sucesso não foi entregue.");
            }
        }

        private async Task EnviarProximasEscalasAsync(long voluntarioId, long chatId)
        {
            var hoje = DateTime.Today;
            var statusAtivoId = await _db.StatusCultos
                .Where(x => x.Codigo == "ATIVO")
                .Select(x => x.Id)
                .FirstOrDefaultAsync();

            if (statusAtivoId == 0)
            {
                statusAtivoId = await _db.StatusCultos
                    .OrderBy(x => x.Ordem)
                    .Select(x => x.Id)
                    .FirstOrDefaultAsync();
            }

            var escalas = await _db.Escalas
                .AsNoTracking()
                .Where(x => x.VoluntarioId == voluntarioId)
                .Select(x => new
                {
                    x.Id,
                    CultoNome = _db.Cultos.Where(c => c.Id == x.CultoId).Select(c => c.Nome).FirstOrDefault(),
                    DataCulto = _db.Cultos.Where(c => c.Id == x.CultoId).Select(c => c.DataCulto).FirstOrDefault(),
                    HorarioCulto = _db.Cultos.Where(c => c.Id == x.CultoId).Select(c => c.HorarioInicio).FirstOrDefault(),
                    StatusCultoId = _db.Cultos.Where(c => c.Id == x.CultoId).Select(c => c.StatusCultoId).FirstOrDefault(),
                    MinisterioNome = _db.Ministerios.Where(m => m.Id == x.MinisterioId).Select(m => m.Nome).FirstOrDefault(),
                    EtapaAtividade = _db.EtapasCulto.Where(e => e.Id == x.EtapaCultoId).Select(e => e.Atividade).FirstOrDefault(),
                    StatusPresenca = _db.PresencaEscalaStatus.Where(s => s.Id == x.PresencaStatusId).Select(s => s.Nome).FirstOrDefault(),
                    x.Funcao,
                    x.HorarioPrevisto,
                    x.Observacoes
                })
                .Where(x => x.DataCulto >= hoje && x.StatusCultoId == statusAtivoId)
                .OrderBy(x => x.DataCulto)
                .ThenBy(x => x.HorarioCulto)
                .Take(5)
                .ToListAsync();

            if (!escalas.Any())
            {
                await _botClient.EnviarMensagemAsync(chatId, "Você não possui escalas futuras no momento.");
                return;
            }

            var texto = new StringBuilder("Suas próximas escalas:\n");
            var botoes = new List<TelegramBotaoDto>();
            foreach (var escala in escalas)
            {
                var horario = escala.HorarioPrevisto?.ToString("HH:mm") ?? escala.HorarioCulto.ToString(@"hh\:mm");
                texto.Append("\n");
                texto.Append(escala.DataCulto.ToString("dd/MM/yyyy"));
                texto.Append(" às ");
                texto.Append(horario);
                texto.Append(" - ");
                texto.Append(escala.CultoNome ?? "Culto");
                texto.Append("\nFunção: ");
                texto.Append(escala.Funcao);

                if (!string.IsNullOrWhiteSpace(escala.MinisterioNome))
                {
                    texto.Append("\nMinistério: ");
                    texto.Append(escala.MinisterioNome);
                }

                if (!string.IsNullOrWhiteSpace(escala.EtapaAtividade))
                {
                    texto.Append("\nEtapa: ");
                    texto.Append(escala.EtapaAtividade);
                }

                texto.Append("\nStatus: ");
                texto.Append(escala.StatusPresenca ?? "Pendente");
                texto.Append("\n");

                if (!string.Equals(escala.StatusPresenca, "Confirmado", StringComparison.OrdinalIgnoreCase))
                {
                    botoes.Add(new TelegramBotaoDto
                    {
                        Texto = $"Confirmar {escala.DataCulto:dd/MM} {horario}",
                        CallbackData = $"{CallbackConfirmarEscala}{escala.Id}",
                        Linha = botoes.Count
                    });
                }
            }

            await _botClient.EnviarMensagemAsync(chatId, texto.ToString().TrimEnd(), botoes);
        }

        private async Task EnviarDisponibilidadesAsync(long voluntarioId, long chatId)
        {
            var cultos = await _disponibilidadeService.ListarCultosFuturosAsync(voluntarioId, 5);
            if (!cultos.Any())
            {
                await _botClient.EnviarMensagemAsync(chatId, "Não existem cultos futuros disponíveis para resposta.");
                return;
            }

            var texto = new StringBuilder("Informe sua disponibilidade para os próximos cultos:\n");
            var botoes = new List<TelegramBotaoDto>();
            for (var indice = 0; indice < cultos.Count; indice++)
            {
                var culto = cultos[indice];
                texto.Append("\n");
                texto.Append(culto.DataCulto.ToString("dd/MM"));
                texto.Append(" às ");
                texto.Append(culto.HorarioInicio.ToString(@"hh\:mm"));
                texto.Append(" - ");
                texto.Append(culto.CultoNome);
                texto.Append("\nStatus: ");
                texto.Append(culto.StatusNome);
                texto.Append("\n");
                botoes.AddRange(new[]
                {
                    new TelegramBotaoDto
                    {
                        Texto = $"{culto.DataCulto:dd/MM} Disponível",
                        CallbackData = $"disp:sim:{culto.CultoId}",
                        Linha = indice
                    },
                    new TelegramBotaoDto
                    {
                        Texto = $"{culto.DataCulto:dd/MM} Indisponível",
                        CallbackData = $"disp:nao:{culto.CultoId}",
                        Linha = indice
                    }
                });
            }

            await _botClient.EnviarMensagemAsync(chatId, texto.ToString().TrimEnd(), botoes);
        }

        private async Task EnviarRelatoriosAsync(long voluntarioId, long chatId)
        {
            var hoje = DateTime.Today;
            var cultos = await _db.Cultos
                .AsNoTracking()
                .Where(x => x.DataCulto >= hoje
                    && _db.StatusCultos.Any(s => s.Id == x.StatusCultoId && s.Codigo == "ATIVO")
                    && _db.Escalas.Any(e => e.CultoId == x.Id && e.VoluntarioId == voluntarioId))
                .OrderBy(x => x.DataCulto)
                .ThenBy(x => x.HorarioInicio)
                .Take(5)
                .Select(x => new
                {
                    x.Id,
                    x.Nome,
                    x.DataCulto,
                    x.HorarioInicio
                })
                .ToListAsync();

            if (!cultos.Any())
            {
                await _botClient.EnviarMensagemAsync(chatId, "Você não possui cultos futuros com relatório disponível.");
                return;
            }

            var botoes = cultos.Select((culto, indice) =>
            {
                var texto = $"{culto.DataCulto:dd/MM} {culto.HorarioInicio:hh\\:mm} - {culto.Nome}";
                if (texto.Length > 60)
                {
                    texto = texto.Substring(0, 57) + "...";
                }

                return new TelegramBotaoDto
                {
                    Texto = texto,
                    CallbackData = CallbackRelatorio + culto.Id,
                    Linha = indice
                };
            }).ToList();

            await _botClient.EnviarMensagemAsync(
                chatId,
                "Escolha o culto para abrir o relatório:",
                botoes);
        }

        private async Task ProcessarRelatorioCallbackAsync(TelegramCallbackDto callback)
        {
            var mensagem = callback.Message!;
            var conexao = await ObterConexaoAsync(callback.From.Id, mensagem.Chat.Id);
            if (conexao == null)
            {
                await _botClient.ResponderCallbackAsync(callback.Id, "Sua conta não está vinculada.");
                return;
            }

            var cultoTexto = (callback.Data ?? string.Empty).Substring(CallbackRelatorio.Length);
            if (!long.TryParse(cultoTexto, out var cultoId) || cultoId <= 0)
            {
                await _botClient.ResponderCallbackAsync(callback.Id, "Relatório inválido.");
                return;
            }

            RelatorioCompartilhadoLinkDto link;
            try
            {
                link = await _relatorioCompartilhamentoService.GerarLinkAsync(cultoId, conexao.VoluntarioId);
            }
            catch (InvalidOperationException ex)
            {
                await _botClient.ResponderCallbackAsync(callback.Id, ex.Message);
                return;
            }
            conexao.UltimaInteracaoEm = DateTime.UtcNow;
            conexao.AtualizadoEm = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            await _botClient.EditarMensagemAsync(
                mensagem.Chat.Id,
                mensagem.MessageId,
                "O link é pessoal, temporário e válido até algumas horas após o culto. Não o encaminhe.",
                new[]
                {
                    new TelegramBotaoDto
                    {
                        Texto = "Abrir relatório do culto",
                        Url = link.Url
                    }
                });
            await _botClient.ResponderCallbackAsync(callback.Id, "Relatório disponível.");
        }

        private async Task ProcessarComandoRepertorioAsync(long voluntarioId, long chatId, string parametro)
        {
            var cultos = await ListarCultosLouvorAsync(voluntarioId);

            if (!cultos.Any())
            {
                await _botClient.EnviarMensagemAsync(chatId, "Você não possui cultos de louvor disponíveis para repertório.");
                return;
            }

            var botoes = cultos.Select((x, i) => new TelegramBotaoDto
            {
                Texto = $"{x.DataCulto:dd/MM} {x.HorarioInicio:hh\\:mm} - {x.Nome}",
                CallbackData = CallbackRepertorio + "v:" + x.Id,
                Linha = i
            }).ToList();
            await _botClient.EnviarMensagemAsync(chatId, "Escolha o culto para visualizar o repertório:", botoes);
        }

        private async Task ProcessarRepertorioCallbackAsync(TelegramCallbackDto callback)
        {
            var mensagem = callback.Message!;
            var conexao = await ObterConexaoAsync(callback.From.Id, mensagem.Chat.Id);
            if (conexao == null)
            {
                await _botClient.ResponderCallbackAsync(callback.Id, "Sua conta não está vinculada.");
                return;
            }

            var partes = (callback.Data ?? string.Empty).Split(':');
            if (partes.Length == 2 && partes[1] == "l")
            {
                await _botClient.ResponderCallbackAsync(callback.Id, "Voltando aos cultos...");
                await ExibirListaCultosAsync(callback, conexao.VoluntarioId);
                return;
            }

            if (partes.Length < 3 || !long.TryParse(partes[2], out var cultoId) || cultoId <= 0)
            {
                await _botClient.ResponderCallbackAsync(callback.Id, "Ação de repertório inválida.");
                return;
            }

            if (partes[1] != "v")
            {
                await _botClient.ResponderCallbackAsync(callback.Id, "Ação de repertório indisponível. Use a área web para editar.");
                return;
            }

            await _botClient.ResponderCallbackAsync(callback.Id, "Abrindo repertório...");
            await ExibirRepertorioAsync(callback, conexao.VoluntarioId, cultoId);
        }

        private async Task ExibirRepertorioAsync(TelegramCallbackDto callback, long voluntarioId, long cultoId)
        {
            var culto = await _db.Cultos.AsNoTracking().FirstOrDefaultAsync(x => x.Id == cultoId);
            if (culto == null)
            {
                await _botClient.EditarMensagemAsync(callback.Message!.Chat.Id, callback.Message.MessageId, "Culto não encontrado.");
                return;
            }

            var repertorio = await _db.RepertoriosCulto.AsNoTracking().FirstOrDefaultAsync(x => x.CultoId == cultoId);
            var itens = repertorio == null
                ? new List<RepertorioTelegramItemViewModel>()
                : await _db.RepertoriosCultoItens.AsNoTracking()
                    .Where(x => x.RepertorioCultoId == repertorio.Id)
                    .OrderBy(x => x.Ordem)
                    .Select(x => new RepertorioTelegramItemViewModel
                    {
                        MusicaId = x.MusicaId,
                        MusicaTitulo = x.MusicaTitulo,
                        MusicaArtistaBanda = x.MusicaArtistaBanda,
                        MusicaLinkCifra = !string.IsNullOrWhiteSpace(x.MusicaLinkCifra)
                            ? x.MusicaLinkCifra
                            : _db.Musicas.Where(m => m.Id == x.MusicaId).Select(m => m.LinkCifra).FirstOrDefault(),
                        MusicaLinkVideo = !string.IsNullOrWhiteSpace(x.MusicaLinkVideo)
                            ? x.MusicaLinkVideo
                            : _db.Musicas.Where(m => m.Id == x.MusicaId).Select(m => m.LinkVideo).FirstOrDefault()
                    })
                    .ToListAsync();

            var texto = new StringBuilder($"{culto.Nome} - {culto.DataCulto:dd/MM/yyyy}\n\nRepertório:\n");
            for (var i = 0; i < itens.Count; i++)
            {
                var item = itens[i];
                var titulo = !string.IsNullOrWhiteSpace(item.MusicaTitulo) ? item.MusicaTitulo : $"Música #{item.MusicaId}";
                var artista = !string.IsNullOrWhiteSpace(item.MusicaArtistaBanda) ? $" - {item.MusicaArtistaBanda}" : string.Empty;
                texto.AppendLine($"{i + 1}. {titulo}{artista}");
            }

            if (!itens.Any())
            {
                texto.AppendLine("(vazio)");
            }

            var botoes = new List<TelegramBotaoDto>();
            var linhaBotao = itens.Count;
            for (var i = 0; i < itens.Count; i++)
            {
                var item = itens[i];
                var indice = i + 1;
                AdicionarBotaoUrlSeguro(botoes, $"{indice}. Cifra", item.MusicaLinkCifra, i);
                AdicionarBotaoUrlSeguro(botoes, $"{indice}. Vídeo", item.MusicaLinkVideo, i);
            }

            var escalaGerenciavelId = await ObterEscalaGerenciavelIdTelegramAsync(voluntarioId, cultoId);
            if (escalaGerenciavelId.HasValue)
            {
                botoes.Add(new TelegramBotaoDto
                {
                    Texto = "Editar na web",
                    Url = MontarUrlEdicaoRepertorio(escalaGerenciavelId.Value),
                    Linha = linhaBotao
                });
                linhaBotao++;
            }

            botoes.Add(new TelegramBotaoDto
            {
                Texto = "Voltar aos cultos",
                CallbackData = CallbackRepertorio + "l",
                Linha = linhaBotao
            });

            await _botClient.EditarMensagemAsync(callback.Message!.Chat.Id, callback.Message.MessageId, texto.ToString(), botoes);
        }

        private async Task ExibirListaCultosAsync(TelegramCallbackDto callback, long voluntarioId)
        {
            var cultos = await ListarCultosLouvorAsync(voluntarioId);
            var botoes = cultos.Select((x, i) => new TelegramBotaoDto
            {
                Texto = $"{x.DataCulto:dd/MM} {x.HorarioInicio:hh\\:mm} - {x.Nome}",
                CallbackData = CallbackRepertorio + "v:" + x.Id,
                Linha = i
            }).ToList();

            var texto = cultos.Any()
                ? "Escolha o culto para visualizar o repertório:"
                : "Você não possui cultos de louvor disponíveis para repertório.";

            await _botClient.EditarMensagemAsync(callback.Message!.Chat.Id, callback.Message.MessageId, texto, botoes);
        }

        private async Task<List<Culto>> ListarCultosLouvorAsync(long voluntarioId)
        {
            var usuarioId = await _db.Voluntarios.AsNoTracking()
                .Where(v => v.Id == voluntarioId && v.Ativo)
                .Select(v => v.UsuarioId)
                .FirstOrDefaultAsync();

            if (!usuarioId.HasValue)
            {
                return new List<Culto>();
            }

            var ehLouvor = await _db.MinisteriosVoluntarios.AsNoTracking()
                .Join(_db.Voluntarios.AsNoTracking(),
                    vm => vm.VoluntarioId,
                    v => v.Id,
                    (vm, v) => new { vm.MinisterioId, v.UsuarioId, v.Ativo })
                .Join(_db.Ministerios.AsNoTracking(),
                    x => x.MinisterioId,
                    m => m.Id,
                    (x, m) => new { x.UsuarioId, x.Ativo, m.Codigo })
                .AnyAsync(x => x.UsuarioId == usuarioId.Value && x.Ativo && x.Codigo == "LOUVOR");

            if (!ehLouvor)
            {
                return new List<Culto>();
            }

            var cultos = await _db.Cultos.AsNoTracking()
                .Join(_db.StatusCultos.AsNoTracking(),
                    culto => culto.StatusCultoId,
                    status => status.Id,
                    (culto, status) => new { culto, status })
                .Where(x => x.status.Codigo == "ATIVO")
                .Select(x => x.culto)
                .ToListAsync();

            return cultos
                .OrderByDescending(x => x.DataCulto >= DateTime.Today)
                .ThenBy(x => x.DataCulto >= DateTime.Today ? x.DataCulto : DateTime.MinValue)
                .ThenByDescending(x => x.DataCulto < DateTime.Today)
                .ThenByDescending(x => x.DataCulto)
                .ToList();
        }

        private async Task<long?> ObterEscalaGerenciavelIdTelegramAsync(long voluntarioId, long cultoId)
        {
            return await _db.Escalas.AsNoTracking()
                .Where(x => x.CultoId == cultoId
                    && x.VoluntarioId == voluntarioId
                    && x.PodeGerenciarRepertorio
                    && x.Ministerio != null && x.Ministerio.Codigo == "LOUVOR"
                    && x.Voluntario != null && x.Voluntario.Ativo
                    && x.Culto != null
                    && x.Culto.DataCulto >= DateTime.Today
                    && _db.StatusCultos.Any(s => s.Id == x.Culto.StatusCultoId && s.Codigo == "ATIVO"))
                .Select(x => (long?)x.Id)
                .FirstOrDefaultAsync();
        }

        private string MontarUrlEdicaoRepertorio(long escalaId)
        {
            var baseUrl = (_options.FrontendBaseUrl ?? string.Empty).Trim().TrimEnd('/');
            return $"{baseUrl}/voluntario/repertorio/escala/{escalaId}";
        }

        private static bool AdicionarBotaoUrlSeguro(List<TelegramBotaoDto> botoes, string texto, string? url, int linha)
        {
            var urlNormalizada = NormalizarUrl(url);
            if (urlNormalizada == null)
            {
                return false;
            }

            botoes.Add(new TelegramBotaoDto
            {
                Texto = texto,
                Url = urlNormalizada,
                Linha = linha
            });

            return true;
        }

        private static string? NormalizarUrl(string? url)
        {
            if (string.IsNullOrWhiteSpace(url))
            {
                return null;
            }

            return Uri.TryCreate(url.Trim(), UriKind.Absolute, out var uri) &&
                (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps)
                ? uri.ToString()
                : null;
        }

        private sealed class RepertorioTelegramItemViewModel
        {
            public long MusicaId { get; set; }
            public string? MusicaTitulo { get; set; }
            public string? MusicaArtistaBanda { get; set; }
            public string? MusicaLinkCifra { get; set; }
            public string? MusicaLinkVideo { get; set; }
        }

        private async Task<Culto?> ObterCultoRepertorioAutorizadoAsync(long voluntarioId, long cultoId)
        {
            return await _db.Cultos.FirstOrDefaultAsync(x => x.Id == cultoId
                && x.DataCulto >= DateTime.Today
                && _db.StatusCultos.Any(s => s.Id == x.StatusCultoId && s.Codigo == "ATIVO")
                && _db.Escalas.Any(e => e.CultoId == cultoId && e.VoluntarioId == voluntarioId && e.PodeGerenciarRepertorio));
        }

        private async Task<TelegramRepertorioRascunho> ObterOuCriarRascunhoRepertorioAsync(long voluntarioId, long cultoId)
        {
            var rascunho = await _db.TelegramRepertoriosRascunhos
                .FirstOrDefaultAsync(x => x.VoluntarioId == voluntarioId && x.CultoId == cultoId);
            var expirado = rascunho != null && rascunho.ExpiraEm <= DateTime.UtcNow;
            if (rascunho == null || expirado)
            {
                var repertorio = await _db.RepertoriosCulto.AsNoTracking().FirstOrDefaultAsync(x => x.CultoId == cultoId);
                var ids = repertorio == null
                    ? new List<long>()
                    : await _db.RepertoriosCultoItens.AsNoTracking().Where(x => x.RepertorioCultoId == repertorio.Id).OrderBy(x => x.Ordem).Select(x => x.MusicaId).ToListAsync();
                if (rascunho == null)
                {
                    rascunho = new TelegramRepertorioRascunho
                    {
                        VoluntarioId = voluntarioId,
                        CultoId = cultoId
                    };
                    _db.TelegramRepertoriosRascunhos.Add(rascunho);
                }
                rascunho.MusicaIds = string.Join(",", ids);
                rascunho.AcaoPendente = null;
                rascunho.ExpiraEm = DateTime.UtcNow.AddMinutes(30);
            }
            else
            {
                rascunho.ExpiraEm = DateTime.UtcNow.AddMinutes(30);
                rascunho.AtualizadoEm = DateTime.UtcNow;
            }
            await _db.SaveChangesAsync();
            return rascunho;
        }

        private async Task<string?> SalvarRepertorioTelegramAsync(long voluntarioId, long cultoId, List<long> ids)
        {
            if (await ObterCultoRepertorioAutorizadoAsync(voluntarioId, cultoId) == null)
            {
                return "Você não está autorizado para este culto.";
            }
            var musicas = await _db.Musicas.Where(x => ids.Contains(x.Id) && x.Ativo).ToListAsync();
            if (musicas.Count != ids.Count) return "Uma música não está mais disponível.";
            if (ids.Count != ids.Distinct().Count()) return "Não é possível repetir uma música no repertório.";
            using var transacao = await _db.Database.BeginTransactionAsync();
            var repertorio = await _db.RepertoriosCulto.FirstOrDefaultAsync(x => x.CultoId == cultoId);
            if (repertorio == null)
            {
                repertorio = new RepertorioCulto { CultoId = cultoId };
                _db.RepertoriosCulto.Add(repertorio);
                await _db.SaveChangesAsync();
            }
            var atuais = await _db.RepertoriosCultoItens.Where(x => x.RepertorioCultoId == repertorio.Id).ToListAsync();
            _db.RepertoriosCultoItens.RemoveRange(atuais);
            for (var i = 0; i < ids.Count; i++)
            {
                var musica = musicas.First(x => x.Id == ids[i]);
                _db.RepertoriosCultoItens.Add(new RepertorioCultoItem
                {
                    RepertorioCultoId = repertorio.Id, MusicaId = musica.Id, Ordem = i + 1,
                    MusicaTitulo = musica.Titulo, MusicaArtistaBanda = musica.ArtistaBanda,
                    MusicaTom = musica.Tom, MusicaLinkCifra = musica.LinkCifra,
                    MusicaLinkVideo = musica.LinkVideo, MusicaObservacoes = musica.Observacoes,
                    CriadoEm = DateTime.UtcNow
                });
            }
            repertorio.AtualizadoEm = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            await transacao.CommitAsync();
            return null;
        }

        private static List<long> LerIds(string valor)
        {
            return valor.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(x => long.TryParse(x, out var id) ? id : 0).Where(x => x > 0).Distinct().ToList();
        }

        private static void Mover(List<long> ids, long id, int deslocamento)
        {
            var indice = ids.IndexOf(id);
            var destino = indice + deslocamento;
            if (indice >= 0 && destino >= 0 && destino < ids.Count)
            {
                ids[indice] = ids[destino];
                ids[destino] = id;
            }
        }

        private async Task CadastrarMusicaTelegramAsync(long chatId, TelegramRepertorioRascunho rascunho, string texto)
        {
            if (await ObterCultoRepertorioAutorizadoAsync(rascunho.VoluntarioId, rascunho.CultoId) == null)
            {
                rascunho.AcaoPendente = null;
                await _db.SaveChangesAsync();
                await _botClient.EnviarMensagemAsync(chatId, "Você não está autorizado para este culto.");
                return;
            }
            var partes = texto.Split(new[] { '|' }, 2);
            if (partes.Length != 2 || string.IsNullOrWhiteSpace(partes[0]) || string.IsNullOrWhiteSpace(partes[1]))
            {
                await _botClient.EnviarMensagemAsync(chatId, "Formato inválido. Use: Título | Artista/Banda");
                return;
            }
            var titulo = partes[0].Trim();
            var artista = partes[1].Trim();
            if (await _db.Musicas.AnyAsync(x => x.Titulo.ToLower() == titulo.ToLower() && x.ArtistaBanda.ToLower() == artista.ToLower()))
            {
                await _botClient.EnviarMensagemAsync(chatId, "Esta música já está cadastrada. Use a busca para adicioná-la.");
                return;
            }
            var musica = new Musica { Titulo = titulo, ArtistaBanda = artista, Ativo = true, CriadoEm = DateTime.UtcNow };
            _db.Musicas.Add(musica);
            await _db.SaveChangesAsync();
            var ids = LerIds(rascunho.MusicaIds);
            ids.Add(musica.Id);
            rascunho.MusicaIds = string.Join(",", ids);
            rascunho.AcaoPendente = null;
            rascunho.ExpiraEm = DateTime.UtcNow.AddMinutes(30);
            await _db.SaveChangesAsync();
            await _botClient.EnviarMensagemAsync(chatId, $"Música adicionada: {musica.Titulo}. Use o botão Salvar repertório.");
        }

        private async Task ProcessarConfirmarEscalaCallbackAsync(TelegramCallbackDto callback)
        {
            var mensagem = callback.Message!;
            var conexao = await ObterConexaoAsync(callback.From.Id, mensagem.Chat.Id);
            if (conexao == null)
            {
                await _botClient.ResponderCallbackAsync(callback.Id, "Sua conta não está vinculada.");
                return;
            }

            var escalaTexto = (callback.Data ?? string.Empty).Substring(CallbackConfirmarEscala.Length);
            if (!long.TryParse(escalaTexto, out var escalaId) || escalaId <= 0)
            {
                await _botClient.ResponderCallbackAsync(callback.Id, "Escala inválida.");
                return;
            }

            var erro = await ConfirmarEscalaAsync(conexao.VoluntarioId, escalaId);
            if (!string.IsNullOrWhiteSpace(erro))
            {
                await _botClient.ResponderCallbackAsync(callback.Id, erro);
                return;
            }

            conexao.UltimaInteracaoEm = DateTime.UtcNow;
            conexao.AtualizadoEm = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            await _botClient.ResponderCallbackAsync(callback.Id, "Escala confirmada com sucesso.");
            await _botClient.EditarMensagemAsync(
                mensagem.Chat.Id,
                mensagem.MessageId,
                "Escala confirmada com sucesso.\n\nAtualizando suas próximas escalas...");

            await EnviarProximasEscalasAsync(conexao.VoluntarioId, mensagem.Chat.Id);
        }

        private async Task ProcessarDisponibilidadeCallbackAsync(TelegramCallbackDto callback)
        {
            var mensagem = callback.Message!;
            var conexao = await ObterConexaoAsync(callback.From.Id, mensagem.Chat.Id);
            if (conexao == null)
            {
                await _botClient.ResponderCallbackAsync(callback.Id, "Sua conta não está vinculada.");
                return;
            }

            var partes = (callback.Data ?? string.Empty).Split(':');
            if (partes.Length < 3 || !long.TryParse(partes[2], out var cultoId) || cultoId <= 0)
            {
                await _botClient.ResponderCallbackAsync(callback.Id, "Ação de disponibilidade inválida.");
                return;
            }

            conexao.UltimaInteracaoEm = DateTime.UtcNow;
            conexao.AtualizadoEm = DateTime.UtcNow;

            switch (partes[1])
            {
                case "sim":
                    await PrepararRascunhoAsync(conexao.VoluntarioId, cultoId);
                    await TentarExibirSelecaoMinisteriosAsync(callback, conexao.VoluntarioId, cultoId);
                    return;
                case "nao":
                    await SalvarIndisponibilidadeAsync(conexao.VoluntarioId, cultoId);
                    await TentarFinalizarRespostaDisponibilidadeAsync(
                        callback,
                        conexao.VoluntarioId,
                        cultoId,
                        "Indisponibilidade registrada.");
                    return;
                case "min":
                    if (partes.Length != 4 || !long.TryParse(partes[3], out var ministerioId) || ministerioId <= 0)
                    {
                        await _botClient.ResponderCallbackAsync(callback.Id, "Ministério inválido.");
                        return;
                    }

                    await AlternarMinisterioRascunhoAsync(conexao.VoluntarioId, cultoId, ministerioId);
                    await TentarExibirSelecaoMinisteriosAsync(callback, conexao.VoluntarioId, cultoId);
                    return;
                case "ok":
                    var erroConfirmacao = await ConfirmarDisponibilidadeRascunhoAsync(
                        conexao.VoluntarioId,
                        cultoId);
                    if (!string.IsNullOrWhiteSpace(erroConfirmacao))
                    {
                        await _botClient.ResponderCallbackAsync(callback.Id, erroConfirmacao);
                        return;
                    }
                    await TentarFinalizarRespostaDisponibilidadeAsync(
                        callback,
                        conexao.VoluntarioId,
                        cultoId,
                        "Disponibilidade registrada.");
                    return;
                default:
                    await _botClient.ResponderCallbackAsync(callback.Id, "Ação de disponibilidade inválida.");
                    return;
            }
        }

        private async Task<TelegramDisponibilidadeRascunho> PrepararRascunhoAsync(long voluntarioId, long cultoId)
        {
            var detalhe = await _disponibilidadeService.ObterDetalheAsync(voluntarioId, cultoId);
            var agora = DateTime.UtcNow;
            using var transaction = await _db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
            await _db.Database.ExecuteSqlRawAsync(
                "UPDATE voluntario SET id = id WHERE id = {0}",
                voluntarioId);
            var rascunho = await _db.TelegramDisponibilidadeRascunhos
                .FirstOrDefaultAsync(x => x.VoluntarioId == voluntarioId && x.CultoId == cultoId);
            var deveInicializar = rascunho == null || rascunho.ExpiraEm <= agora;

            if (rascunho == null)
            {
                rascunho = new TelegramDisponibilidadeRascunho
                {
                    VoluntarioId = voluntarioId,
                    CultoId = cultoId,
                    ExpiraEm = agora.AddMinutes(30)
                };
                _db.TelegramDisponibilidadeRascunhos.Add(rascunho);
                await _db.SaveChangesAsync();
            }
            else
            {
                rascunho.ExpiraEm = agora.AddMinutes(30);
                rascunho.AtualizadoEm = agora;
            }

            if (deveInicializar)
            {
                var anteriores = await _db.TelegramDisponibilidadeRascunhosMinisterios
                    .Where(x => x.TelegramDisponibilidadeRascunhoId == rascunho.Id)
                    .ToListAsync();
                _db.TelegramDisponibilidadeRascunhosMinisterios.RemoveRange(anteriores);

                var permitidos = detalhe.MinisteriosPermitidos.Select(x => x.MinisterioId).ToHashSet();
                var selecionados = detalhe.MinisterioIdsSelecionados
                    .Where(permitidos.Contains)
                    .Distinct()
                    .Select(id => new TelegramDisponibilidadeRascunhoMinisterio
                    {
                        TelegramDisponibilidadeRascunhoId = rascunho.Id,
                        MinisterioId = id
                    });
                _db.TelegramDisponibilidadeRascunhosMinisterios.AddRange(selecionados);
            }

            await _db.SaveChangesAsync();
            await transaction.CommitAsync();
            return rascunho;
        }

        private async Task SalvarIndisponibilidadeAsync(long voluntarioId, long cultoId)
        {
            using var transaction = await _db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
            await _db.Database.ExecuteSqlRawAsync(
                "UPDATE voluntario SET id = id WHERE id = {0}",
                voluntarioId);
            await _disponibilidadeService.SalvarAsync(
                voluntarioId,
                cultoId,
                false,
                Array.Empty<long>(),
                null);

            var rascunho = await _db.TelegramDisponibilidadeRascunhos
                .FirstOrDefaultAsync(x => x.VoluntarioId == voluntarioId && x.CultoId == cultoId);
            if (rascunho != null)
            {
                rascunho.ExpiraEm = DateTime.UtcNow;
                rascunho.AtualizadoEm = DateTime.UtcNow;
                await _db.SaveChangesAsync();
            }

            await transaction.CommitAsync();
        }

        private async Task<string?> ConfirmarDisponibilidadeRascunhoAsync(long voluntarioId, long cultoId)
        {
            using var transaction = await _db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
            await _db.Database.ExecuteSqlRawAsync(
                "UPDATE voluntario SET id = id WHERE id = {0}",
                voluntarioId);
            var rascunho = await _db.TelegramDisponibilidadeRascunhos
                .FirstOrDefaultAsync(x => x.VoluntarioId == voluntarioId && x.CultoId == cultoId);
            if (rascunho == null || rascunho.ExpiraEm <= DateTime.UtcNow)
            {
                return "A seleção expirou. Escolha Disponível novamente.";
            }

            await _db.Database.ExecuteSqlRawAsync(
                "UPDATE telegram_disponibilidade_rascunho SET id = id WHERE id = {0}",
                rascunho.Id);
            await _db.Entry(rascunho).ReloadAsync();
            if (rascunho.ExpiraEm <= DateTime.UtcNow)
            {
                return "A seleção expirou. Escolha Disponível novamente.";
            }

            var ministerioIds = await _db.TelegramDisponibilidadeRascunhosMinisterios
                .Where(x => x.TelegramDisponibilidadeRascunhoId == rascunho.Id)
                .Select(x => x.MinisterioId)
                .ToListAsync();
            if (!ministerioIds.Any())
            {
                return "Selecione ao menos um ministério.";
            }

            await _disponibilidadeService.SalvarAsync(
                voluntarioId,
                cultoId,
                true,
                ministerioIds,
                null);
            rascunho.ExpiraEm = DateTime.UtcNow;
            rascunho.AtualizadoEm = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            await transaction.CommitAsync();
            return null;
        }

        private async Task AlternarMinisterioRascunhoAsync(long voluntarioId, long cultoId, long ministerioId)
        {
            var rascunho = await PrepararRascunhoAsync(voluntarioId, cultoId);
            using var transaction = await _db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
            await _db.Database.ExecuteSqlRawAsync(
                "UPDATE telegram_disponibilidade_rascunho SET id = id WHERE id = {0}",
                rascunho.Id);
            await _db.Entry(rascunho).ReloadAsync();
            var detalhe = await _disponibilidadeService.ObterDetalheAsync(voluntarioId, cultoId);
            if (!detalhe.MinisteriosPermitidos.Any(x => x.MinisterioId == ministerioId))
            {
                throw new InvalidOperationException("Ministério inválido para este voluntário.");
            }

            if (rascunho.ExpiraEm <= DateTime.UtcNow)
            {
                var anteriores = await _db.TelegramDisponibilidadeRascunhosMinisterios
                    .Where(x => x.TelegramDisponibilidadeRascunhoId == rascunho.Id)
                    .ToListAsync();
                _db.TelegramDisponibilidadeRascunhosMinisterios.RemoveRange(anteriores);
                var permitidos = detalhe.MinisteriosPermitidos.Select(x => x.MinisterioId).ToHashSet();
                _db.TelegramDisponibilidadeRascunhosMinisterios.AddRange(
                    detalhe.MinisterioIdsSelecionados
                        .Where(permitidos.Contains)
                        .Distinct()
                        .Select(id => new TelegramDisponibilidadeRascunhoMinisterio
                        {
                            TelegramDisponibilidadeRascunhoId = rascunho.Id,
                            MinisterioId = id
                        }));
                rascunho.ExpiraEm = DateTime.UtcNow.AddMinutes(30);
                rascunho.AtualizadoEm = DateTime.UtcNow;
                await _db.SaveChangesAsync();
            }

            var selecionado = await _db.TelegramDisponibilidadeRascunhosMinisterios
                .FirstOrDefaultAsync(x => x.TelegramDisponibilidadeRascunhoId == rascunho.Id
                    && x.MinisterioId == ministerioId);

            if (selecionado == null)
            {
                _db.TelegramDisponibilidadeRascunhosMinisterios.Add(
                    new TelegramDisponibilidadeRascunhoMinisterio
                    {
                        TelegramDisponibilidadeRascunhoId = rascunho.Id,
                        MinisterioId = ministerioId
                    });
            }
            else
            {
                _db.TelegramDisponibilidadeRascunhosMinisterios.Remove(selecionado);
            }

            await _db.SaveChangesAsync();
            await transaction.CommitAsync();
        }

        private async Task TentarExibirSelecaoMinisteriosAsync(
            TelegramCallbackDto callback,
            long voluntarioId,
            long cultoId)
        {
            try
            {
                await ExibirSelecaoMinisteriosAsync(callback, voluntarioId, cultoId);
            }
            catch (Exception ex)
            {
                // A seleção já foi persistida; o mesmo update não pode alterná-la novamente em um retry.
                _logger.LogWarning(ex, "Seleção de disponibilidade salva, mas a mensagem Telegram não foi atualizada.");
            }
        }

        private async Task ExibirSelecaoMinisteriosAsync(
            TelegramCallbackDto callback,
            long voluntarioId,
            long cultoId)
        {
            var detalhe = await _disponibilidadeService.ObterDetalheAsync(voluntarioId, cultoId);
            var rascunho = await _db.TelegramDisponibilidadeRascunhos
                .AsNoTracking()
                .FirstAsync(x => x.VoluntarioId == voluntarioId && x.CultoId == cultoId);
            var selecionados = await _db.TelegramDisponibilidadeRascunhosMinisterios
                .AsNoTracking()
                .Where(x => x.TelegramDisponibilidadeRascunhoId == rascunho.Id)
                .Select(x => x.MinisterioId)
                .ToListAsync();
            var selecionadosSet = selecionados.ToHashSet();
            var botoes = detalhe.MinisteriosPermitidos
                .Select((ministerio, indice) => new TelegramBotaoDto
                {
                    Texto = $"{(selecionadosSet.Contains(ministerio.MinisterioId) ? "[x]" : "[ ]")} {ministerio.MinisterioNome}",
                    CallbackData = $"disp:min:{cultoId}:{ministerio.MinisterioId}",
                    Linha = indice / 2
                })
                .ToList();

            if (detalhe.MinisteriosPermitidos.Any())
            {
                botoes.Add(new TelegramBotaoDto
                {
                    Texto = "Confirmar disponibilidade",
                    CallbackData = $"disp:ok:{cultoId}",
                    Linha = (detalhe.MinisteriosPermitidos.Count + 1) / 2
                });
            }

            var texto = MontarResumoDisponibilidade(
                detalhe.Culto.CultoNome,
                detalhe.Culto.DataCulto,
                detalhe.Culto.HorarioInicio,
                detalhe.Culto.StatusNome)
                + "\n\nSelecione um ou mais ministérios e confirme.";
            if (!detalhe.MinisteriosPermitidos.Any())
            {
                texto += "\nVocê não possui ministérios vinculados ao cadastro.";
            }

            await _db.SaveChangesAsync();
            await _botClient.ResponderCallbackAsync(callback.Id, "Seleção atualizada.");
            await _botClient.EditarMensagemAsync(
                callback.Message!.Chat.Id,
                callback.Message.MessageId,
                texto,
                botoes);
        }

        private async Task EditarRespostaDisponibilidadeAsync(
            TelegramCallbackDto callback,
            long voluntarioId,
            long cultoId)
        {
            var detalhe = await _disponibilidadeService.ObterDetalheAsync(voluntarioId, cultoId);
            var ministerios = detalhe.MinisterioIdsSelecionados.Any()
                ? await _db.Ministerios
                    .AsNoTracking()
                    .Where(x => detalhe.MinisterioIdsSelecionados.Contains(x.Id))
                    .OrderBy(x => x.Nome)
                    .Select(x => x.Nome)
                    .ToListAsync()
                : new List<string>();
            var texto = MontarResumoDisponibilidade(
                detalhe.Culto.CultoNome,
                detalhe.Culto.DataCulto,
                detalhe.Culto.HorarioInicio,
                detalhe.Culto.StatusNome);
            if (ministerios.Any())
            {
                texto += "\nMinistérios: " + string.Join(", ", ministerios);
            }

            await _botClient.EditarMensagemAsync(
                callback.Message!.Chat.Id,
                callback.Message.MessageId,
                texto);
        }

        private async Task TentarFinalizarRespostaDisponibilidadeAsync(
            TelegramCallbackDto callback,
            long voluntarioId,
            long cultoId,
            string mensagemCallback)
        {
            try
            {
                await _botClient.ResponderCallbackAsync(callback.Id, mensagemCallback);
                await EditarRespostaDisponibilidadeAsync(callback, voluntarioId, cultoId);
            }
            catch (Exception ex)
            {
                // A resposta de domínio já foi persistida e não deve ser reexecutada por falha visual.
                _logger.LogWarning(ex, "Disponibilidade salva, mas a confirmação Telegram não foi atualizada.");
            }
        }

        private static string MontarResumoDisponibilidade(
            string cultoNome,
            DateTime dataCulto,
            TimeSpan horarioInicio,
            string statusNome)
        {
            return $"{cultoNome}\n{dataCulto:dd/MM/yyyy} às {horarioInicio:hh\\:mm}\nStatus: {statusNome}";
        }

        private async Task<VoluntarioTelegramConexao?> ObterConexaoAsync(long telegramUserId, long chatId)
        {
            return await _db.VoluntariosTelegramConexoes
                .FirstOrDefaultAsync(x => x.TelegramUserId == telegramUserId
                    && x.TelegramChatId == chatId
                    && x.Ativo
                    && _db.Voluntarios.Any(v => v.Id == x.VoluntarioId && v.Ativo));
        }

        private async Task RevogarTokensPendentesAsync(long voluntarioId, DateTime agora)
        {
            var tokens = await _db.VoluntariosTelegramVinculoTokens
                .Where(x => x.VoluntarioId == voluntarioId
                    && x.UsadoEm == null
                    && x.RevogadoEm == null)
                .ToListAsync();

            foreach (var token in tokens)
            {
                token.RevogadoEm = agora;
                token.AtualizadoEm = agora;
            }
        }

        private async Task<VoluntarioTelegramVinculoToken?> ObterTokenValidoAsync(string token)
        {
            var agora = DateTime.UtcNow;
            var vinculoToken = await ObterTokenAsync(token);
            return vinculoToken != null
                && vinculoToken.ExpiraEm > agora
                && vinculoToken.UsadoEm == null
                && vinculoToken.RevogadoEm == null
                    ? vinculoToken
                    : null;
        }

        private async Task<VoluntarioTelegramVinculoToken?> ObterTokenAsync(string token)
        {
            var hash = CalcularHash(token);
            return await _db.VoluntariosTelegramVinculoTokens
                .FirstOrDefaultAsync(x => x.TokenHash == hash);
        }

        private async Task<string?> ConfirmarEscalaAsync(long voluntarioId, long escalaId)
        {
            using var transaction = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable);

            var escala = await _db.Escalas
                .FirstOrDefaultAsync(x => x.Id == escalaId && x.VoluntarioId == voluntarioId);

            if (escala == null)
            {
                return "Escala não encontrada para o seu cadastro.";
            }

            if (escala.PresencaStatusId == 2)
            {
                return "Essa escala já está confirmada.";
            }

            escala.PresencaStatusId = 2;
            escala.ConfirmadoEm = DateTime.UtcNow;
            escala.AtualizadoEm = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            await transaction.CommitAsync();
            return null;
        }

        private void ValidarConfiguracao()
        {
            if (!_options.Enabled
                || string.IsNullOrWhiteSpace(_options.BotToken)
                || string.IsNullOrWhiteSpace(_options.BotUsername))
            {
                throw new InvalidOperationException("A integração Telegram não está configurada.");
            }
        }

        private static string MontarAjuda()
        {
            return "Comandos disponíveis:\n/minhaescala - consultar e confirmar suas escalas pendentes\n/proximas - listar próximas escalas\n/disponibilidade - informar disponibilidade\n/relatorio - abrir relatório de um culto\n/repertorio - gerenciar repertório dos cultos autorizados\n/cancelar - cancelar uma edição em andamento\n/desvincular - remover o vínculo\n/ajuda - exibir esta mensagem";
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
