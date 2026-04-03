using System.Collections.Generic;

namespace GestaoCulto.Application.DTOs.Cadastros
{
    public class UsuarioDto
    {
        public long Id { get; set; }
        public string Nome { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? Telefone { get; set; }
        public bool Ativo { get; set; }
        public IList<string> Perfis { get; set; } = new List<string>();
        public IList<long> PerfilIds { get; set; } = new List<long>();
    }

    public class UsuarioRequestDto
    {
        public string Nome { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? Telefone { get; set; }
        public bool Ativo { get; set; } = true;
        public string? Senha { get; set; }
        public IList<long> PerfilIds { get; set; } = new List<long>();
    }

    public class PerfilOpcaoDto
    {
        public long Id { get; set; }
        public string Nome { get; set; } = string.Empty;
        public string Codigo { get; set; } = string.Empty;
    }
}
