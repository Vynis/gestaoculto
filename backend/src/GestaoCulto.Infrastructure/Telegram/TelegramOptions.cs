namespace GestaoCulto.Infrastructure.Telegram
{
    public class TelegramOptions
    {
        public bool Enabled { get; set; }
        public string BotToken { get; set; } = string.Empty;
        public string BotUsername { get; set; } = string.Empty;
        public string WebhookSecret { get; set; } = string.Empty;
        public string WebhookUrl { get; set; } = string.Empty;
        public int LinkExpirationMinutes { get; set; } = 30;
        public string ConsentimentoVersao { get; set; } = "1.0";
    }
}
