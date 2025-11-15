using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace SistemaFacturacionSRI.Application.Security
{
    /// <summary>
    /// Clase para generar, validar y manipular tokens JWT
    /// </summary>
    public class JwtTokenGenerator
    {
        private readonly IConfiguration _configuration;
        private readonly string _secretKey;
        private readonly string _issuer;
        private readonly string _audience;
        private readonly int _expirationMinutes;

        public JwtTokenGenerator(IConfiguration configuration)
        {
            _configuration = configuration;
            
            // Leer configuración de appsettings.json
            _secretKey = _configuration["JwtSettings:SecretKey"] 
                ?? throw new InvalidOperationException("JWT SecretKey no configurada");
            _issuer = _configuration["JwtSettings:Issuer"] 
                ?? throw new InvalidOperationException("JWT Issuer no configurado");
            _audience = _configuration["JwtSettings:Audience"] 
                ?? throw new InvalidOperationException("JWT Audience no configurado");
            _expirationMinutes = int.Parse(_configuration["JwtSettings:ExpirationMinutes"] ?? "480");

            // Validar longitud mínima de la clave (seguridad)
            if (_secretKey.Length < 32)
            {
                throw new InvalidOperationException("La SecretKey debe tener al menos 32 caracteres");
            }
        }

        /// <summary>
        /// Genera un token JWT para un usuario autenticado
        /// </summary>
        /// <param name="usuarioId">ID del usuario</param>
        /// <param name="username">Nombre de usuario</param>
        /// <param name="email">Email del usuario</param>
        /// <param name="rol">Rol del usuario (Administrador o Vendedor)</param>
        /// <returns>Token JWT como string</returns>
        public string GenerateToken(int usuarioId, string username, string email, string rol)
        {
            // 1. Crear los CLAIMS (datos que irán dentro del token)
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, usuarioId.ToString()),
                new Claim(ClaimTypes.Name, username),
                new Claim(ClaimTypes.Email, email),
                new Claim(ClaimTypes.Role, rol),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()), // ID único del token
                new Claim(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString()) // Fecha de emisión
            };

            // 2. Crear la CLAVE DE FIRMA (convierte tu SecretKey a bytes)
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secretKey));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            // 3. Crear el TOKEN con todos los datos
            var token = new JwtSecurityToken(
                issuer: _issuer,
                audience: _audience,
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(_expirationMinutes), // Expiración
                signingCredentials: credentials
            );

            // 4. Convertir el token a STRING para enviarlo
            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        /// <summary>
        /// Valida si un token JWT es válido (firma correcta y no expirado)
        /// </summary>
        /// <param name="token">Token JWT a validar</param>
        /// <returns>True si el token es válido</returns>
        public bool ValidateToken(string token)
        {
            if (string.IsNullOrEmpty(token))
                return false;

            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(_secretKey);

            try
            {
                // Parámetros de validación
                tokenHandler.ValidateToken(token, new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true, // Verificar firma
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = true, // Verificar emisor
                    ValidIssuer = _issuer,
                    ValidateAudience = true, // Verificar audiencia
                    ValidAudience = _audience,
                    ValidateLifetime = true, // Verificar expiración
                    ClockSkew = TimeSpan.Zero // Sin tolerancia de tiempo
                }, out SecurityToken validatedToken);

                return true;
            }
            catch
            {
                // Token inválido, expirado o con firma incorrecta
                return false;
            }
        }

        /// <summary>
        /// Extrae los claims (información) de un token JWT
        /// </summary>
        /// <param name="token">Token JWT</param>
        /// <returns>Lista de claims o null si el token es inválido</returns>
        public IEnumerable<Claim>? GetClaimsFromToken(string token)
        {
            if (string.IsNullOrEmpty(token))
                return null;

            var tokenHandler = new JwtSecurityTokenHandler();

            try
            {
                // Leer el token sin validarlo (solo para extraer datos)
                var jwtToken = tokenHandler.ReadJwtToken(token);
                return jwtToken.Claims;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Obtiene el ID del usuario desde un token JWT
        /// </summary>
        /// <param name="token">Token JWT</param>
        /// <returns>ID del usuario o null si el token es inválido</returns>
        public int? GetUsuarioIdFromToken(string token)
        {
            var claims = GetClaimsFromToken(token);
            if (claims == null)
                return null;

            var userIdClaim = claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier);
            if (userIdClaim != null && int.TryParse(userIdClaim.Value, out int userId))
            {
                return userId;
            }

            return null;
        }

        /// <summary>
        /// Obtiene el rol del usuario desde un token JWT
        /// </summary>
        /// <param name="token">Token JWT</param>
        /// <returns>Rol del usuario o null si el token es inválido</returns>
        public string? GetRolFromToken(string token)
        {
            var claims = GetClaimsFromToken(token);
            return claims?.FirstOrDefault(c => c.Type == ClaimTypes.Role)?.Value;
        }

        /// <summary>
        /// Obtiene la fecha de expiración de un token JWT
        /// </summary>
        /// <param name="token">Token JWT</param>
        /// <returns>Fecha de expiración o null si el token es inválido</returns>
        public DateTime? GetExpirationDate(string token)
        {
            if (string.IsNullOrEmpty(token))
                return null;

            var tokenHandler = new JwtSecurityTokenHandler();

            try
            {
                var jwtToken = tokenHandler.ReadJwtToken(token);
                return jwtToken.ValidTo;
            }
            catch
            {
                return null;
            }
        }
    }
}