using GestaoCulto.Domain.Common;

namespace GestaoCulto.Domain.Entities
{
    public class Auditoria : AuditableEntity
    {
        public string Tabela { get; set; } = string.Empty;
        public string EntidadeId { get; set; } = string.Empty;
        public string Acao { get; set; } = string.Empty;
        public string? DadosAnteriores { get; set; }
        public string? DadosNovos { get; set; }
        public long? UsuarioId { get; set; }
        public string? IpOrigem { get; set; }
    }
}
