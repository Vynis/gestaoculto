namespace GestaoCulto.Application.DTOs.Cadastros
{
    public class MinisterioDto
    {
        public long Id { get; set; }
        public string Nome { get; set; } = string.Empty;
        public string Codigo { get; set; } = string.Empty;
        public string? Descricao { get; set; }
        public bool Ativo { get; set; }
    }
}
