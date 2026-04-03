using System.Collections.Generic;
using System.Threading.Tasks;
using GestaoCulto.Application.DTOs.Cultos;

namespace GestaoCulto.Application.Interfaces
{
    public interface ICultoService
    {
        Task<IReadOnlyList<CultoResponseDto>> ListarAsync();
        Task<CultoResponseDto?> ObterPorIdAsync(long id);
        Task<CultoResponseDto> CriarAsync(CultoRequestDto dto);
        Task<CultoResponseDto?> AtualizarAsync(long id, CultoRequestDto dto);
        Task<bool> ExcluirAsync(long id);
    }
}
