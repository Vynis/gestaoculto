using System;
using System.Collections.Generic;

namespace GestaoCulto.Application.DTOs.Auth
{
    public class AuthResponseDto
    {
        public string Token { get; set; } = string.Empty;
        public DateTime ExpiraEm { get; set; }
        public string Nome { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public IList<string> Perfis { get; set; } = new List<string>();
    }
}
