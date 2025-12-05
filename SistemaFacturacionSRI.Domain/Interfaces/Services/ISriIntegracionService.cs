// SistemaFacturacionSRI.Domain/Interfaces/Services/ISriIntegracionService.cs
// T-082: Interfaz del orquestador completo de integración con el SRI

using SistemaFacturacionSRI.Domain.DTOs.SRI;
using SistemaFacturacionSRI.Domain.Entities;

namespace SistemaFacturacionSRI.Domain.Interfaces.Services
{
    /// <summary>
    /// T-082: Orquestador completo para la integración con el SRI
    /// Maneja el flujo: Generar XML → Firmar → Enviar → Consultar → Actualizar BD
    /// </summary>
    public interface ISriIntegracionService
    {
        Task<ResultadoIntegracionSri> ProcesarFacturaCompletaAsync(
            int facturaId,
            CancellationToken cancellationToken = default);

        Task<List<ResultadoIntegracionSri>> ProcesarFacturasLoteAsync(
            List<int> facturasIds,
            CancellationToken cancellationToken = default);

        Task<ResultadoIntegracionSri> ReprocesarFacturaAsync(
            int facturaId,
            CancellationToken cancellationToken = default);

        Task<ResultadoIntegracionSri> ConsultarEstadoFacturaAsync(
            int facturaId,
            CancellationToken cancellationToken = default);

        Task<EstadoServiciosSri> VerificarEstadoSriAsync();
    }

    /// <summary>
    /// T-082: Resultado de la integración completa con el SRI
    /// </summary>
    public class ResultadoIntegracionSri
    {
        public int FacturaId { get; set; }

        public bool Exitoso { get; set; }

        public string ClaveAcceso { get; set; } = string.Empty;

        public string EstadoFinal { get; set; } = string.Empty;
        public string? NumeroAutorizacion { get; set; }

        public DateTime? FechaAutorizacion { get; set; }

        public string? XmlFirmado { get; set; }

        public string? XmlAutorizado { get; set; }

        public List<MensajeSri> Mensajes { get; set; } = new List<MensajeSri>();

        public string? MensajeError { get; set; }

        public Dictionary<string, EtapaProcesoSri> Etapas { get; set; } = new Dictionary<string, EtapaProcesoSri>();

        public TimeSpan TiempoTotal { get; set; }

        public int TotalIntentos { get; set; }

        public bool EstadoActualizadoEnBd { get; set; }

        public static ResultadoIntegracionSri CrearExitoso(
            int facturaId,
            string claveAcceso,
            string numeroAutorizacion,
            DateTime fechaAutorizacion,
            string xmlAutorizado)
        {
            return new ResultadoIntegracionSri
            {
                FacturaId = facturaId,
                Exitoso = true,
                ClaveAcceso = claveAcceso,
                EstadoFinal = "AUTORIZADO",
                NumeroAutorizacion = numeroAutorizacion,
                FechaAutorizacion = fechaAutorizacion,
                XmlAutorizado = xmlAutorizado,
                EstadoActualizadoEnBd = true
            };
        }

        /// <summary>
        /// Crea un resultado fallido
        /// </summary>
        public static ResultadoIntegracionSri Fallido(
            int facturaId,
            string claveAcceso,
            string estadoFinal,
            string mensajeError)
        {
            return new ResultadoIntegracionSri
            {
                FacturaId = facturaId,
                Exitoso = false,
                ClaveAcceso = claveAcceso,
                EstadoFinal = estadoFinal,
                MensajeError = mensajeError
            };
        }

        /// <summary>
        /// Agrega información de una etapa
        /// </summary>
        public void AgregarEtapa(string nombre, bool exitosa, TimeSpan duracion, string? error = null)
        {
            Etapas[nombre] = new EtapaProcesoSri
            {
                Nombre = nombre,
                Exitosa = exitosa,
                Duracion = duracion,
                Error = error
            };
        }

        public string ObtenerResumen()
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"Factura ID: {FacturaId}");
            sb.AppendLine($"Estado: {(Exitoso ? "✅ Exitoso" : "❌ Fallido")}");
            sb.AppendLine($"Clave Acceso: {ClaveAcceso}");
            sb.AppendLine($"Estado Final: {EstadoFinal}");

            if (!string.IsNullOrEmpty(NumeroAutorizacion))
            {
                sb.AppendLine($"Número Autorización: {NumeroAutorizacion}");
            }

            if (FechaAutorizacion.HasValue)
            {
                sb.AppendLine($"Fecha Autorización: {FechaAutorizacion:dd/MM/yyyy HH:mm:ss}");
            }

            if (!string.IsNullOrEmpty(MensajeError))
            {
                sb.AppendLine($"Error: {MensajeError}");
            }

            sb.AppendLine($"\nEtapas completadas: {Etapas.Count}");
            foreach (var etapa in Etapas.Values)
            {
                sb.AppendLine($"  {(etapa.Exitosa ? "✅" : "❌")} {etapa.Nombre} ({etapa.Duracion.TotalSeconds:F2}s)");
                if (!string.IsNullOrEmpty(etapa.Error))
                {
                    sb.AppendLine($"     Error: {etapa.Error}");
                }
            }

            sb.AppendLine($"\nTiempo Total: {TiempoTotal.TotalSeconds:F2}s");
            sb.AppendLine($"Total Intentos: {TotalIntentos}");

            return sb.ToString();
        }
    }
    public class EtapaProcesoSri
    {
        public string Nombre { get; set; } = string.Empty;
        public bool Exitosa { get; set; }
        public TimeSpan Duracion { get; set; }
        public string? Error { get; set; }
        public DateTime Inicio { get; set; } = DateTime.Now;
        public DateTime? Fin { get; set; }
    }

    /// <summary>
    /// T-082: Opciones para el procesamiento de facturas
    /// </summary>
    public class OpcionesProcesamiento
    {

        public bool ContinuarEnError { get; set; } = true;

        public int MaxParalelismo { get; set; } = 1;

        public bool GuardarXmlEnDisco { get; set; } = true;

        public string? RutaXml { get; set; }

        public bool ActualizarEstadoAutomatico { get; set; } = true;

        public bool ReintentarAutomatico { get; set; } = true;
    }
}