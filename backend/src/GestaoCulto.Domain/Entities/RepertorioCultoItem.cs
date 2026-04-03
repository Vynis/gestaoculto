using GestaoCulto.Domain.Common;

namespace GestaoCulto.Domain.Entities
{
    public class RepertorioCultoItem : AuditableEntity
    {
        public long RepertorioCultoId { get; set; }
        public RepertorioCulto RepertorioCulto { get; set; } = null!;
        public long MusicaId { get; set; }
        public Musica Musica { get; set; } = null!;
        public long? EtapaCultoId { get; set; }
        public EtapaCulto? EtapaCulto { get; set; }
        public int Ordem { get; set; }
        public string? Responsavel { get; set; }
        public string? Observacoes { get; set; }
    }
}
