using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace SistemaFacturacionSRI.WebUI.Services
{
    public class AuthHeaderHandler : DelegatingHandler
    {
        private readonly ITokenStorage _tokenStorage;
        private readonly IAutoLoginService _autoLoginService;

        public AuthHeaderHandler(
            ITokenStorage tokenStorage,
            IAutoLoginService autoLoginService)
        {
            _tokenStorage = tokenStorage;
            _autoLoginService = autoLoginService;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            await _autoLoginService.EnsureAdminTokenAsync(cancellationToken);
            AttachToken(request);

            var response = await base.SendAsync(request, cancellationToken);

            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                _tokenStorage.Clear();
            }

            return response;
        }

        private void AttachToken(HttpRequestMessage request)
        {
            var token = _tokenStorage.Token;
            if (!string.IsNullOrEmpty(token))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }
        }
    }
}
