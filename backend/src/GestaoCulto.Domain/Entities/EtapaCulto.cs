using System;
using System.Collections.Generic;
using GestaoCulto.Domain.Common;

namespace GestaoCulto.Domain.Entities
{
    public class EtapaCulto : AuditableEntity
    {
        public long CultoId { get; set; }
        public Culto Culto { get; set; } = null!;
        public int Sequencia { get; set; }
        public DateTime HorarioInicio { get; set; }
        public int DuracaoMinutos { get; set; }
        public DateTime? HorarioFimCalculado { get; set; }
        public string Atividade { get; set; } = string.Empty;
        public string BlocoCronograma { get; set; } = "PRINCIPAL";
        public string? Descricao { get; set; }
        public long? ResponsavelPrincipalUsuarioId { get; set; }
        public Usuario? ResponsavelPrincipalUsuario { get; set; }
        public long? MinisterioResponsavelId { get; set; }
        public Ministerio? MinisterioResponsavel { get; set; }
        public string? Observacoes { get; set; }
        public int AtrasoMinutos { get; set; }
        public long StatusEtapaId { get; set; }
        public StatusEtapa StatusEtapa { get; set; } = null!;
        public ICollection<EtapaCultoMinisterioAcao> AcoesMinisterio { get; set; } = new List<EtapaCultoMinisterioAcao>();
    }
}
