using System;
using GestaoCulto.Domain.Common;

namespace GestaoCulto.Domain.Entities
{
    public class Escala : AuditableEntity
    {
        public long CultoId { get; set; }
        public Culto Culto { get; set; } = null!;
        public long? EtapaCultoId { get; set; }
        public EtapaCulto? EtapaCulto { get; set; }
        public long? VoluntarioId { get; set; }
        public Voluntario? Voluntario { get; set; }
        public string? VoluntarioAvulsoNome { get; set; }
        public string? VoluntarioAvulsoTelefone { get; set; }
        public long? MinisterioId { get; set; }
        public Ministerio? Ministerio { get; set; }
        public string Funcao { get; set; } = string.Empty;
        public DateTime? HorarioPrevisto { get; set; }
        public long PresencaStatusId { get; set; }
        public PresencaEscalaStatus PresencaStatus { get; set; } = null!;
        public DateTime? ConfirmadoEm { get; set; }
        public string? Observacoes { get; set; }
    }
}
