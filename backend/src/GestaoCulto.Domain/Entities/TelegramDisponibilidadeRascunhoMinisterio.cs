namespace GestaoCulto.Domain.Entities
{
    public class TelegramDisponibilidadeRascunhoMinisterio
    {
        public long TelegramDisponibilidadeRascunhoId { get; set; }
        public TelegramDisponibilidadeRascunho TelegramDisponibilidadeRascunho { get; set; } = null!;
        public long MinisterioId { get; set; }
        public Ministerio Ministerio { get; set; } = null!;
    }
}
