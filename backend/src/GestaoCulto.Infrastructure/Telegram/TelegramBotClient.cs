using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using GestaoCulto.Application.DTOs.Telegram;
using GestaoCulto.Application.Interfaces;
using Microsoft.Extensions.Options;

namespace GestaoCulto.Infrastructure.Telegram
{
    public class TelegramBotClient : ITelegramBotClient
    {
        private readonly HttpClient _httpClient;
        private readonly TelegramOptions _options;

        public TelegramBotClient(HttpClient httpClient, IOptions<TelegramOptions> options)
        {
            _httpClient = httpClient;
            _options = options.Value;
        }

        public Task EnviarMensagemAsync(long chatId, string texto, IReadOnlyCollection<TelegramBotaoDto>? botoes = null)
        {
            var payload = new Dictionary<string, object>
            {
                ["chat_id"] = chatId,
                ["text"] = texto
            };

            if (botoes != null && botoes.Any())
            {
                payload["reply_markup"] = CriarTeclado(botoes);
            }

            return ChamarAsync("sendMessage", payload);
        }

        public Task EditarMensagemAsync(
            long chatId,
            long messageId,
            string texto,
            IReadOnlyCollection<TelegramBotaoDto>? botoes = null)
        {
            var payload = new Dictionary<string, object>
            {
                ["chat_id"] = chatId,
                ["message_id"] = messageId,
                ["text"] = texto,
                ["reply_markup"] = CriarTeclado(botoes ?? Array.Empty<TelegramBotaoDto>())
            };

            return ChamarAsync("editMessageText", payload);
        }

        public Task ResponderCallbackAsync(string callbackId, string? texto = null)
        {
            var payload = new Dictionary<string, object>
            {
                ["callback_query_id"] = callbackId
            };

            if (!string.IsNullOrWhiteSpace(texto))
            {
                payload["text"] = texto!;
            }

            return ChamarAsync("answerCallbackQuery", payload);
        }

        public Task ConfigurarWebhookAsync()
        {
            if (string.IsNullOrWhiteSpace(_options.WebhookUrl) || string.IsNullOrWhiteSpace(_options.WebhookSecret))
            {
                throw new InvalidOperationException("Configure Telegram:WebhookUrl e Telegram:WebhookSecret.");
            }

            var payload = new Dictionary<string, object>
            {
                ["url"] = _options.WebhookUrl,
                ["secret_token"] = _options.WebhookSecret,
                ["allowed_updates"] = new[] { "message", "callback_query" }
            };

            return ChamarAsync("setWebhook", payload);
        }

        private async Task ChamarAsync(string metodo, object payload)
        {
            ValidarConfiguracao();
            var json = JsonSerializer.Serialize(payload);
            using var content = new StringContent(json, Encoding.UTF8, "application/json");
            using var response = await _httpClient.PostAsync($"./bot{_options.BotToken}/{metodo}", content);
            var responseBody = await response.Content.ReadAsStringAsync();

            TelegramApiResponse? telegramResponse = null;
            try
            {
                telegramResponse = JsonSerializer.Deserialize<TelegramApiResponse>(responseBody);
            }
            catch (JsonException)
            {
                // O status HTTP abaixo ainda produz um erro útil sem registrar o token.
            }

            if (!response.IsSuccessStatusCode || telegramResponse?.Ok != true)
            {
                var descricao = telegramResponse?.Description ?? $"HTTP {(int)response.StatusCode}";
                throw new InvalidOperationException($"Falha na API do Telegram: {descricao}");
            }
        }

        private void ValidarConfiguracao()
        {
            if (!_options.Enabled || string.IsNullOrWhiteSpace(_options.BotToken))
            {
                throw new InvalidOperationException("A integração Telegram não está habilitada ou não possui token.");
            }
        }

        private static Dictionary<string, object> CriarTeclado(IReadOnlyCollection<TelegramBotaoDto> botoes)
        {
            var linhas = botoes
                .GroupBy(x => x.Linha)
                .OrderBy(x => x.Key)
                .Select(linha => linha.Select(x =>
                {
                    var botao = new Dictionary<string, string> { ["text"] = x.Texto };
                    if (!string.IsNullOrWhiteSpace(x.Url))
                    {
                        botao["url"] = x.Url!;
                    }
                    else
                    {
                        botao["callback_data"] = x.CallbackData ?? string.Empty;
                    }
                    return botao;
                }).ToArray())
                .ToArray();

            return new Dictionary<string, object> { ["inline_keyboard"] = linhas };
        }

        private class TelegramApiResponse
        {
            [JsonPropertyName("ok")]
            public bool Ok { get; set; }

            [JsonPropertyName("description")]
            public string? Description { get; set; }
        }
    }
}
