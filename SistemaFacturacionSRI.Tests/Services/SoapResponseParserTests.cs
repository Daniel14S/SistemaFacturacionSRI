// Tests/Services/SoapResponseParserTests.cs
// T-074: Tests para el parser de respuestas SOAP

using Xunit;
using Microsoft.Extensions.Logging;
using Moq;
using SistemaFacturacionSRI.Infrastructure.Services.SRI;
using SistemaFacturacionSRI.Domain.DTOs.SRI;

namespace Tests.Services
{
    /// <summary>
    /// T-074: Tests para SoapResponseParser
    /// </summary>
    public class SoapResponseParserTests
    {
        private readonly SoapResponseParser _parser;
        private readonly Mock<ILogger<SoapResponseParser>> _loggerMock;

        public SoapResponseParserTests()
        {
            _loggerMock = new Mock<ILogger<SoapResponseParser>>();
            _parser = new SoapResponseParser(_loggerMock.Object);
        }

        // ============================================================
        // TESTS DE RECEPCIÓN
        // ============================================================

        [Fact]
        public void ParsearRespuestaRecepcion_ConEstadoRecibida_DebeRetornarRecibida()
        {
            // Arrange
            var soapXml = @"<?xml version=""1.0"" encoding=""UTF-8""?>
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

            // Act
            var resultado = _parser.ParsearRespuestaRecepcion(soapXml);

            // Assert
            Assert.NotNull(resultado);
            Assert.Equal("RECIBIDA", resultado.Estado);
            Assert.True(resultado.FueRecibido);
            Assert.False(resultado.FueDevuelto);
            Assert.Single(resultado.Comprobantes);
            Assert.Equal("0312202401123456789000110010010000000011234567819", resultado.Comprobantes[0].ClaveAcceso);
        }

        [Fact]
        public void ParsearRespuestaRecepcion_ConEstadoDevuelta_DebeRetornarDevuelta()
        {
            // Arrange
            var soapXml = @"<?xml version=""1.0"" encoding=""UTF-8""?>
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

            // Act
            var resultado = _parser.ParsearRespuestaRecepcion(soapXml);

            // Assert
            Assert.NotNull(resultado);
            Assert.Equal("DEVUELTA", resultado.Estado);
            Assert.False(resultado.FueRecibido);
            Assert.True(resultado.FueDevuelto);
            
            var errores = resultado.ObtenerErrores();
            Assert.Single(errores);
            Assert.Equal("70", errores[0].Identificador);
            Assert.True(errores[0].EsError);
        }

        [Fact]
        public void ParsearRespuestaRecepcion_ConMultiplesMensajes_DebeRetornarTodos()
        {
            // Arrange
            var soapXml = @"<?xml version=""1.0"" encoding=""UTF-8""?>
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
                        <identificador>65</identificador>
                        <mensaje>CLAVE DE ACCESO INCORRECTA</mensaje>
                        <tipo>ERROR</tipo>
                     </mensaje>
                     <mensaje>
                        <identificador>68</identificador>
                        <mensaje>CERTIFICADO POR EXPIRAR</mensaje>
                        <tipo>ADVERTENCIA</tipo>
                     </mensaje>
                  </mensajes>
               </comprobante>
            </comprobantes>
         </RespuestaRecepcionComprobante>
      </ns2:validarComprobanteResponse>
   </soap:Body>
</soap:Envelope>";

            // Act
            var resultado = _parser.ParsearRespuestaRecepcion(soapXml);

            // Assert
            var comprobante = resultado.Comprobantes.First();
            Assert.Equal(2, comprobante.Mensajes.Count);
            
            var errores = resultado.ObtenerErrores();
            var advertencias = resultado.ObtenerAdvertencias();
            
            Assert.Single(errores);
            Assert.Single(advertencias);
        }

        // ============================================================
        // TESTS DE AUTORIZACIÓN
        // ============================================================

