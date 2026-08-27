using System;
using GestaoCulto.Domain.Common;

namespace GestaoCulto.Domain.Entities
{
    public class VoluntarioTelegramVinculoToken : AuditableEntity
    {
        public long VoluntarioId { get; set; }
        public Voluntario Voluntario { get; set; } = null!;
        public string TokenHash { get; set; } = string.Empty;
        public DateTime ExpiraEm { get; set; }
        public DateTime? UsadoEm { get; set; }
        public DateTime? RevogadoEm { get; set; }
    }
}
