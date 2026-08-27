using System;
using GestaoCulto.Domain.Common;

namespace GestaoCulto.Domain.Entities
{
    public class TelegramDisponibilidadeRascunho : AuditableEntity
    {
        public long VoluntarioId { get; set; }
        public Voluntario Voluntario { get; set; } = null!;
        public long CultoId { get; set; }
        public Culto Culto { get; set; } = null!;
        public DateTime ExpiraEm { get; set; }
    }
}
