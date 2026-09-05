using System.Threading.Tasks;
using GestaoCulto.Application.DTOs.Relatorios;

namespace GestaoCulto.Application.Interfaces
{
    public interface IRelatorioCompartilhamentoService
    {
        Task<RelatorioCompartilhadoLinkDto> GerarLinkAdministrativoAsync(long cultoId);
        Task<RelatorioCompartilhadoLinkDto> GerarLinkVoluntarioAsync(long cultoId, long voluntarioId);
        Task<long?> ObterCultoIdAsync(string token);
    }
}
