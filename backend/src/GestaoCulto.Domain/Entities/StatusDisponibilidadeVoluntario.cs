namespace GestaoCulto.Domain.Entities
{
    public class StatusDisponibilidadeVoluntario
    {
        public long Id { get; set; }
        public string Nome { get; set; } = string.Empty;
        public string Codigo { get; set; } = string.Empty;
        public string? CorHex { get; set; }
        public int Ordem { get; set; }
    }
}
