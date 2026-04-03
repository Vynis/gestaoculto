using GestaoCulto.Domain.Common;

namespace GestaoCulto.Domain.Entities
{
    public class TemplateCulto : AuditableEntity
    {
        public string Nome { get; set; } = string.Empty;
        public string TipoCulto { get; set; } = string.Empty;
        public string? Descricao { get; set; }
        public bool Ativo { get; set; } = true;
    }
}
