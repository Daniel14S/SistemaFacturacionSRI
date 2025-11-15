using System;
using BCrypt.Net;
using SistemaFacturacionSRI.Application.Interfaces.Security;

namespace SistemaFacturacionSRI.Application.Security
{
    /// <summary>
    /// Implementación de <see cref="IPasswordHasher"/> usando BCrypt.
    /// </summary>
    public class PasswordHasher : IPasswordHasher
    {
        /// <summary>
        /// Factor de costo recomendado para producción.
        /// </summary>
        private const int WorkFactor = 12;

        /// <inheritdoc />
        public string HashPassword(string password)
        {
            if (string.IsNullOrWhiteSpace(password))
            {
                throw new ArgumentException("La contraseña no puede estar vacía", nameof(password));
            }

            return BCrypt.Net.BCrypt.HashPassword(password, WorkFactor);
        }

        /// <inheritdoc />
        public bool VerifyPassword(string password, string hashedPassword)
        {
            if (string.IsNullOrEmpty(hashedPassword))
            {
                return false;
            }

            if (string.IsNullOrEmpty(password))
            {
                return false;
            }

            return BCrypt.Net.BCrypt.Verify(password, hashedPassword);
        }
    }
}
