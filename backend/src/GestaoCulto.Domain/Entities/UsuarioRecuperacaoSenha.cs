using System;
using GestaoCulto.Domain.Common;

namespace GestaoCulto.Domain.Entities
{
    public class UsuarioRecuperacaoSenha : AuditableEntity
    {
        public long UsuarioId { get; set; }
        public Usuario Usuario { get; set; } = null!;
        public string TokenHash { get; set; } = string.Empty;
        public string Contexto { get; set; } = string.Empty;
        public DateTime ExpiraEm { get; set; }
        public DateTime? UsadoEm { get; set; }
    }
}
