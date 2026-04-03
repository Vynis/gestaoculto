using System;
using System.Collections.Generic;
using GestaoCulto.Domain.Common;

namespace GestaoCulto.Domain.Entities
{
    public class TemplateEtapaCulto : AuditableEntity
    {
        public long TemplateCultoId { get; set; }
        public TemplateCulto TemplateCulto { get; set; } = null!;
        public int Sequencia { get; set; }
        public TimeSpan? HorarioInicialPadrao { get; set; }
        public int DuracaoMinutos { get; set; }
        public string Atividade { get; set; } = string.Empty;
        public string? Descricao { get; set; }
        public long? MinisterioResponsavelId { get; set; }
        public Ministerio? MinisterioResponsavel { get; set; }
        public string? Observacoes { get; set; }
        public long? StatusEtapaId { get; set; }
        public ICollection<TemplateEtapaCultoMinisterioAcao> AcoesMinisterio { get; set; } = new List<TemplateEtapaCultoMinisterioAcao>();
    }
}
