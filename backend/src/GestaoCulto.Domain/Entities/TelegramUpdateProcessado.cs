using System;

namespace GestaoCulto.Domain.Entities
{
    public class TelegramUpdateProcessado
    {
        public long TelegramUpdateId { get; set; }
        public DateTime RecebidoEm { get; set; } = DateTime.UtcNow;
        public string Status { get; set; } = "PROCESSANDO";
        public string? ClaimId { get; set; }
        public DateTime IniciadoEm { get; set; } = DateTime.UtcNow;
        public DateTime? ConcluidoEm { get; set; }
    }
}
