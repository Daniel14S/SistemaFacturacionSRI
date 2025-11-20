using Microsoft.JSInterop;
using System.Net.Http.Headers;
using System.Text.Json;

namespace SistemaFacturacionSRI.WebUI.Services

{
    public class WebAuthService

    {
        private readonly HttpClient _httpClient;
        private readonly IJSRuntime _jsRuntime;

        private const string TOKEN_KEY = "authToken";
        private const string USER_KEY = "currentUser";

        private readonly JsonSerializerOptions _jsonOptions =
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

        public WebAuthService(HttpClient httpClient, IJSRuntime jsRuntime)
        {
            _httpClient = httpClient;
            _jsRuntime = jsRuntime;
        }

        // ==========================================================
        // LOGIN
        // ==========================================================
        public async Task<LoginResult> Login(string username, string password)
        {
            try
            {
                var loginData = new { Username = username, Password = password };

                var response = await _httpClient.PostAsJsonAsync("api/auth/login", loginData);

                if (!response.IsSuccessStatusCode)
                {
                    return new LoginResult
                    {
                        Success = false,
                        Message = "Credenciales inválidas."
                    };
                }

                // Deserializar respuesta
                var result = await response.Content.ReadFromJsonAsync<LoginResponse>(_jsonOptions);

                if (result == null || string.IsNullOrEmpty(result.Token))
                {
                    return new LoginResult
                    {
                        Success = false,
                        Message = "Respuesta inválida del servidor."
                    };
                }

                // Guardar token
                await _jsRuntime.InvokeVoidAsync("localStorage.setItem", TOKEN_KEY, result.Token);

                // Guardar usuario
                var userJson = JsonSerializer.Serialize(result.User, _jsonOptions);
                await _jsRuntime.InvokeVoidAsync("localStorage.setItem", USER_KEY, userJson);

                // Configurar Header
                _httpClient.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", result.Token);

                return new LoginResult { Success = true, Message = "Login exitoso." };
            }
            catch (Exception ex)
            {
                return new LoginResult
                {
                    Success = false,
                    Message = $"Error inesperado: {ex.Message}"
                };
            }
        }

        // ==========================================================
        // LOGOUT
        // ==========================================================
        public async Task Logout()
        {
            await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", TOKEN_KEY);
            await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", USER_KEY);

            _httpClient.DefaultRequestHeaders.Authorization = null;
        }

        // ==========================================================
        // GET TOKEN
        // ==========================================================
        public async Task<string?> GetToken()
        {
            try
            {
                return await _jsRuntime.InvokeAsync<string>("localStorage.getItem", TOKEN_KEY);
            }
            catch
            {
                return null;
            }
        }

        // ==========================================================
        // IS AUTHENTICATED
        // ==========================================================
        public async Task<bool> IsAuthenticated()
        {
            var token = await GetToken();
            return !string.IsNullOrEmpty(token);
        }

        // ==========================================================
        // GET CURRENT USER
        // ==========================================================
        public async Task<User?> GetCurrentUser()
        {
            try
            {
                var json = await _jsRuntime.InvokeAsync<string>("localStorage.getItem", USER_KEY);

                if (string.IsNullOrWhiteSpace(json))
                    return null;

                return JsonSerializer.Deserialize<User>(json, _jsonOptions);
            }
            catch
            {
                return null;
            }
        }

        // ==========================================================
        // GET USER ROLE
        // ==========================================================
        public async Task<string?> GetUserRole()
        {
            var user = await GetCurrentUser();
            return user?.Role;
        }

        // ==========================================================
        // INITIALIZE AUTH (CARGA INICIAL)
        // ==========================================================
        public async Task InitializeAuth()
        {
            var token = await GetToken();

            if (!string.IsNullOrEmpty(token))
            {
                _httpClient.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", token);
            }
        }
    }

    // ==========================================================
    // MODELOS
    // ==========================================================
    public class LoginResponse
    {
        public string Token { get; set; } = string.Empty;
        public User User { get; set; } = new User();
    }

    public class User
    {
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
    }

    public class LoginResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}
