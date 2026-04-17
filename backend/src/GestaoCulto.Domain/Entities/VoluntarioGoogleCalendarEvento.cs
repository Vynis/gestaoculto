using System;
using GestaoCulto.Domain.Common;

namespace GestaoCulto.Domain.Entities
{
    public class VoluntarioGoogleCalendarEvento : AuditableEntity
    {
        public long VoluntarioId { get; set; }
        public Voluntario Voluntario { get; set; } = null!;
        public long EscalaId { get; set; }
        public Escala Escala { get; set; } = null!;
        public string CalendarioGoogleId { get; set; } = string.Empty;
        public string EventoGoogleId { get; set; } = string.Empty;
        public DateTime? UltimaSincronizacaoEm { get; set; }
        public string? UltimoErroSync { get; set; }
    }
}
