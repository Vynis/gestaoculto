using System;
using GestaoCulto.Domain.Common;

namespace GestaoCulto.Domain.Entities
{
    public class CultoRecorrencia : AuditableEntity
    {
        public string Nome { get; set; } = string.Empty;
        public string TipoCulto { get; set; } = string.Empty;
        public int DiaSemana { get; set; }
        public TimeSpan HorarioInicio { get; set; }
        public TimeSpan? HorarioFimPrevisto { get; set; }
        public long StatusCultoId { get; set; }
        public StatusCulto StatusCulto { get; set; } = null!;
        public string? ObservacoesGerais { get; set; }
        public long? TemplateCultoId { get; set; }
        public TemplateCulto? TemplateCulto { get; set; }
        public int QuantidadeSemanasAntecedencia { get; set; } = 12;
        public bool Ativo { get; set; } = true;
        public DateTime? UltimaGeracaoEm { get; set; }
    }
}
