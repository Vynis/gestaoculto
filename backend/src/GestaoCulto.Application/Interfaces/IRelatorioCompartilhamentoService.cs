using System.Threading.Tasks;
using GestaoCulto.Application.DTOs.Relatorios;

namespace GestaoCulto.Application.Interfaces
{
    public interface IRelatorioCompartilhamentoService
    {
        Task<RelatorioCompartilhadoLinkDto> GerarLinkAsync(long cultoId);
        Task<RelatorioCompartilhadoLinkDto> GerarLinkAsync(long cultoId, long voluntarioId);
        Task<long?> ObterCultoIdAsync(string token);
    }
}
