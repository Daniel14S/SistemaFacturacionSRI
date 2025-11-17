namespace SistemaFacturacionSRI.WebUI.Services
{
    /// <summary>
    /// Implementación en memoria (scoped) para almacenar el token del usuario autenticado.
    /// </summary>
    public class TokenStorage : ITokenStorage
    {
        public string? Token { get; set; }
        public DateTime? TokenExpiresAt { get; set; }

        public void Clear()
        {
            Token = null;
            TokenExpiresAt = null;
        }
    }
}
