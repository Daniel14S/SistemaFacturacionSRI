namespace SistemaFacturacionSRI.WebUI.Authorization
{
    /// <summary>
    /// Constantes de políticas de autorización utilizadas en la aplicación.
    /// </summary>
    public static class AuthorizationPolicies
    {
        public const string AdminOnly = "AdminOnly";
        public const string VendedorOnly = "VendedorOnly";
        public const string AdminOrVendedor = "AdminOrVendedor";
    }
}