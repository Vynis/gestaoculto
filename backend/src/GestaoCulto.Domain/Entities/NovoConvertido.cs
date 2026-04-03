using System;
using GestaoCulto.Domain.Common;

namespace GestaoCulto.Domain.Entities
{
    public class NovoConvertido : AuditableEntity
    {
        public long ConvidadoId { get; set; }
        public Convidado Convidado { get; set; } = null!;
        public DateTime? DataDecisao { get; set; }
        public string StatusAcompanhamento { get; set; } = "PENDENTE";
        public string? Observacoes { get; set; }
    }
}
