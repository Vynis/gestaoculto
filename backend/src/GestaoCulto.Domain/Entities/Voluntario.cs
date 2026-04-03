using GestaoCulto.Domain.Common;

namespace GestaoCulto.Domain.Entities
{
    public class Voluntario : AuditableEntity
    {
        public long? UsuarioId { get; set; }
        public Usuario? Usuario { get; set; }
        public string Nome { get; set; } = string.Empty;
        public string? Telefone { get; set; }
        public string? Email { get; set; }
        public long? MinisterioPrincipalId { get; set; }
        public Ministerio? MinisterioPrincipal { get; set; }
        public string? Observacoes { get; set; }
        public string? RestricoesIndisponibilidade { get; set; }
        public bool Ativo { get; set; } = true;
    }
}
