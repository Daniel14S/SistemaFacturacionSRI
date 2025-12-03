// Tests/Services/FirmaElectronicaServiceTests.cs
using Xunit;
using Moq;
using Microsoft.Extensions.Logging;
using SistemaFacturacionSRI.Domain.Interfaces.Services;
using SistemaFacturacionSRI.Infrastructure.Services;
using System.Security.Cryptography.X509Certificates; // ← Faltaba este using
using SistemaFacturacionSRI.Domain.Models; // ← Para InformacionCertificado y ResultadoValidacionFirma

namespace Tests.Services
{
    /// <summary>
    /// Tests para T-055: IFirmaElectronicaService
    /// </summary>
    public class FirmaElectronicaServiceTests
    {
        private readonly Mock<ICertificadoDigitalService> _certificadoServiceMock;
        private readonly Mock<ILogger<FirmaElectronicaService>> _loggerMock;
        private readonly FirmaElectronicaService _service;

        public FirmaElectronicaServiceTests()
        {
            _certificadoServiceMock = new Mock<ICertificadoDigitalService>();
            _loggerMock = new Mock<ILogger<FirmaElectronicaService>>();
            _service = new FirmaElectronicaService(
                _certificadoServiceMock.Object,
                _loggerMock.Object
            );
        }

        [Fact]
        public void Constructor_ConDependenciasNulas_DebeLanzarExcepcion()
        {
            // Arrange & Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                new FirmaElectronicaService(null!, _loggerMock.Object)
            );

            Assert.Throws<ArgumentNullException>(() =>
                new FirmaElectronicaService(_certificadoServiceMock.Object, null!)
            );
        }

        [Fact]
        public void TieneFirma_ConXmlSinFirma_DebeRetornarFalse()
        {
            // Arrange
            var xmlSinFirma = @"<?xml version=""1.0""?>
                <factura id=""comprobante"">
                    <infoTributaria>
                        <ambiente>1</ambiente>
                    </infoTributaria>
                </factura>";

            // Act
            var tieneFirma = _service.TieneFirma(xmlSinFirma);

            // Assert
            Assert.False(tieneFirma);
        }

        [Fact]
        public void TieneFirma_ConXmlConFirma_DebeRetornarTrue()
        {
            // Arrange
            var xmlConFirma = @"<?xml version=""1.0""?>
                <factura id=""comprobante"">
                    <infoTributaria>
                        <ambiente>1</ambiente>
                    </infoTributaria>
                    <Signature xmlns=""http://www.w3.org/2000/09/xmldsig#"">
                        <SignedInfo></SignedInfo>
                    </Signature>
                </factura>";

            // Act
            var tieneFirma = _service.TieneFirma(xmlConFirma);

            // Assert
            Assert.True(tieneFirma);
        }

        [Fact]
        public void ExtraerXmlOriginal_ConXmlFirmado_DebeEliminarSignature()
        {
            // Arrange
            var xmlFirmado = @"<?xml version=""1.0""?>
                <factura id=""comprobante"">
                    <infoTributaria>
                        <ambiente>1</ambiente>
                    </infoTributaria>
                    <Signature xmlns=""http://www.w3.org/2000/09/xmldsig#"">
                        <SignedInfo></SignedInfo>
                    </Signature>
                </factura>";

            // Act
            var xmlOriginal = _service.ExtraerXmlOriginal(xmlFirmado);

            // Assert
            Assert.DoesNotContain("Signature", xmlOriginal);
            Assert.Contains("infoTributaria", xmlOriginal);
        }

