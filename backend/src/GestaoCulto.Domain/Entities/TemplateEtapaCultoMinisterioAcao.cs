using GestaoCulto.Domain.Common;

namespace GestaoCulto.Domain.Entities
{
    public class TemplateEtapaCultoMinisterioAcao : AuditableEntity
    {
        public long TemplateEtapaCultoId { get; set; }
        public TemplateEtapaCulto TemplateEtapaCulto { get; set; } = null!;
        public long MinisterioId { get; set; }
        public Ministerio Ministerio { get; set; } = null!;
        public int? Ordem { get; set; }
        public string DescricaoAcao { get; set; } = string.Empty;
        public string? Observacao { get; set; }
        public bool Ativo { get; set; } = true;
    }
}
