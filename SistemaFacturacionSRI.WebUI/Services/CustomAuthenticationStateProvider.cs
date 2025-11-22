using Microsoft.AspNetCore.Components.Authorization;
using System.Security.Claims;
using SistemaFacturacionSRI.Application.Security;

namespace SistemaFacturacionSRI.WebUI.Services
{
    /// <summary>
    /// Proveedor de estado de autenticación personalizado.
    /// Gestiona el estado de autenticación del usuario basado en el token JWT.
    /// </summary>
    public class CustomAuthenticationStateProvider : AuthenticationStateProvider
    {
        private readonly ITokenStorage _tokenStorage;
        private readonly JwtTokenGenerator _jwtTokenGenerator;

        public CustomAuthenticationStateProvider(
            ITokenStorage tokenStorage,
            JwtTokenGenerator jwtTokenGenerator)
        {
            _tokenStorage = tokenStorage;
            _jwtTokenGenerator = jwtTokenGenerator;
        }

        /// <summary>
        /// Obtiene el estado de autenticación actual del usuario.
        /// </summary>
        public override Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            var token = _tokenStorage.Token;

            if (string.IsNullOrEmpty(token))
            {
                // Usuario no autenticado
                return Task.FromResult(new AuthenticationState(
                    new ClaimsPrincipal(new ClaimsIdentity())
                ));
            }

            // Validar el token
            var isValid = _jwtTokenGenerator.ValidateToken(token);
            if (!isValid)
            {
                // Token inválido, limpiar y retornar no autenticado
                _tokenStorage.Clear();
                return Task.FromResult(new AuthenticationState(
                    new ClaimsPrincipal(new ClaimsIdentity())
                ));
            }

            // Token válido, extraer claims
            var claims = _jwtTokenGenerator.GetClaimsFromToken(token);
            if (claims == null || !claims.Any())
            {
                return Task.FromResult(new AuthenticationState(
                    new ClaimsPrincipal(new ClaimsIdentity())
                ));
            }

            // Crear identidad autenticada
            var identity = new ClaimsIdentity(claims, "jwt");
            var user = new ClaimsPrincipal(identity);

            return Task.FromResult(new AuthenticationState(user));
        }

        /// <summary>
        /// Marca al usuario como autenticado y notifica a los componentes.
        /// </summary>
        public void MarkUserAsAuthenticated(string token)
        {
            _tokenStorage.Token = token;

            var claims = _jwtTokenGenerator.GetClaimsFromToken(token);
            var expiresAt = _jwtTokenGenerator.GetExpirationDate(token);
            _tokenStorage.TokenExpiresAt = expiresAt;

            var identity = new ClaimsIdentity(claims, "jwt");
            var user = new ClaimsPrincipal(identity);

            NotifyAuthenticationStateChanged(
                Task.FromResult(new AuthenticationState(user))
            );
        }

        /// <summary>
        /// Marca al usuario como no autenticado y notifica a los componentes.
        /// </summary>
        public void MarkUserAsLoggedOut()
        {
            _tokenStorage.Clear();

            var anonymous = new ClaimsPrincipal(new ClaimsIdentity());
            NotifyAuthenticationStateChanged(
                Task.FromResult(new AuthenticationState(anonymous))
            );
        }
    }
}