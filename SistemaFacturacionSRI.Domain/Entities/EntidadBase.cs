namespace SistemaFacturacionSRI.Domain.Entities
{
    /// <summary>
    /// Clase base para todas las entidades del sistema.
    /// Proporciona propiedades comunes que todas las tablas necesitan.
    /// </summary>
    public abstract class EntidadBase
    {
        /// <summary>
        /// Identificador único de cada registro.
        /// Es la clave primaria (Primary Key) en la base de datos.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Fecha y hora en que se creó el registro.
        /// Se establece automáticamente al crear.
        /// </summary>
        public DateTime FechaCreacion { get; set; }

        /// <summary>
        /// Fecha y hora de la última modificación del registro.
        /// Se actualiza automáticamente al modificar.
        /// </summary>
        public DateTime? FechaModificacion { get; set; }

        /// <summary>
        /// Indica si el registro está activo o fue eliminado lógicamente.
        /// true = activo, false = eliminado (no se borra físicamente de la BD)
        /// </summary>
        public bool Activo { get; set; } = true;
    }
}