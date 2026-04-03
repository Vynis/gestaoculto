using System;
using GestaoCulto.Domain.Common;

namespace GestaoCulto.Domain.Entities
{
    public class UsuarioGoogle : AuditableEntity
    {
        public long UsuarioId { get; set; }
        public Usuario Usuario { get; set; } = null!;
        public string GoogleSub { get; set; } = string.Empty;
        public string EmailGoogle { get; set; } = string.Empty;
        public string? AvatarUrl { get; set; }
        public DateTime VinculadoEm { get; set; } = DateTime.UtcNow;
        public DateTime? UltimoLoginGoogleEm { get; set; }
    }
}
