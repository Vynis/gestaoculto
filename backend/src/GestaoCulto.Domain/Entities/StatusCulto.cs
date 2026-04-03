using GestaoCulto.Domain.Common;

namespace GestaoCulto.Domain.Entities
{
    public class StatusCulto : AuditableEntity
    {
        public string Nome { get; set; } = string.Empty;
        public string Codigo { get; set; } = string.Empty;
        public string? CorHex { get; set; }
        public int Ordem { get; set; }
    }
}
