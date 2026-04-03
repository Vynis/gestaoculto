using System.Threading.Tasks;
using GestaoCulto.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GestaoCulto.API.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/dashboard")]
    public class DashboardController : ControllerBase
    {
        private readonly IDashboardService _dashboardService;

        public DashboardController(IDashboardService dashboardService)
        {
            _dashboardService = dashboardService;
        }

        [HttpGet("resumo")]
        public async Task<IActionResult> Resumo()
        {
            return Ok(await _dashboardService.ObterResumoAsync());
        }
    }
}
