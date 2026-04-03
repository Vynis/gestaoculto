using GestaoCulto.Domain.Common;

namespace GestaoCulto.Domain.Entities
{
    public class RepertorioCulto : AuditableEntity
    {
        public long CultoId { get; set; }
        public Culto Culto { get; set; } = null!;
        public string? Observacoes { get; set; }
    }
}
