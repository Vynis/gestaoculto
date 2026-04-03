using System;
using System.Security.Cryptography;
using GestaoCulto.Application.Interfaces;

namespace GestaoCulto.Infrastructure.Security
{
    public class SimplePasswordHasher : IPasswordHasher
    {
        public string Hash(string senha)
        {
            var salt = new byte[16];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(salt);

            using var pbkdf2 = new Rfc2898DeriveBytes(senha, salt, 10000, HashAlgorithmName.SHA256);
            var hash = pbkdf2.GetBytes(32);

            return Convert.ToBase64String(salt) + "." + Convert.ToBase64String(hash);
        }

        public bool Verificar(string senha, string hash)
        {
            var parts = hash.Split('.');
            if (parts.Length != 2)
            {
                return false;
            }

            var salt = Convert.FromBase64String(parts[0]);
            var hashEsperado = Convert.FromBase64String(parts[1]);
            using var pbkdf2 = new Rfc2898DeriveBytes(senha, salt, 10000, HashAlgorithmName.SHA256);
            var hashCalculado = pbkdf2.GetBytes(32);
            return CryptographicOperations.FixedTimeEquals(hashCalculado, hashEsperado);
        }
    }
}
