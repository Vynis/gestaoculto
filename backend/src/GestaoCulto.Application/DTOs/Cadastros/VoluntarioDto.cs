using System;
using System.Collections.Generic;

namespace GestaoCulto.Application.DTOs.Cadastros
{
    public class VoluntarioDto
    {
        public long Id { get; set; }
        public long? UsuarioId { get; set; }
        public string? UsuarioNome { get; set; }
        public string Nome { get; set; } = string.Empty;
        public DateTime? DataNascimento { get; set; }
        public string? Telefone { get; set; }
        public string? Email { get; set; }
        public long? MinisterioPrincipalId { get; set; }
        public string? Observacoes { get; set; }
        public string? RestricoesIndisponibilidade { get; set; }
        public bool Ativo { get; set; }
        public List<long> MinisterioIds { get; set; } = new List<long>();
    }

    public class VoluntarioUsuarioOpcaoDto
    {
        public long Id { get; set; }
        public string Nome { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public long? VoluntarioId { get; set; }
    }
}
