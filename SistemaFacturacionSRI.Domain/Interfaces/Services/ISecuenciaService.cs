namespace SistemaFacturacionSRI.Domain.Interfaces.Services;

/// <summary>
/// Interfaz para el servicio de gestión de secuencias de facturación
/// T-015: SPRINT 3 - DÍA 4
/// 
/// Maneja la generación de números secuenciales thread-safe para facturas
/// Formato: 001-001-000000001 (Establecimiento-PuntoEmision-Secuencial)
/// </summary>
public interface ISecuenciaService
{
    /// <summary>
    /// Obtiene el siguiente número secuencial disponible (solo el número, sin formato)
    /// Ejemplo: Si el último fue "000000005", retorna "000000006"
    /// </summary>
    /// <returns>Número secuencial de 9 dígitos con ceros a la izquierda</returns>
    Task<string> ObtenerSiguienteNumeroAsync();

    /// <summary>
    /// Actualiza el contador de secuencia después de usar un número
    /// Este método debe ser thread-safe para evitar duplicados
    /// </summary>
    /// <returns>True si se actualizó correctamente</returns>
    Task<bool> ActualizarSecuenciaAsync();

    /// <summary>
    /// Genera el número completo de factura con formato
    /// Formato: 001-001-000000001
    /// </summary>
    /// <returns>Número completo de factura formateado</returns>
    Task<string> GenerarNumeroCompletoAsync();

    /// <summary>
    /// Obtiene el número secuencial actual sin incrementarlo
    /// Útil para consultas
    /// </summary>
    /// <returns>Número secuencial actual</returns>
    Task<string> ObtenerSecuencialActualAsync();

    /// <summary>
    /// Resetea la secuencia a un número específico
    /// Solo debe ser usado por administradores y con precaución
    /// </summary>
    /// <param name="nuevoSecuencial">Nuevo número secuencial (9 dígitos)</param>
    /// <returns>True si se reseteo correctamente</returns>
    Task<bool> ResetearSecuenciaAsync(string nuevoSecuencial);

    /// <summary>
    /// Obtiene información completa de la secuencia actual
    /// </summary>
    /// <returns>Objeto con información de establecimiento, punto de emisión y secuencial</returns>
    Task<SecuenciaInfo> ObtenerInfoSecuenciaAsync();
}

/// <summary>
/// Clase para retornar información de la secuencia
/// </summary>
public class SecuenciaInfo
{
    /// <summary>
    /// Código del establecimiento (3 dígitos)
    /// </summary>
    public string Establecimiento { get; set; } = string.Empty;

    /// <summary>
    /// Código del punto de emisión (3 dígitos)
    /// </summary>
    public string PuntoEmision { get; set; } = string.Empty;

    /// <summary>
    /// Número secuencial actual (9 dígitos)
    /// </summary>
    public string SecuencialActual { get; set; } = string.Empty;

    /// <summary>
    /// Siguiente número secuencial disponible (9 dígitos)
    /// </summary>
    public string SiguienteSecuencial { get; set; } = string.Empty;

    /// <summary>
    /// Número completo formateado
    /// Ejemplo: 001-001-000000123
    /// </summary>
    public string NumeroCompleto { get; set; } = string.Empty;

    /// <summary>
    /// Fecha de la última actualización
    /// </summary>
    public DateTime? UltimaActualizacion { get; set; }
}