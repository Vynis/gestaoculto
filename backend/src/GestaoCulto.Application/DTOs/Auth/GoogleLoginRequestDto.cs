namespace GestaoCulto.Application.DTOs.Auth
{
    public class GoogleLoginRequestDto
    {
        public string IdToken { get; set; } = string.Empty;
        public bool ManterConectado { get; set; }
    }
}
