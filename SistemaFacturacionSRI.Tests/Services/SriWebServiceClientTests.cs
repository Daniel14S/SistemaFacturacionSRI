// Tests/Services/SriWebServiceClientTests.cs
// T-075, T-076, T-077: Tests del cliente SOAP

using Xunit;
using Moq;
using Moq.Protected;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net;
using SistemaFacturacionSRI.Domain.Configuration;
using SistemaFacturacionSRI.Domain.DTOs.SRI;
using SistemaFacturacionSRI.Infrastructure.Services.SRI;

namespace Tests.Services
{
    /// <summary>
    /// Tests para SriWebServiceClient
    /// </summary>
    public class SriWebServiceClientTests
    {
        private readonly Mock<ILogger<SriWebServiceClient>> _loggerMock;
        private readonly Mock<ILogger<SoapResponseParser>> _parserLoggerMock;
        private readonly SriWebServicesOptions _options;
        private readonly SoapResponseParser _parser;

        public SriWebServiceClientTests()
        {
            _loggerMock = new Mock<ILogger<SriWebServiceClient>>();
            _parserLoggerMock = new Mock<ILogger<SoapResponseParser>>();
            _parser = new SoapResponseParser(_parserLoggerMock.Object);

            _options = new SriWebServicesOptions
            {
                Ambiente = "PRUEBAS",
                AmbienteCodigo = 1,
                UrlRecepcion = "https://cel.sri.gob.ec/comprobantes-electronicos-ws/RecepcionComprobantesOffline",
                UrlAutorizacion = "https://cel.sri.gob.ec/comprobantes-electronicos-ws/AutorizacionComprobantesOffline",
                TimeoutSegundos = 30,
                ReintentoMaximo = 3,
                DelayEntreReintentosMs = 2000,
                LogSoapDetallado = false,
                ValidarCertificadoSsl = true,
                UserAgent = "SistemaFacturacionSRI-Test/1.0"
            };
        }

        // ============================================================
        // TESTS DE RECEPCIÓN
        // ============================================================

        [Fact]
        public async Task EnviarComprobanteAsync_ConRespuestaRecibida_DebeRetornarRecibida()
        {
            // Arrange
            var mockResponse = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<soap:Envelope xmlns:soap=""http://schemas.xmlsoap.org/soap/envelope/"">
   <soap:Body>
      <ns2:validarComprobanteResponse xmlns:ns2=""http://ec.gob.sri.ws.recepcion"">
         <RespuestaRecepcionComprobante>
            <estado>RECIBIDA</estado>
            <comprobantes>
               <comprobante>
                  <claveAcceso>0312202401123456789000110010010000000011234567819</claveAcceso>
                  <mensajes>
                     <mensaje>
                        <identificador>43</identificador>
                        <mensaje>CLAVE ACCESO REGISTRADA</mensaje>
                        <tipo>INFORMATIVO</tipo>
                     </mensaje>
                  </mensajes>
               </comprobante>
            </comprobantes>
         </RespuestaRecepcionComprobante>
      </ns2:validarComprobanteResponse>
   </soap:Body>
</soap:Envelope>";

            var httpClient = CrearHttpClientMock(mockResponse, HttpStatusCode.OK);
            var client = new SriWebServiceClient(
                httpClient,
                Options.Create(_options),
                _parser,
                _loggerMock.Object);

            var request = new RecepcionComprobanteRequest
            {
                ClaveAcceso = "0312202401123456789000110010010000000011234567819",
                XmlComprobante = "<factura>TEST</factura>",
                RucEmisor = "1234567890001",
                FechaEmision = DateTime.Now
            };

            // Act
            var resultado = await client.EnviarComprobanteAsync(request);

            // Assert
            Assert.NotNull(resultado);
            Assert.Equal("RECIBIDA", resultado.Estado);
            Assert.True(resultado.FueRecibido);
            Assert.False(resultado.FueDevuelto);
        }

