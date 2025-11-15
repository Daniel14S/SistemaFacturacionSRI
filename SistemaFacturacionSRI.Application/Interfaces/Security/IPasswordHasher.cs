using System;

namespace SistemaFacturacionSRI.Application.Interfaces.Security
{
    /// <summary>
    /// Contrato para operaciones de hashing de contraseñas basado en BCrypt.
    /// </summary>
    public interface IPasswordHasher
    {
        /// <summary>
        /// Genera un hash seguro para la contraseña proporcionada.
        /// </summary>
        /// <param name="password">Contraseña en texto plano.</param>
        /// <returns>Hash generado.</returns>
        /// <exception cref="ArgumentException">Si la contraseña es nula o vacía.</exception>
        string HashPassword(string password);

        /// <summary>
        /// Verifica si una contraseña en texto plano coincide con el hash almacenado.
        /// </summary>
        /// <param name="password">Contraseña en texto plano.</param>
        /// <param name="hashedPassword">Hash previamente almacenado.</param>
        /// <returns>True si coinciden; false en caso contrario.</returns>
        bool VerifyPassword(string password, string hashedPassword);
    }
}
