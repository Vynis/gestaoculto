using System;
using GestaoCulto.Domain.Common;

namespace GestaoCulto.Domain.Entities
{
    public class VoluntarioAcessoRecuperacao : AuditableEntity
    {
        public long VoluntarioId { get; set; }
        public Voluntario Voluntario { get; set; } = null!;
        public string CodigoHash { get; set; } = string.Empty;
        public DateTime ExpiraEm { get; set; }
        public DateTime? UsadoEm { get; set; }
        public int Tentativas { get; set; }
        public string? Canal { get; set; }
    }
}
