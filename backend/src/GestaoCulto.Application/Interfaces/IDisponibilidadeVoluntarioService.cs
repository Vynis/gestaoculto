using System.Collections.Generic;
using System.Threading.Tasks;
using GestaoCulto.Application.DTOs.Disponibilidades;

namespace GestaoCulto.Application.Interfaces
{
    public interface IDisponibilidadeVoluntarioService
    {
        Task<IReadOnlyList<CultoDisponibilidadeDto>> ListarCultosFuturosAsync(long voluntarioId, int limite);
        Task<DisponibilidadeVoluntarioDetalheDto> ObterDetalheAsync(long voluntarioId, long cultoId);
        Task SalvarAsync(
            long voluntarioId,
            long cultoId,
            bool disponivel,
            IReadOnlyCollection<long> ministerioIds,
            string? observacao);
    }
}
