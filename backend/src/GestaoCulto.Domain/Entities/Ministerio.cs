using GestaoCulto.Domain.Common;

namespace GestaoCulto.Domain.Entities
{
    public class Ministerio : AuditableEntity
    {
        public string Nome { get; set; } = string.Empty;
        public string Codigo { get; set; } = string.Empty;
        public string? Descricao { get; set; }
        public bool Ativo { get; set; } = true;
    }
}
