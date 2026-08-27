using System;
using GestaoCulto.Domain.Common;

namespace GestaoCulto.Domain.Entities
{
    public class RelatorioCultoCompartilhamento : AuditableEntity
    {
        public long CultoId { get; set; }
        public Culto Culto { get; set; } = null!;
        public long VoluntarioId { get; set; }
        public Voluntario Voluntario { get; set; } = null!;
        public string TokenHash { get; set; } = string.Empty;
        public DateTime ExpiraEm { get; set; }
        public DateTime? UltimoAcessoEm { get; set; }
        public DateTime? RevogadoEm { get; set; }
    }
}