        [Fact]
        public async Task EnviarComprobanteAsync_ConRespuestaDevuelta_DebeRetornarDevuelta()
        {
            // Arrange
            var mockResponse = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<soap:Envelope xmlns:soap=""http://schemas.xmlsoap.org/soap/envelope/"">
   <soap:Body>
      <ns2:validarComprobanteResponse xmlns:ns2=""http://ec.gob.sri.ws.recepcion"">
         <RespuestaRecepcionComprobante>
            <estado>DEVUELTA</estado>
            <comprobantes>
               <comprobante>
                  <claveAcceso>0312202401123456789000110010010000000011234567819</claveAcceso>
                  <mensajes>
                     <mensaje>
                        <identificador>70</identificador>
                        <mensaje>FIRMA ELECTRONICA NO VALIDA</mensaje>
                        <tipo>ERROR</tipo>
                     </mensaje>
                  </mensajes>
               </comprobante>
            </comprobantes>
         </RespuestaRecepcionComprobante>
      </ns2:validarComprobanteResponse>
   </soap:Body>
</soap:Envelope>";

            var httpClient = CrearHttpClientMock(mockResponse, HttpStatusCode.OK);
            var client = new SriWebServiceClient(
                httpClient,
                Options.Create(_options),
                _parser,
                _loggerMock.Object);

            var request = new RecepcionComprobanteRequest
            {
                ClaveAcceso = "0312202401123456789000110010010000000011234567819",
                XmlComprobante = "<factura>TEST</factura>",
                RucEmisor = "1234567890001",
                FechaEmision = DateTime.Now
            };

            // Act
            var resultado = await client.EnviarComprobanteAsync(request);

            // Assert
            Assert.NotNull(resultado);
            Assert.Equal("DEVUELTA", resultado.Estado);
            Assert.False(resultado.FueRecibido);
            Assert.True(resultado.FueDevuelto);
            
            var errores = resultado.ObtenerErrores();
            Assert.Single(errores);
            Assert.Equal("70", errores[0].Identificador);
        }

        // ============================================================
        // TESTS DE AUTORIZACIÓN
        // ============================================================

        [Fact]
        public async Task ConsultarAutorizacionAsync_ConAutorizado_DebeRetornarAutorizado()
        {
            // Arrange
            var mockResponse = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<soap:Envelope xmlns:soap=""http://schemas.xmlsoap.org/soap/envelope/"">
   <soap:Body>
      <ns2:autorizacionComprobanteResponse xmlns:ns2=""http://ec.gob.sri.ws.autorizacion"">
         <RespuestaAutorizacionComprobante>
            <claveAccesoConsultada>0312202401123456789000110010010000000011234567819</claveAccesoConsultada>
            <numeroComprobantes>1</numeroComprobantes>
            <autorizaciones>
               <autorizacion>
                  <estado>AUTORIZADO</estado>
                  <numeroAutorizacion>1234567890</numeroAutorizacion>
                  <fechaAutorizacion>03/12/2024 15:30:45</fechaAutorizacion>
                  <ambiente>PRUEBAS</ambiente>
                  <comprobante><![CDATA[<factura>...</factura>]]></comprobante>
               </autorizacion>
            </autorizaciones>
         </RespuestaAutorizacionComprobante>
      </ns2:autorizacionComprobanteResponse>
   </soap:Body>
</soap:Envelope>";

            var httpClient = CrearHttpClientMock(mockResponse, HttpStatusCode.OK);
            var client = new SriWebServiceClient(
                httpClient,
                Options.Create(_options),
                _parser,
                _loggerMock.Object);

            var request = new AutorizacionComprobanteRequest
            {
                ClaveAcceso = "0312202401123456789000110010010000000011234567819"
            };

            // Act
            var resultado = await client.ConsultarAutorizacionAsync(request);

            // Assert
            Assert.NotNull(resultado);
            Assert.True(resultado.FueAutorizado);
            Assert.NotNull(resultado.PrimeraAutorizacion);
            Assert.Equal("AUTORIZADO", resultado.PrimeraAutorizacion.Estado);
            Assert.Equal("1234567890", resultado.PrimeraAutorizacion.NumeroAutorizacion);
        }

