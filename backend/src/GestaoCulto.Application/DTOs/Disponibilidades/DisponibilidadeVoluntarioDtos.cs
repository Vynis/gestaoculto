using System;
using System.Collections.Generic;

namespace GestaoCulto.Application.DTOs.Disponibilidades
{
    public class CultoDisponibilidadeDto
    {
        public long CultoId { get; set; }
        public string CultoNome { get; set; } = string.Empty;
        public DateTime DataCulto { get; set; }
        public TimeSpan HorarioInicio { get; set; }
        public string StatusCodigo { get; set; } = "NAO_RESPONDIDO";
        public string StatusNome { get; set; } = "Não respondido";
        public bool Escalado { get; set; }
    }

    public class MinisterioDisponibilidadeDto
    {
        public long MinisterioId { get; set; }
        public string MinisterioNome { get; set; } = string.Empty;
        public bool Principal { get; set; }
    }

    public class DisponibilidadeVoluntarioDetalheDto
    {
        public CultoDisponibilidadeDto Culto { get; set; } = new CultoDisponibilidadeDto();
        public IReadOnlyList<MinisterioDisponibilidadeDto> MinisteriosPermitidos { get; set; }
            = new List<MinisterioDisponibilidadeDto>();
        public IReadOnlyList<long> MinisterioIdsSelecionados { get; set; } = new List<long>();
        public string? Observacao { get; set; }
    }
}
