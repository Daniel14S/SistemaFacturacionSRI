namespace SistemaFacturacionSRI.WebUI.Services
{
    /// <summary>
    /// Almacena el token JWT de la sesión actual en memoria (por circuito) para que
    /// los HttpClient del lado del servidor puedan adjuntarlo sin depender de localStorage.
    /// </summary>
    public interface ITokenStorage
    {
        string? Token { get; set; }
        DateTime? TokenExpiresAt { get; set; }
        void Clear();
    }
}
