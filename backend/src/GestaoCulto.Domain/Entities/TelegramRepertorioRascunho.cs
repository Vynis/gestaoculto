using System;
using GestaoCulto.Domain.Common;

namespace GestaoCulto.Domain.Entities
{
    public class TelegramRepertorioRascunho : AuditableEntity
    {
        public long VoluntarioId { get; set; }
        public Voluntario Voluntario { get; set; } = null!;
        public long CultoId { get; set; }
        public Culto Culto { get; set; } = null!;
        public string MusicaIds { get; set; } = string.Empty;
        public string? AcaoPendente { get; set; }
        public DateTime ExpiraEm { get; set; }
    }
}