        [Fact]
        public async Task FirmarXml_ConCertificadoValido_DebeLanzarNotImplementedException()
        {
            // Arrange
            var xml = "<factura></factura>";
            var certificadoMock = new Mock<X509Certificate2>();
            certificadoMock.Setup(c => c.HasPrivateKey).Returns(true);
            certificadoMock.Setup(c => c.NotAfter).Returns(DateTime.UtcNow.AddDays(30)); // ← Cambiado a UtcNow
            certificadoMock.Setup(c => c.NotBefore).Returns(DateTime.UtcNow.AddDays(-30)); // ← Cambiado a UtcNow

            _certificadoServiceMock
                .Setup(s => s.ObtenerCertificadoActual())
                .Returns(certificadoMock.Object);
            
            _certificadoServiceMock
                .Setup(s => s.ValidarYRegistrarCertificado(It.IsAny<X509Certificate2>()))
                .Returns(true);

            // Act & Assert
            // T-057 valida el certificado, luego lanza NotImplementedException (firma pendiente)
            await Assert.ThrowsAsync<NotImplementedException>(
                async () => await _service.FirmarXml(xml)
            );
        }

        [Fact]
        public void Constructor_DebeInicializarCertificado()
        {
            // Arrange
            var certificadoMock = new Mock<X509Certificate2>();
            certificadoMock.Setup(c => c.HasPrivateKey).Returns(true);
            certificadoMock.Setup(c => c.NotAfter).Returns(DateTime.UtcNow.AddDays(30)); // ← Cambiado a UtcNow
            certificadoMock.Setup(c => c.NotBefore).Returns(DateTime.UtcNow.AddDays(-30)); // ← Cambiado a UtcNow

            _certificadoServiceMock
                .Setup(s => s.CargarCertificado())
                .Returns(certificadoMock.Object);
            
            _certificadoServiceMock
                .Setup(s => s.ValidarYRegistrarCertificado(It.IsAny<X509Certificate2>()))
                .Returns(true);
            
            _certificadoServiceMock
                .Setup(s => s.ObtenerInformacionCertificado(It.IsAny<X509Certificate2>()))
                .Returns(new InformacionCertificado
                {
                    Subject = "CN=Test",
                    ValidoHasta = DateTime.UtcNow.AddDays(30), // ← Cambiado a UtcNow
                    DiasRestantes = 30
                });

            // Act & Assert - Constructor debe ejecutarse sin errores
            var service = new FirmaElectronicaService(
                _certificadoServiceMock.Object,
                _loggerMock.Object
            );

            // Verificar que se llamó CargarCertificado
            _certificadoServiceMock.Verify(s => s.CargarCertificado(), Times.Once);
        }

        [Fact]
        public async Task ValidarFirmaDetallada_EnEstadoActual_DebeRetornarError()
        {
            // Arrange
            var xmlFirmado = "<factura><Signature></Signature></factura>";

            // Act
            var resultado = await _service.ValidarFirmaDetallada(xmlFirmado);

            // Assert
            Assert.False(resultado.EsValida);
            Assert.NotEmpty(resultado.Errores);
        }

        [Fact]
        public void TieneFirma_ConXmlInvalido_DebeRetornarFalse()
        {
            // Arrange
            var xmlInvalido = "esto no es xml";

            // Act
            var tieneFirma = _service.TieneFirma(xmlInvalido);

            // Assert
            Assert.False(tieneFirma);
        }

        [Fact]
        public void ResultadoValidacionFirma_Exitoso_DebeTenerPropiedadesCorrectas()
        {
            // Arrange & Act
            var resultado = ResultadoValidacionFirma.Exitoso(
                DateTime.UtcNow, // ← Cambiado a UtcNow
                "CN=Test"
            );

            // Assert
            Assert.True(resultado.EsValida);
            Assert.True(resultado.FirmaVerificada);
            Assert.True(resultado.CertificadoValido);
            Assert.NotNull(resultado.FechaFirma);
            Assert.Equal("CN=Test", resultado.SubjectCertificado);
        }

        [Fact]
        public void ResultadoValidacionFirma_ConError_DebeTenerPropiedadesCorrectas()
        {
            // Arrange & Act
            var resultado = ResultadoValidacionFirma.ConError("Error de prueba");

            // Assert
            Assert.False(resultado.EsValida);
            Assert.False(resultado.FirmaVerificada);
            Assert.Single(resultado.Errores);
            Assert.Contains("Error de prueba", resultado.Errores);
        }
    }
}