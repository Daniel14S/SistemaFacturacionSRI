namespace SistemaFacturacionSRI.Domain.Enums
{
    /// <summary>
    /// Métodos de extensión para el enum Rol.
    /// Facilita la conversión y obtención de información del rol.
    /// </summary>
    public static class RolExtensions
    {
        /// <summary>
        /// Obtiene el nombre descriptivo del rol
        /// </summary>
        public static string ObtenerNombre(this Rol rol)
        {
            return rol switch
            {
                Rol.Administrador => "Administrador",
                Rol.Vendedor => "Vendedor",
                _ => "Desconocido"
            };
        }

        /// <summary>
        /// Obtiene la descripción detallada del rol
        /// </summary>
        public static string ObtenerDescripcion(this Rol rol)
        {
            return rol switch
            {
                Rol.Administrador => "Acceso completo a todas las funcionalidades del sistema",
                Rol.Vendedor => "Acceso limitado a operaciones de ventas y consulta de inventario",
                _ => "Rol no definido"
            };
        }

        /// <summary>
        /// Verifica si el rol tiene permisos de administrador
        /// </summary>
        public static bool EsAdministrador(this Rol rol)
        {
            return rol == Rol.Administrador;
        }

        /// <summary>
        /// Verifica si el rol tiene permisos de vendedor
        /// </summary>
        public static bool EsVendedor(this Rol rol)
        {
            return rol == Rol.Vendedor;
        }

        /// <summary>
        /// Verifica si el rol puede gestionar usuarios
        /// </summary>
        public static bool PuedeGestionarUsuarios(this Rol rol)
        {
            return rol == Rol.Administrador;
        }

        /// <summary>
        /// Verifica si el rol puede modificar productos
        /// </summary>
        public static bool PuedeModificarProductos(this Rol rol)
        {
            return rol == Rol.Administrador;
        }

        /// <summary>
        /// Verifica si el rol puede ver productos
        /// </summary>
        public static bool PuedeVerProductos(this Rol rol)
        {
            return rol == Rol.Administrador || rol == Rol.Vendedor;
        }

        /// <summary>
        /// Convierte un string a enum Rol de forma segura
        /// </summary>
        public static Rol? ConvertirDesdeString(string rolString)
        {
            if (string.IsNullOrWhiteSpace(rolString))
                return null;

            return rolString.Trim().ToLower() switch
            {
                "administrador" or "admin" => Rol.Administrador,
                "vendedor" => Rol.Vendedor,
                _ => null
            };
        }

        /// <summary>
        /// Obtiene todos los roles disponibles
        /// </summary>
        public static List<(Rol Rol, string Nombre, string Descripcion)> ObtenerTodosLosRoles()
        {
            return new List<(Rol, string, string)>
            {
                (Rol.Administrador, "Administrador", "Acceso completo al sistema"),
                (Rol.Vendedor, "Vendedor", "Acceso limitado a ventas")
            };
        }
    }
}