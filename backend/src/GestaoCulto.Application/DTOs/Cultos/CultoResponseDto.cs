using System;

namespace GestaoCulto.Application.DTOs.Cultos
{
    public class CultoResponseDto
    {
        public long Id { get; set; }
        public string Nome { get; set; } = string.Empty;
        public string TipoCulto { get; set; } = string.Empty;
        public DateTime DataCulto { get; set; }
        public TimeSpan HorarioInicio { get; set; }
        public TimeSpan? HorarioFimPrevisto { get; set; }
        public long StatusCultoId { get; set; }
        public string? ObservacoesGerais { get; set; }
        public int TotalVisitantes { get; set; }
        public int TotalNovosConvertidos { get; set; }
    }
}
