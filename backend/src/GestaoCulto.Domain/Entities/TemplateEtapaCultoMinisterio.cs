namespace GestaoCulto.Domain.Entities
{
    public class TemplateEtapaCultoMinisterio
    {
        public long TemplateEtapaCultoId { get; set; }
        public TemplateEtapaCulto TemplateEtapaCulto { get; set; } = null!;
        public long MinisterioId { get; set; }
        public Ministerio Ministerio { get; set; } = null!;
    }
}
