using System.Threading.Tasks;
using GestaoCulto.Application.DTOs.Auth;

namespace GestaoCulto.Application.Interfaces
{
    public interface IAuthService
    {
        Task<AuthResponseDto> LoginAsync(LoginRequestDto request);
        Task<AuthResponseDto> LoginComGoogleAsync(GoogleLoginRequestDto request);
    }
}
