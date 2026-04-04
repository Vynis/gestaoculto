using GestaoCulto.API.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GestaoCulto.API.Controllers
{
    [ApiController]
    [AllowAnonymous]
    [Route("api/version")]
    public class VersionController : ControllerBase
    {
        private readonly BuildVersionInfo _buildVersionInfo;

        public VersionController(BuildVersionInfo buildVersionInfo)
        {
            _buildVersionInfo = buildVersionInfo;
        }

        [HttpGet]
        public IActionResult Get()
        {
            return Ok(_buildVersionInfo);
        }
    }
}
