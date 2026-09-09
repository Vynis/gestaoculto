using System;
using System.Collections.Generic;

namespace GestaoCulto.Application.DTOs.Dashboard
{
    public class DashboardResumoDto
    {
        public long? ProximoCultoId { get; set; }
        public string? ProximoCultoNome { get; set; }
        public int VoluntariosEscalados { get; set; }
        public int TarefasPendentes { get; set; }
        public int ConvidadosRegistrados { get; set; }
        public int NovosConvertidos { get; set; }
        public int AlertasOperacionais { get; set; }
        public List<DashboardAniversarianteDto> Aniversariantes { get; set; } = new List<DashboardAniversarianteDto>();
    }

    public class DashboardAniversarianteDto
    {
        public long VoluntarioId { get; set; }
        public string Nome { get; set; } = string.Empty;
        public DateTime DataNascimento { get; set; }
    }
}
