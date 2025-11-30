using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SistemaFacturacionSRI.Domain.DTOs.Auth;
using SistemaFacturacionSRI.Domain.Interfaces;

namespace SistemaFacturacionSRI.WebUI.Services
{
    public class AutoLoginService : IAutoLoginService
    {
        private readonly IAuthService _authService;
        private readonly ITokenStorage _tokenStorage;
        private readonly ILogger<AutoLoginService> _logger;
        private readonly SemaphoreSlim _tokenLock = new(1, 1);

        public AutoLoginService(
            IAuthService authService,
            ITokenStorage tokenStorage,
            ILogger<AutoLoginService> logger)
        {
            _authService = authService;
            _tokenStorage = tokenStorage;
            _logger = logger;
        }

        public async Task EnsureAdminTokenAsync(CancellationToken cancellationToken = default)
        {
            if (HasValidToken())
            {
                return;
            }

            await _tokenLock.WaitAsync(cancellationToken);
            try
            {
                if (HasValidToken())
                {
                    return;
                }

                _logger.LogInformation("Generando token automático para el usuario administrador");

                var response = await _authService.LoginAsync(new LoginRequestDto
                {
                    Username = "admin",
                    Password = "Admin123*"
                });

                if (!response.Success || string.IsNullOrWhiteSpace(response.Token))
                {
                    throw new InvalidOperationException(response.Message ?? "No se pudo iniciar sesión automáticamente");
                }

                _tokenStorage.Token = response.Token;
                _tokenStorage.TokenExpiresAt = response.ExpiresAt ?? DateTime.UtcNow.AddMinutes(30);

                _logger.LogInformation("Token automático generado correctamente para administrador");
            }
            finally
            {
                _tokenLock.Release();
            }
        }

        public async Task ForceRefreshTokenAsync(CancellationToken cancellationToken = default)
        {
            _tokenStorage.Clear();
            await EnsureAdminTokenAsync(cancellationToken);
        }

        private bool HasValidToken()
        {
            return !string.IsNullOrWhiteSpace(_tokenStorage.Token)
                && _tokenStorage.TokenExpiresAt.HasValue
                && _tokenStorage.TokenExpiresAt.Value > DateTime.UtcNow.AddMinutes(-1);
        }
    }
}