        [Fact]
        public void ParsearRespuestaAutorizacion_ConAutorizado_DebeRetornarAutorizado()
        {
            // Arrange
            var soapXml = @"<?xml version=""1.0"" encoding=""UTF-8""?>
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
                  <comprobante><![CDATA[<?xml version=""1.0""?><factura>...</factura>]]></comprobante>
                  <mensajes>
                     <mensaje>
                        <identificador>60</identificador>
                        <mensaje>COMPROBANTE AUTORIZADO</mensaje>
                        <tipo>INFORMATIVO</tipo>
                     </mensaje>
                  </mensajes>
               </autorizacion>
            </autorizaciones>
         </RespuestaAutorizacionComprobante>
      </ns2:autorizacionComprobanteResponse>
   </soap:Body>
</soap:Envelope>";

            // Act
            var resultado = _parser.ParsearRespuestaAutorizacion(soapXml);

            // Assert
            Assert.NotNull(resultado);
            Assert.Equal("0312202401123456789000110010010000000011234567819", resultado.ClaveAccesoConsultada);
            Assert.Equal(1, resultado.NumeroComprobantes);
            Assert.True(resultado.TieneAutorizaciones);
            Assert.True(resultado.FueAutorizado);
            
            var autorizacion = resultado.PrimeraAutorizacion;
            Assert.NotNull(autorizacion);
            Assert.Equal("AUTORIZADO", autorizacion.Estado);
            Assert.Equal("1234567890", autorizacion.NumeroAutorizacion);
            Assert.Equal("03/12/2024 15:30:45", autorizacion.FechaAutorizacion);
            Assert.NotNull(autorizacion.FechaAutorizacionParsed);
        }

        [Fact]
        public void ParsearRespuestaAutorizacion_ConEnProcesamiento_DebeRetornarEnProcesamiento()
        {
            // Arrange
            var soapXml = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<soap:Envelope xmlns:soap=""http://schemas.xmlsoap.org/soap/envelope/"">
   <soap:Body>
      <ns2:autorizacionComprobanteResponse xmlns:ns2=""http://ec.gob.sri.ws.autorizacion"">
         <RespuestaAutorizacionComprobante>
            <claveAccesoConsultada>0312202401123456789000110010010000000011234567819</claveAccesoConsultada>
            <numeroComprobantes>1</numeroComprobantes>
            <autorizaciones>
               <autorizacion>
                  <estado>EN PROCESAMIENTO</estado>
                  <mensajes>
                     <mensaje>
                        <identificador>99</identificador>
                        <mensaje>Comprobante en procesamiento</mensaje>
                        <tipo>INFORMATIVO</tipo>
                     </mensaje>
                  </mensajes>
               </autorizacion>
            </autorizaciones>
         </RespuestaAutorizacionComprobante>
      </ns2:autorizacionComprobanteResponse>
   </soap:Body>
</soap:Envelope>";

            // Act
            var resultado = _parser.ParsearRespuestaAutorizacion(soapXml);

            // Assert
            var autorizacion = resultado.PrimeraAutorizacion;
            Assert.NotNull(autorizacion);
            Assert.True(autorizacion.EnProcesamiento);
            Assert.False(autorizacion.EstaAutorizado);
            Assert.True(resultado.EnProcesamiento);
        }

        [Fact]
        public void ParsearRespuestaAutorizacion_ConNoAutorizado_DebeRetornarNoAutorizado()
        {
            // Arrange
            var soapXml = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<soap:Envelope xmlns:soap=""http://schemas.xmlsoap.org/soap/envelope/"">
   <soap:Body>
      <ns2:autorizacionComprobanteResponse xmlns:ns2=""http://ec.gob.sri.ws.autorizacion"">
         <RespuestaAutorizacionComprobante>
            <claveAccesoConsultada>0312202401123456789000110010010000000011234567819</claveAccesoConsultada>
            <numeroComprobantes>1</numeroComprobantes>
            <autorizaciones>
               <autorizacion>
                  <estado>NO AUTORIZADO</estado>
                  <ambiente>PRUEBAS</ambiente>
                  <mensajes>
                     <mensaje>
                        <identificador>235</identificador>
                        <mensaje>SECUENCIAL DUPLICADO</mensaje>
                        <tipo>ERROR</tipo>
                     </mensaje>
                  </mensajes>
               </autorizacion>
            </autorizaciones>
         </RespuestaAutorizacionComprobante>
      </ns2:autorizacionComprobanteResponse>
   </soap:Body>
</soap:Envelope>";

            // Act
            var resultado = _parser.ParsearRespuestaAutorizacion(soapXml);

            // Assert
            var autorizacion = resultado.PrimeraAutorizacion;
            Assert.NotNull(autorizacion);
            Assert.True(autorizacion.EstaNoAutorizado);
            Assert.False(autorizacion.EstaAutorizado);
            Assert.True(resultado.FueRechazado);
            Assert.True(autorizacion.TieneErrores);
        }

