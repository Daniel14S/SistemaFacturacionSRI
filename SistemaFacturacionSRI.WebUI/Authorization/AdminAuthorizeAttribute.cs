using Microsoft.AspNetCore.Authorization;

namespace SistemaFacturacionSRI.WebUI.Authorization
{
    /// <summary>
    /// Atributo de autorización reutilizable para endpoints solo administradores.
    /// Preconfigura el rol y la política asociada.
    /// </summary>
    public class AdminAuthorizeAttribute : AuthorizeAttribute
    {
        public AdminAuthorizeAttribute()
        {
            Roles = "Administrador";
            Policy = AuthorizationPolicies.AdminOnly;
        }
    }
}