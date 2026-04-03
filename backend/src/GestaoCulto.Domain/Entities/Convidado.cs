using GestaoCulto.Domain.Common;

namespace GestaoCulto.Domain.Entities
{
    public class Convidado : AuditableEntity
    {
        public long CultoId { get; set; }
        public Culto Culto { get; set; } = null!;
        public string Nome { get; set; } = string.Empty;
        public string? Telefone { get; set; }
        public string? QuemConvidou { get; set; }
        public bool PrimeiraVezIgreja { get; set; }
        public string? Observacoes { get; set; }
        public string StatusAcompanhamento { get; set; } = "PENDENTE";
    }
}