        // ============================================================
        // TESTS DE UTILIDADES
        // ============================================================

        [Fact]
        public void EsRespuestaSoapValida_ConSoapValido_DebeRetornarTrue()
        {
            // Arrange
            var soapXml = @"<?xml version=""1.0""?>
<soap:Envelope xmlns:soap=""http://schemas.xmlsoap.org/soap/envelope/"">
   <soap:Body>
      <response>Test</response>
   </soap:Body>
</soap:Envelope>";

            // Act
            var esValido = _parser.EsRespuestaSoapValida(soapXml);

            // Assert
            Assert.True(esValido);
        }

        [Fact]
        public void EsRespuestaSoapValida_ConXmlInvalido_DebeRetornarFalse()
        {
            // Arrange
            var xmlInvalido = "esto no es xml";

            // Act
            var esValido = _parser.EsRespuestaSoapValida(xmlInvalido);

            // Assert
            Assert.False(esValido);
        }

        [Fact]
        public void DetectarTipoRespuesta_ConRecepcion_DebeRetornarRecepcion()
        {
            // Arrange
            var soapXml = @"<?xml version=""1.0""?>
<soap:Envelope xmlns:soap=""http://schemas.xmlsoap.org/soap/envelope/"">
   <soap:Body>
      <ns2:validarComprobanteResponse>
         <RespuestaRecepcionComprobante>
            <estado>RECIBIDA</estado>
         </RespuestaRecepcionComprobante>
      </ns2:validarComprobanteResponse>
   </soap:Body>
</soap:Envelope>";

            // Act
            var tipo = _parser.DetectarTipoRespuesta(soapXml);

            // Assert
            Assert.Equal(TipoRespuestaSri.Recepcion, tipo);
        }

        [Fact]
        public void DetectarTipoRespuesta_ConAutorizacion_DebeRetornarAutorizacion()
        {
            // Arrange
            var soapXml = @"<?xml version=""1.0""?>
<soap:Envelope xmlns:soap=""http://schemas.xmlsoap.org/soap/envelope/"">
   <soap:Body>
      <ns2:autorizacionComprobanteResponse>
         <RespuestaAutorizacionComprobante>
            <estado>AUTORIZADO</estado>
         </RespuestaAutorizacionComprobante>
      </ns2:autorizacionComprobanteResponse>
   </soap:Body>
</soap:Envelope>";

            // Act
            var tipo = _parser.DetectarTipoRespuesta(soapXml);

            // Assert
            Assert.Equal(TipoRespuestaSri.Autorizacion, tipo);
        }

        [Fact]
        public void CodigosMensajeSri_EsCodigoExitoso_DebeIdentificarCorrectamente()
        {
            // Act & Assert
            Assert.True(CodigosMensajeSri.EsCodigoExitoso("43"));
            Assert.True(CodigosMensajeSri.EsCodigoExitoso("60"));
            Assert.False(CodigosMensajeSri.EsCodigoExitoso("70"));
        }

        [Fact]
        public void CodigosMensajeSri_EsErrorCritico_DebeIdentificarCorrectamente()
        {
            // Act & Assert
            Assert.True(CodigosMensajeSri.EsErrorCritico("69"));
            Assert.True(CodigosMensajeSri.EsErrorCritico("70"));
            Assert.True(CodigosMensajeSri.EsErrorCritico("235"));
            Assert.False(CodigosMensajeSri.EsErrorCritico("43"));
        }

        [Fact]
        public void MensajeSri_ToString_DebeFormatearCorrectamente()
        {
            // Arrange
            var mensaje = new MensajeSri
            {
                Tipo = "ERROR",
                Identificador = "70",
                Mensaje = "FIRMA ELECTRONICA NO VALIDA",
                InformacionAdicional = "Detalle adicional"
            };

            // Act
            var resultado = mensaje.ToString();

            // Assert
            Assert.Contains("ERROR", resultado);
            Assert.Contains("70", resultado);
            Assert.Contains("FIRMA ELECTRONICA NO VALIDA", resultado);
            Assert.Contains("Detalle adicional", resultado);
        }
    }
}