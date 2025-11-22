namespace SistemaFacturacionSRI.WebUI.Services
{
    /// <summary>
    /// Almacenamiento en memoria del token JWT (Scoped por circuito).
    /// </summary>
    public class TokenStorage : ITokenStorage
    {
        // ✅ Variables en memoria COMPARTIDAS por TODO el circuito
        private string? _token;
        private DateTime? _tokenExpiresAt;

        public string? Token 
        { 
            get => _token;
            set => _token = value;
        }

        public DateTime? TokenExpiresAt 
        { 
            get => _tokenExpiresAt;
            set => _tokenExpiresAt = value;
        }

        public void Clear()
        {
            _token = null;
            _tokenExpiresAt = null;
        }
    }
}