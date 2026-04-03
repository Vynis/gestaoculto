using System.Collections.Generic;
using System.Security.Claims;
using GestaoCulto.Domain.Entities;

namespace GestaoCulto.Application.Interfaces
{
    public interface ITokenService
    {
        string GerarToken(Usuario usuario, IList<string> perfis, bool manterConectado);
        IEnumerable<Claim> GerarClaims(Usuario usuario, IList<string> perfis);
    }
}
