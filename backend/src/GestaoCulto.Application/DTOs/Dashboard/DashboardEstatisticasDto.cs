using System;
using System.Collections.Generic;

namespace GestaoCulto.Application.DTOs.Dashboard
{
    public class DashboardEstatisticasDto
    {
        public DateTime PeriodoInicio { get; set; }
        public DateTime PeriodoFim { get; set; }
        public List<DashboardVoluntarioRankingDto> Voluntarios { get; set; } = new List<DashboardVoluntarioRankingDto>();
        public List<DashboardMusicaRankingDto> Musicas { get; set; } = new List<DashboardMusicaRankingDto>();
    }

    public class DashboardVoluntarioRankingDto
    {
        public long VoluntarioId { get; set; }
        public string Nome { get; set; } = string.Empty;
        public int QuantidadeCultos { get; set; }
        public bool Ativo { get; set; }
    }

    public class DashboardMusicaRankingDto
    {
        public long MusicaId { get; set; }
        public string Titulo { get; set; } = string.Empty;
        public string ArtistaBanda { get; set; } = string.Empty;
        public int QuantidadeCultos { get; set; }
        public bool Ativo { get; set; }
    }
}
