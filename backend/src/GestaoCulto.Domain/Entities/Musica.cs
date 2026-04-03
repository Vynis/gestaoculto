using GestaoCulto.Domain.Common;

namespace GestaoCulto.Domain.Entities
{
    public class Musica : AuditableEntity
    {
        public string Titulo { get; set; } = string.Empty;
        public string ArtistaBanda { get; set; } = string.Empty;
        public string? Tom { get; set; }
        public string? LinkCifra { get; set; }
        public string? LinkVideo { get; set; }
        public string? Observacoes { get; set; }
        public bool Ativo { get; set; } = true;
    }
}
