using GestaoCulto.Domain.Common;

namespace GestaoCulto.Domain.Entities
{
    public class MinisterioFuncaoPadrao : AuditableEntity
    {
        public long MinisterioId { get; set; }
        public Ministerio Ministerio { get; set; } = null!;
        public string Nome { get; set; } = string.Empty;
        public int Ordem { get; set; }
        public bool Ativo { get; set; } = true;
    }
}
