using System.Threading.Tasks;
using GestaoCulto.Application.DTOs.Dashboard;

namespace GestaoCulto.Application.Interfaces
{
    public interface IDashboardService
    {
        Task<DashboardResumoDto> ObterResumoAsync();
    }
}
