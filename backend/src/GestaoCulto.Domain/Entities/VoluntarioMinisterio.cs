using System;

namespace GestaoCulto.Domain.Entities
{
    public class VoluntarioMinisterio
    {
        public long VoluntarioId { get; set; }
        public Voluntario Voluntario { get; set; } = null!;
        public long MinisterioId { get; set; }
        public Ministerio Ministerio { get; set; } = null!;
        public bool Principal { get; set; }
        public DateTime CriadoEm { get; set; } = DateTime.UtcNow;
    }
}
