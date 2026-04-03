using System.Threading.Tasks;
using GestaoCulto.Application.DTOs.Auth;
using GestaoCulto.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GestaoCulto.API.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        [AllowAnonymous]
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequestDto request)
        {
            var response = await _authService.LoginAsync(request);
            return Ok(response);
        }

        [AllowAnonymous]
        [HttpPost("google")]
        public async Task<IActionResult> LoginGoogle([FromBody] GoogleLoginRequestDto request)
        {
            var response = await _authService.LoginComGoogleAsync(request);
            return Ok(response);
        }
    }
}