        [Fact]
        public async Task ConsultarAutorizacionAsync_ConEnProcesamiento_DebeRetornarEnProcesamiento()
        {
            // Arrange
            var mockResponse = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<soap:Envelope xmlns:soap=""http://schemas.xmlsoap.org/soap/envelope/"">
   <soap:Body>
      <ns2:autorizacionComprobanteResponse xmlns:ns2=""http://ec.gob.sri.ws.autorizacion"">
         <RespuestaAutorizacionComprobante>
            <claveAccesoConsultada>0312202401123456789000110010010000000011234567819</claveAccesoConsultada>
            <numeroComprobantes>1</numeroComprobantes>
            <autorizaciones>
               <autorizacion>
                  <estado>EN PROCESAMIENTO</estado>
               </autorizacion>
            </autorizaciones>
         </RespuestaAutorizacionComprobante>
      </ns2:autorizacionComprobanteResponse>
   </soap:Body>
</soap:Envelope>";

            var httpClient = CrearHttpClientMock(mockResponse, HttpStatusCode.OK);
            var client = new SriWebServiceClient(
                httpClient,
                Options.Create(_options),
                _parser,
                _loggerMock.Object);

            var request = new AutorizacionComprobanteRequest
            {
                ClaveAcceso = "0312202401123456789000110010010000000011234567819"
            };

            // Act
            var resultado = await client.ConsultarAutorizacionAsync(request);

            // Assert
            Assert.NotNull(resultado);
            Assert.True(resultado.EnProcesamiento);
            Assert.False(resultado.FueAutorizado);
            Assert.NotNull(resultado.PrimeraAutorizacion);
            Assert.True(resultado.PrimeraAutorizacion.EnProcesamiento);
        }

        // ============================================================
        // TESTS DE CONECTIVIDAD
        // ============================================================

        [Fact]
        public async Task VerificarConectividadAsync_ConServidorAccesible_DebeRetornarTrue()
        {
            // Arrange
            var httpClient = CrearHttpClientMock("", HttpStatusCode.MethodNotAllowed);
            var client = new SriWebServiceClient(
                httpClient,
                Options.Create(_options),
                _parser,
                _loggerMock.Object);

            // Act
            var resultado = await client.VerificarConectividadAsync();

            // Assert
            Assert.True(resultado);
        }

        [Fact]
        public async Task ObtenerEstadoServiciosAsync_DebeRetornarEstado()
        {
            // Arrange
            var httpClient = CrearHttpClientMock("", HttpStatusCode.MethodNotAllowed);
            var client = new SriWebServiceClient(
                httpClient,
                Options.Create(_options),
                _parser,
                _loggerMock.Object);

            // Act
            var resultado = await client.ObtenerEstadoServiciosAsync();

            // Assert
            Assert.NotNull(resultado);
            Assert.True(resultado.RecepcionDisponible);
            Assert.True(resultado.AutorizacionDisponible);
        }

        // ============================================================
        // TESTS DE ERRORES
        // ============================================================

        [Fact]
        public async Task EnviarComprobanteAsync_ConTimeout_DebeLanzarTimeoutException()
        {
            // Arrange
            var handlerMock = new Mock<HttpMessageHandler>();
            handlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ThrowsAsync(new TaskCanceledException("Timeout"));

            var httpClient = new HttpClient(handlerMock.Object);
            var client = new SriWebServiceClient(
                httpClient,
                Options.Create(_options),
                _parser,
                _loggerMock.Object);

            var request = new RecepcionComprobanteRequest
            {
                ClaveAcceso = "0312202401123456789000110010010000000011234567819",
                XmlComprobante = "<factura>TEST</factura>",
                RucEmisor = "1234567890001",
                FechaEmision = DateTime.Now
            };

            // Act & Assert
            await Assert.ThrowsAsync<TimeoutException>(
                () => client.EnviarComprobanteAsync(request));
        }

        // ============================================================
        // MÉTODOS AUXILIARES
        // ============================================================

        private HttpClient CrearHttpClientMock(string responseContent, HttpStatusCode statusCode)
        {
            var handlerMock = new Mock<HttpMessageHandler>();
            
            handlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = statusCode,
                    Content = new StringContent(responseContent, System.Text.Encoding.UTF8, "text/xml")
                });

            return new HttpClient(handlerMock.Object);
        }
    }
}