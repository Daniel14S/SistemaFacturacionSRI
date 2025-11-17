using System.Threading;
using System.Threading.Tasks;

namespace SistemaFacturacionSRI.WebUI.Services
{
    public interface IAutoLoginService
    {
        Task EnsureAdminTokenAsync(CancellationToken cancellationToken = default);
        Task ForceRefreshTokenAsync(CancellationToken cancellationToken = default);
    }
}
