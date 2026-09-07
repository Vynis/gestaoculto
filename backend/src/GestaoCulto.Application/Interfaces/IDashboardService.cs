using System.Threading.Tasks;
using System;
using GestaoCulto.Application.DTOs.Dashboard;

namespace GestaoCulto.Application.Interfaces
{
    public interface IDashboardService
    {
        Task<DashboardResumoDto> ObterResumoAsync();
        Task<DashboardEstatisticasDto> ObterEstatisticasAsync(DateTime? inicio, DateTime? fim, int limite);
    }
}
