using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace GestaoCulto.Domain.Common
{
    public abstract class AuditableEntity
    {
        public long Id { get; set; }
        public DateTime CriadoEm { get; set; } = DateTime.UtcNow;
        public DateTime? AtualizadoEm { get; set; }
        [NotMapped]
        public long? CriadoPorId { get; set; }
        [NotMapped]
        public long? AtualizadoPorId { get; set; }
    }
}
