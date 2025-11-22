namespace SistemaFacturacionSRI.WebUI.Services
{
    public interface ITokenStorage
    {
        string? Token { get; set; }
        DateTime? TokenExpiresAt { get; set; }
        void Clear();
    }
}