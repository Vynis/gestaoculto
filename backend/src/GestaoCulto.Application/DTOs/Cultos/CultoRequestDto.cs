namespace GestaoCulto.Application.DTOs.Cultos
{
    public class CultoRequestDto
    {
        public string Nome { get; set; } = string.Empty;
        public string TipoCulto { get; set; } = string.Empty;
        public string DataCulto { get; set; } = string.Empty;
        public string HorarioInicio { get; set; } = string.Empty;
        public string? HorarioFimPrevisto { get; set; }
        public long StatusCultoId { get; set; }
        public string? ObservacoesGerais { get; set; }
        public long? TemplateCultoId { get; set; }
    }
}
