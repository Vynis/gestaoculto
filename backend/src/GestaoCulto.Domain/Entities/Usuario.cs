using System;
using System.Collections.Generic;
using GestaoCulto.Domain.Common;

namespace GestaoCulto.Domain.Entities
{
    public class Usuario : AuditableEntity
    {
        public string Nome { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string SenhaHash { get; set; } = string.Empty;
        public string? Telefone { get; set; }
        public bool Ativo { get; set; } = true;
        public bool DeveTrocarSenha { get; set; }
        public string? OrigemConta { get; set; }
        public DateTime? UltimoLoginEm { get; set; }
        public ICollection<UsuarioPerfil> Perfis { get; set; } = new List<UsuarioPerfil>();
        public UsuarioGoogle? UsuarioGoogle { get; set; }
    }
}
