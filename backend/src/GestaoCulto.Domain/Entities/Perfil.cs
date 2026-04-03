using GestaoCulto.Domain.Common;

namespace GestaoCulto.Domain.Entities
{
    public class Perfil : AuditableEntity
    {
        public string Nome { get; set; } = string.Empty;
        public string Codigo { get; set; } = string.Empty;
        public bool Ativo { get; set; } = true;
    }
}
