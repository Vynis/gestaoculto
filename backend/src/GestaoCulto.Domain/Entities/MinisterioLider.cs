using System;

namespace GestaoCulto.Domain.Entities
{
    public class MinisterioLider
    {
        public long MinisterioId { get; set; }
        public Ministerio Ministerio { get; set; } = null!;
        public long UsuarioId { get; set; }
        public Usuario Usuario { get; set; } = null!;
        public bool Principal { get; set; }
        public DateTime CriadoEm { get; set; } = DateTime.UtcNow;
    }
}
