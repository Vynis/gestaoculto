namespace GestaoCulto.Domain.Entities
{
    public class DisponibilidadeCultoVoluntarioMinisterio
    {
        public long DisponibilidadeCultoVoluntarioId { get; set; }
        public DisponibilidadeCultoVoluntario DisponibilidadeCultoVoluntario { get; set; } = null!;
        public long MinisterioId { get; set; }
        public Ministerio Ministerio { get; set; } = null!;
    }
}
