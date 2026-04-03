using System;
using System.Threading.Tasks;
using GestaoCulto.Application.Interfaces;
using Google.Apis.Auth;
using Microsoft.Extensions.Configuration;

namespace GestaoCulto.Infrastructure.ExternalAuth
{
    public class GoogleTokenValidator : IGoogleTokenValidator
    {
        private readonly IConfiguration _configuration;

        public GoogleTokenValidator(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task<GoogleTokenPayload?> ValidarAsync(string idToken)
        {
            try
            {
                var clientId = _configuration["GoogleAuth:ClientId"];
                var payload = await GoogleJsonWebSignature.ValidateAsync(idToken, new GoogleJsonWebSignature.ValidationSettings
                {
                    Audience = new[] { clientId }
                });

                return new GoogleTokenPayload
                {
                    Subject = payload.Subject,
                    Email = payload.Email,
                    Name = payload.Name,
                    Picture = payload.Picture
                };
            }
            catch
            {
                return null;
            }
        }
    }
}
