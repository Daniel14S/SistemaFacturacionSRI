using System.Net;
using System.Net.Http.Json;
using SistemaFacturacionSRI.Domain.DTOs.Auth;

namespace SistemaFacturacionSRI.WebUI.Services
{
    /// <summary>
    /// Cliente HTTP para consumir los endpoints de autenticación.
    /// </summary>
    public class AuthHttpService : IAuthHttpService
    {
        private const string LoginEndpoint = "/api/auth/login";
        private const string LogoutEndpoint = "/api/auth/logout";
        private readonly HttpClient _httpClient;

        public AuthHttpService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<LoginResponseDto> LoginAsync(string username, string password, CancellationToken cancellationToken = default)
        {
            var request = new LoginRequestDto
            {
                Username = username,
                Password = password
            };

            var response = await _httpClient.PostAsJsonAsync(LoginEndpoint, request, cancellationToken);
            var loginResponse = await response.Content.ReadFromJsonAsync<LoginResponseDto>(cancellationToken: cancellationToken);

            if (response.IsSuccessStatusCode && loginResponse is not null)
            {
                return loginResponse;
            }

            if (loginResponse is not null)
            {
                throw new InvalidOperationException(string.IsNullOrWhiteSpace(loginResponse.Message)
                    ? "No se pudo iniciar sesión con las credenciales proporcionadas."
                    : loginResponse.Message);
            }

            var rawError = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new InvalidOperationException(string.IsNullOrWhiteSpace(rawError)
                ? "No se pudo iniciar sesión con las credenciales proporcionadas."
                : rawError);
        }

        public async Task LogoutAsync(CancellationToken cancellationToken = default)
        {
            var response = await _httpClient.PostAsync(LogoutEndpoint, content: null, cancellationToken);

            if (!response.IsSuccessStatusCode && response.StatusCode != HttpStatusCode.Unauthorized)
            {
                var rawError = await response.Content.ReadAsStringAsync(cancellationToken);
                throw new InvalidOperationException(string.IsNullOrWhiteSpace(rawError)
                    ? "No se pudo cerrar la sesión correctamente."
                    : rawError);
            }
        }
    }
}
