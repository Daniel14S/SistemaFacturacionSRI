namespace SistemaFacturacionSRI.Domain.Enums
{
    /// <summary>
    /// Roles disponibles en el sistema de facturación.
    /// Define los niveles de acceso y permisos de los usuarios.
    /// </summary>
    public enum Rol
    {
        /// <summary>
        /// Administrador del sistema.
        /// Tiene acceso completo a todas las funcionalidades:
        /// - Gestión de usuarios (CRUD completo)
        /// - Gestión de clientes (CRUD completo)
        /// - Gestión de productos (CRUD completo)
        /// - Gestión de lotes (CRUD completo)
        /// - Generación de facturas
        /// - Acceso a reportes y estadísticas
        /// - Configuración del sistema
        /// </summary>
        Administrador = 1,

        /// <summary>
        /// Vendedor del sistema.
        /// Tiene acceso limitado a funcionalidades operativas:
        /// - Gestión de clientes (CRUD completo)
        /// - Visualización de productos (solo lectura)
        /// - Visualización de lotes (solo lectura)
        /// - Generación de facturas
        /// - NO puede gestionar usuarios
        /// - NO puede modificar productos ni lotes
        /// </summary>
        Vendedor = 2
    }
}