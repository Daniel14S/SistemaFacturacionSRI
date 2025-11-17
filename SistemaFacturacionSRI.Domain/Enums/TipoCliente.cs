namespace SistemaFacturacionSRI.Domain.Enums
{
    /// <summary>
    /// Tipos de cliente según la clasificación tributaria ecuatoriana.
    /// Define la naturaleza jurídica del cliente y el tipo de documento de identificación.
    /// </summary>
    public enum TipoCliente
    {
        /// <summary>
        /// Persona Natural Ecuatoriana.
        /// Cliente individual ecuatoriano que actúa a título personal.
        /// Usa cédula de identidad ecuatoriana (10 dígitos).
        /// Ejemplo: 1234567890
        /// </summary>
        PersonaNatural = 1,

        /// <summary>
        /// Empresa o Persona Jurídica.
        /// Organización legalmente constituida en Ecuador.
        /// Usa RUC (Registro Único de Contribuyentes - 13 dígitos).
        /// Ejemplo: 1234567890001
        /// </summary>
        Empresa = 2,

        /// <summary>
        /// Persona Extranjera con Pasaporte.
        /// Cliente extranjero que no posee cédula ecuatoriana.
        /// Usa número de pasaporte (formato variable, generalmente alfanumérico).
        /// Ejemplo: AB123456, X1234567
        /// </summary>
        Pasaporte = 3
    }
}