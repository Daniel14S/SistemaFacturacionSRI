using System;
using System.Security.Cryptography;
using System.Text;
using SistemaFacturacionSRI.Domain.Interfaces.Security;

namespace SistemaFacturacionSRI.Application.Security
{
    /// <summary>
    /// Implementación de <see cref="IPasswordHasher"/>.
    /// ATENCIÓN: Se ha modificado temporalmente para usar SHA-256 y coincidir con los datos de seed.
    /// La implementación original usaba BCrypt, que es más seguro.
    /// </summary>
    public class PasswordHasher : IPasswordHasher
    {
        /// <inheritdoc />
        public string HashPassword(string password)
        {
            if (string.IsNullOrWhiteSpace(password))
            {
                throw new ArgumentException("La contraseña no puede estar vacía", nameof(password));
            }

            // TEMPORARY: Using SHA-256 to match seed data.
            // TODO: Revert to BCrypt and fix seed data migration.
            using var sha256 = SHA256.Create();
            var passwordBytes = Encoding.UTF8.GetBytes(password);
            var hashBytes = sha256.ComputeHash(passwordBytes);
            return Convert.ToBase64String(hashBytes);
        }

        /// <inheritdoc />
        public bool VerifyPassword(string password, string hashedPassword)
        {
            if (string.IsNullOrEmpty(hashedPassword) || string.IsNullOrEmpty(password))
            {
                return false;
            }

            // TEMPORARY: Using SHA-256 to match seed data.
            // The seed data uses SHA-256/Base64, while the original code used BCrypt.
            // This temporary change allows login with the existing seeded users.
            // TODO: Revert to BCrypt and fix seed data migration.
            using var sha256 = SHA256.Create();
            var passwordBytes = Encoding.UTF8.GetBytes(password);
            var hashBytes = sha256.ComputeHash(passwordBytes);
            var hashBase64 = Convert.ToBase64String(hashBytes);

            return hashBase64 == hashedPassword;
        }
    }
}
