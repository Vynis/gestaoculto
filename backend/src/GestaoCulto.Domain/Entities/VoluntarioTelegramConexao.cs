using System;
using GestaoCulto.Domain.Common;

namespace GestaoCulto.Domain.Entities
{
    public class VoluntarioTelegramConexao : AuditableEntity
    {
        public long VoluntarioId { get; set; }
        public Voluntario Voluntario { get; set; } = null!;
        public long TelegramUserId { get; set; }
        public long TelegramChatId { get; set; }
        public string? TelegramUsername { get; set; }
        public string? TelegramPrimeiroNome { get; set; }
        public bool Ativo { get; set; } = true;
        public DateTime ConsentimentoEm { get; set; }
        public string ConsentimentoVersao { get; set; } = "1.0";
        public DateTime VinculadoEm { get; set; }
        public DateTime? UltimaInteracaoEm { get; set; }
        public DateTime? DesvinculadoEm { get; set; }
    }
}
