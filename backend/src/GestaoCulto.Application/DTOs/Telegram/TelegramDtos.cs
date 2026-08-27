using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace GestaoCulto.Application.DTOs.Telegram
{
    public class TelegramVinculoDto
    {
        public string Url { get; set; } = string.Empty;
        public DateTime ExpiraEm { get; set; }
    }

    public class TelegramConexaoStatusDto
    {
        public bool Vinculado { get; set; }
        public bool Ativo { get; set; }
        public string? Username { get; set; }
        public DateTime? VinculadoEm { get; set; }
        public DateTime? UltimaInteracaoEm { get; set; }
    }

    public class TelegramBotaoDto
    {
        public string Texto { get; set; } = string.Empty;
        public string? CallbackData { get; set; }
        public string? Url { get; set; }
        public int Linha { get; set; }
    }

    public class TelegramUpdateDto
    {
        [JsonPropertyName("update_id")]
        public long UpdateId { get; set; }

        [JsonPropertyName("message")]
        public TelegramMensagemDto? Message { get; set; }

        [JsonPropertyName("callback_query")]
        public TelegramCallbackDto? CallbackQuery { get; set; }
    }

    public class TelegramMensagemDto
    {
        [JsonPropertyName("message_id")]
        public long MessageId { get; set; }

        [JsonPropertyName("from")]
        public TelegramUsuarioDto? From { get; set; }

        [JsonPropertyName("chat")]
        public TelegramChatDto Chat { get; set; } = new TelegramChatDto();

        [JsonPropertyName("text")]
        public string? Text { get; set; }
    }

    public class TelegramCallbackDto
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("from")]
        public TelegramUsuarioDto From { get; set; } = new TelegramUsuarioDto();

        [JsonPropertyName("message")]
        public TelegramMensagemDto? Message { get; set; }

        [JsonPropertyName("data")]
        public string? Data { get; set; }
    }

    public class TelegramUsuarioDto
    {
        [JsonPropertyName("id")]
        public long Id { get; set; }

        [JsonPropertyName("first_name")]
        public string? FirstName { get; set; }

        [JsonPropertyName("username")]
        public string? Username { get; set; }
    }

    public class TelegramChatDto
    {
        [JsonPropertyName("id")]
        public long Id { get; set; }

        [JsonPropertyName("type")]
        public string Type { get; set; } = string.Empty;
    }
}
