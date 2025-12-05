using Microsoft.AspNetCore.Mvc;
using System.Collections.Concurrent;
using System.Text;
using System.Xml.Linq;

namespace SistemaFacturacionSRI.WebUI.Controllers
{
    /// <summary>
    /// T-086: Simulador del SRI para desarrollo local.
    /// Permite probar el flujo completo sin certificado digital ni conexión al SRI real.
    /// </summary>
    [ApiController]
    [Route("api/sri-simulator")]
    public class SriSimulatorController : ControllerBase
    {
        private readonly ILogger<SriSimulatorController> _logger;
        
        // Almacenamiento temporal de comprobantes recibidos para devolverlos en la autorización
        private static readonly ConcurrentDictionary<string, string> _comprobantesStore = new();

        public SriSimulatorController(ILogger<SriSimulatorController> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Simula el WebService de Recepción del SRI
        /// </summary>
        [HttpPost("recepcion")]
        [Consumes("application/xml", "text/xml")]
        [Produces("text/xml")]
        public async Task<IActionResult> Recepcion()
        {
            try
            {
                string requestXml;
                using (var reader = new StreamReader(Request.Body, Encoding.UTF8))
                {
                    requestXml = await reader.ReadToEndAsync();
                }

                _logger.LogInformation("SRI SIMULATOR: Recibida solicitud de recepción");

                // Extraer clave de acceso y contenido del XML
                var doc = XDocument.Parse(requestXml);
                
                // El XML viene envuelto en SOAP, necesitamos extraer el contenido real
                // Buscamos el nodo <xml> que contiene el comprobante en base64
                var xmlNode = doc.Descendants().FirstOrDefault(x => x.Name.LocalName == "xml");
                
                string claveAcceso = "0000000000000000000000000000000000000000000000000";
                string contenidoXml = "";

                if (xmlNode != null)
                {
                    // Decodificar base64
                    var base64Xml = xmlNode.Value;
                    var bytes = Convert.FromBase64String(base64Xml);
                    contenidoXml = Encoding.UTF8.GetString(bytes);
                    
                    // Extraer clave de acceso del XML decodificado
                    var docComprobante = XDocument.Parse(contenidoXml);
                    var claveNode = docComprobante.Descendants("claveAcceso").FirstOrDefault();
                    if (claveNode != null)
                    {
                        claveAcceso = claveNode.Value;
                    }
                    
                    // Guardar para la fase de autorización
                    _comprobantesStore[claveAcceso] = contenidoXml;
                    _logger.LogInformation("SRI SIMULATOR: Comprobante guardado. Clave: {Clave}", claveAcceso);
                }

                // Construir respuesta SOAP
                var responseSoap = $@"<soap:Envelope xmlns:soap=""http://schemas.xmlsoap.org/soap/envelope/"">
    <soap:Body>
        <ns2:validarComprobanteResponse xmlns:ns2=""http://ec.gob.sri.ws.recepcion"">
            <RespuestaRecepcionComprobante>
                <estado>RECIBIDA</estado>
                <comprobantes>
                    <comprobante>
                        <claveAcceso>{claveAcceso}</claveAcceso>
                        <mensajes/>
                    </comprobante>
                </comprobantes>
            </RespuestaRecepcionComprobante>
        </ns2:validarComprobanteResponse>
    </soap:Body>
</soap:Envelope>";

                return Content(responseSoap, "text/xml");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SRI SIMULATOR: Error en recepción");
                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// Simula el WebService de Autorización del SRI
        /// </summary>
        [HttpPost("autorizacion")]
        [Consumes("application/xml", "text/xml")]
        [Produces("text/xml")]
        public async Task<IActionResult> Autorizacion()
        {
            try
            {
                string requestXml;
                using (var reader = new StreamReader(Request.Body, Encoding.UTF8))
                {
                    requestXml = await reader.ReadToEndAsync();
                }

                _logger.LogInformation("SRI SIMULATOR: Recibida solicitud de autorización");

                // Extraer clave de acceso solicitada
                var doc = XDocument.Parse(requestXml);
                var claveNode = doc.Descendants().FirstOrDefault(x => x.Name.LocalName == "claveAccesoComprobante");
                
                string claveAcceso = claveNode?.Value ?? "0000000000000000000000000000000000000000000000000";
                
                // Recuperar XML almacenado
                string comprobanteXml = "";
                if (_comprobantesStore.TryGetValue(claveAcceso, out var xmlGuardado))
                {
                    comprobanteXml = xmlGuardado;
                }
                else
                {
                    _logger.LogWarning("SRI SIMULATOR: No se encontró XML para clave {Clave}", claveAcceso);
                    comprobanteXml = "<xml>Contenido no encontrado en simulador</xml>";
                }

                // Generar número de autorización aleatorio
                var numeroAutorizacion = DateTime.Now.ToString("yyyyMMddHHmmss") + new Random().Next(1000, 9999);
                var fechaAutorizacion = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:sszzz");

                // Construir respuesta SOAP
                var responseSoap = $@"<soap:Envelope xmlns:soap=""http://schemas.xmlsoap.org/soap/envelope/"">
    <soap:Body>
        <ns2:autorizacionComprobanteResponse xmlns:ns2=""http://ec.gob.sri.ws.autorizacion"">
            <RespuestaAutorizacionComprobante>
                <claveAccesoConsultada>{claveAcceso}</claveAccesoConsultada>
                <numeroComprobantes>1</numeroComprobantes>
                <autorizaciones>
                    <autorizacion>
                        <estado>AUTORIZADO</estado>
                        <numeroAutorizacion>{numeroAutorizacion}</numeroAutorizacion>
                        <fechaAutorizacion>{fechaAutorizacion}</fechaAutorizacion>
                        <ambiente>PRUEBAS</ambiente>
                        <comprobante><![CDATA[{comprobanteXml}]]></comprobante>
                        <mensajes/>
                    </autorizacion>
                </autorizaciones>
            </RespuestaAutorizacionComprobante>
        </ns2:autorizacionComprobanteResponse>
    </soap:Body>
</soap:Envelope>";

                return Content(responseSoap, "text/xml");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SRI SIMULATOR: Error en autorización");
                return BadRequest(ex.Message);
            }
        }
    }
}
