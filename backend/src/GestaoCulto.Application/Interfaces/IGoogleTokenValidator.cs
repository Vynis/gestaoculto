using System.Threading.Tasks;

namespace GestaoCulto.Application.Interfaces
{
    public interface IGoogleTokenValidator
    {
        Task<GoogleTokenPayload?> ValidarAsync(string idToken);
    }

    public class GoogleTokenPayload
    {
        public string Subject { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Picture { get; set; }
    }
}
