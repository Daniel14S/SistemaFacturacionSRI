// T-075: Interfaz para cliente de WebServices SOAP del SRI

using SistemaFacturacionSRI.Domain.DTOs.SRI;

namespace SistemaFacturacionSRI.Domain.Interfaces.Services
{
    /// <summary>
    /// T-075: Cliente para consumir WebServices SOAP del SRI Ecuador
    /// </summary>
    public interface ISriWebServiceClient
    {

        Task<RespuestaRecepcionComprobante> EnviarComprobanteAsync(
            RecepcionComprobanteRequest request,
            CancellationToken cancellationToken = default);

        Task<RespuestaAutorizacionComprobante> ConsultarAutorizacionAsync(
            AutorizacionComprobanteRequest request,
            CancellationToken cancellationToken = default);

        Task<bool> VerificarConectividadAsync();

        Task<EstadoServiciosSri> ObtenerEstadoServiciosAsync();
    }

    public class EstadoServiciosSri
    {
        public bool RecepcionDisponible { get; set; }

        public bool AutorizacionDisponible { get; set; }

        public long? TiempoRespuestaRecepcionMs { get; set; }

        public long? TiempoRespuestaAutorizacionMs { get; set; }

        public string? UltimoError { get; set; }

        public DateTime FechaVerificacion { get; set; } = DateTime.Now;

        public bool TodosLosServiciosDisponibles =>
            RecepcionDisponible && AutorizacionDisponible;
    }
}