using System;
using GestaoCulto.Domain.Common;

namespace GestaoCulto.Domain.Entities
{
    public class DisponibilidadeCultoVoluntario : AuditableEntity
    {
        public long CultoId { get; set; }
        public Culto Culto { get; set; } = null!;
        public long VoluntarioId { get; set; }
        public Voluntario Voluntario { get; set; } = null!;
        public long StatusDisponibilidadeId { get; set; }
        public StatusDisponibilidadeVoluntario StatusDisponibilidade { get; set; } = null!;
        public string? Observacao { get; set; }
        public DateTime RespondidoEm { get; set; }
    }
}
