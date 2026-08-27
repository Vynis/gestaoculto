using System;
using GestaoCulto.Domain.Common;

namespace GestaoCulto.Domain.Entities
{
    public class VoluntarioGoogleCalendarConexao : AuditableEntity
    {
        public long VoluntarioId { get; set; }
        public Voluntario Voluntario { get; set; } = null!;
        public string GoogleEmail { get; set; } = string.Empty;
        public string? GoogleSub { get; set; }
        public string AccessToken { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;
        public DateTime? AccessTokenExpiraEm { get; set; }
        public string? CalendarioGoogleId { get; set; }
        public string? CalendarioGoogleNome { get; set; }
        public bool Ativo { get; set; } = true;
        public DateTime? UltimoSyncEm { get; set; }
        public string? UltimoErroSync { get; set; }
    }
}
