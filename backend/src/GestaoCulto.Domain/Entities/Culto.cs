using System;
using GestaoCulto.Domain.Common;

namespace GestaoCulto.Domain.Entities
{
    public class Culto : AuditableEntity
    {
        public long? TemplateCultoId { get; set; }
        public TemplateCulto? TemplateCulto { get; set; }
        public string Nome { get; set; } = string.Empty;
        public string TipoCulto { get; set; } = string.Empty;
        public DateTime DataCulto { get; set; }
        public TimeSpan HorarioInicio { get; set; }
        public TimeSpan? HorarioFimPrevisto { get; set; }
        public long StatusCultoId { get; set; }
        public StatusCulto StatusCulto { get; set; } = null!;
        public string? ObservacoesGerais { get; set; }
        public int TotalVisitantes { get; set; }
        public int TotalNovosConvertidos { get; set; }
    }
}
