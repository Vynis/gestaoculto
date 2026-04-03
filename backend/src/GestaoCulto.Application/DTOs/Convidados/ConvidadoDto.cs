namespace GestaoCulto.Application.DTOs.Convidados
{
    public class ConvidadoDto
    {
        public long Id { get; set; }
        public long CultoId { get; set; }
        public string Nome { get; set; } = string.Empty;
        public string? Telefone { get; set; }
        public string? QuemConvidou { get; set; }
        public bool PrimeiraVezIgreja { get; set; }
        public string? Observacoes { get; set; }
        public string StatusAcompanhamento { get; set; } = "PENDENTE";
    }
}
