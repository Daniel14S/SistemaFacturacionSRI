// Tests/Services/FirmaElectronicaErrorHandlingTests.cs
// T-066: Tests de manejo de errores de firma

using Xunit;
using Moq;
using Microsoft.Extensions.Logging;
using SistemaFacturacionSRI.Domain.Interfaces.Services;
using SistemaFacturacionSRI.Domain.Exceptions;
using SistemaFacturacionSRI.Infrastructure.Services;
using System.Security.Cryptography.X509Certificates;

namespace Tests.Services
{
    /// <summary>
    /// Tests para T-066: Manejo de errores de firma electrónica
    /// </summary>
    public class FirmaElectronicaErrorHandlingTests
    {
        private readonly Mock<ICertificadoDigitalService> _certificadoServiceMock;
        private readonly Mock<ILogger<FirmaElectronicaService>> _loggerMock;

        public FirmaElectronicaErrorHandlingTests()
        {
            _certificadoServiceMock = new Mock<ICertificadoDigitalService>();
            _loggerMock = new Mock<ILogger<FirmaElectronicaService>>();
        }

        // ============================================================
        // TESTS DE CERTIFICADO
        // ============================================================

        [Fact]
        public void Constructor_CertificadoExpirado_DebeLanzarCertificadoExpiradoException()
        {
            // Arrange
            var certificadoExpirado = CrearCertificadoMock(
                tieneClavePrivada: true,
                notBefore: DateTime.Now.AddYears(-2),
                notAfter: DateTime.Now.AddDays(-10) // Expirado hace 10 días
            );

            _certificadoServiceMock
                .Setup(s => s.CargarCertificado())
                .Returns(certificadoExpirado);

            _certificadoServiceMock
                .Setup(s => s.ValidarYRegistrarCertificado(It.IsAny<X509Certificate2>()))
                .Returns(false);

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() =>
            {
                new FirmaElectronicaService(_certificadoServiceMock.Object, _loggerMock.Object);
            });
        }

        [Fact]
        public void Constructor_CertificadoSinClavePrivada_DebeLanzarExcepcion()
        {
            // Arrange
            var certificadoSinClave = CrearCertificadoMock(
                tieneClavePrivada: false, // SIN CLAVE PRIVADA
                notBefore: DateTime.Now.AddDays(-30),
                notAfter: DateTime.Now.AddDays(365)
            );

            _certificadoServiceMock
                .Setup(s => s.CargarCertificado())
                .Returns(certificadoSinClave);

            _certificadoServiceMock
                .Setup(s => s.ValidarYRegistrarCertificado(It.IsAny<X509Certificate2>()))
                .Returns(true);

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() =>
            {
                new FirmaElectronicaService(_certificadoServiceMock.Object, _loggerMock.Object);
            });
        }

        // ============================================================
        // TESTS DE XML
        // ============================================================

        [Fact]
        public async Task FirmarXml_XmlVacio_DebeLanzarXmlInvalidoException()
        {
            // Arrange
            var service = CrearServicioConCertificadoValido();
            var xmlVacio = "";

            // Act & Assert
            await Assert.ThrowsAsync<XmlInvalidoException>(async () =>
            {
                await service.FirmarXml(xmlVacio);
            });
        }

        [Fact]
        public async Task FirmarXml_XmlMalFormado_DebeLanzarXmlInvalidoException()
        {
            // Arrange
            var service = CrearServicioConCertificadoValido();
            var xmlMalFormado = "<factura><infoTributaria>"; // Sin cerrar tags

            // Act & Assert
            await Assert.ThrowsAsync<XmlInvalidoException>(async () =>
            {
                await service.FirmarXml(xmlMalFormado);
            });
        }

        [Fact]
        public async Task FirmarXml_XmlYaFirmado_DebeLanzarXmlYaFirmadoException()
        {
            // Arrange
            var service = CrearServicioConCertificadoValido();
            var xmlYaFirmado = @"<?xml version=""1.0""?>
                <factura id=""comprobante"">
                    <infoTributaria>
                        <ambiente>1</ambiente>
                    </infoTributaria>
                    <Signature xmlns=""http://www.w3.org/2000/09/xmldsig#"">
                        <SignedInfo></SignedInfo>
                    </Signature>
                </factura>";

            // Act & Assert
            await Assert.ThrowsAsync<XmlYaFirmadoException>(async () =>
            {
                await service.FirmarXml(xmlYaFirmado);
            });
        }

        [Fact]
        public async Task FirmarXml_XmlSinNodoRaiz_DebeLanzarXmlInvalidoException()
        {
            // Arrange
            var service = CrearServicioConCertificadoValido();
            var xmlSinRaiz = "<?xml version=\"1.0\"?>";

            // Act & Assert
            await Assert.ThrowsAsync<XmlInvalidoException>(async () =>
            {
                await service.FirmarXml(xmlSinRaiz);
            });
        }

        // ============================================================
        // TESTS DE EXCEPCIONES PERSONALIZADAS
        // ============================================================

        [Fact]
        public void CertificadoExpiradoException_DebeContenerFechaExpiracion()
        {
            // Arrange
            var fechaExpiracion = new DateTime(2024, 1, 1);

            // Act
            var exception = new CertificadoExpiradoException(fechaExpiracion);

            // Assert
            Assert.Equal(fechaExpiracion, exception.FechaExpiracion);
            Assert.Contains("expiró", exception.Message.ToLower());
            Assert.Contains("01/01/2024", exception.Message);
            Assert.Equal("CERT_EXPIRADO", exception.CodigoError);
        }

        [Fact]
        public void CertificadoPorExpirarException_DebeContenerDiasRestantes()
        {
            // Arrange
            var fechaExpiracion = DateTime.Now.AddDays(5);
            var diasRestantes = 5;

            // Act
            var exception = new CertificadoPorExpirarException(fechaExpiracion, diasRestantes);

            // Assert
            Assert.Equal(fechaExpiracion, exception.FechaExpiracion);
            Assert.Equal(5, exception.DiasRestantes);
            Assert.Contains("expira en 5 días", exception.Message);
            Assert.Equal("CERT_POR_EXPIRAR", exception.CodigoError);
        }

        [Fact]
        public void ClavePrivadaNoDisponibleException_DebeTenerMensajeCorrecto()
        {
            // Act
            var exception = new ClavePrivadaNoDisponibleException();

            // Assert
            Assert.Contains("clave privada", exception.Message.ToLower());
            Assert.Equal("CERT_SIN_CLAVE", exception.CodigoError);
        }

        [Fact]
        public void XmlInvalidoException_DebeContenerDetallesError()
        {
            // Arrange
            var mensajeError = "Tag no cerrado";

            // Act
            var exception = new XmlInvalidoException(mensajeError);

            // Assert
            Assert.Contains("XML inválido", exception.Message);
            Assert.Contains(mensajeError, exception.Message);
            Assert.Equal("XML_INVALIDO", exception.CodigoError);
        }

        [Fact]
        public void XmlYaFirmadoException_DebeTenerMensajeCorrecto()
        {
            // Act
            var exception = new XmlYaFirmadoException();

            // Assert
            Assert.Contains("ya tiene firma digital", exception.Message);
            Assert.Equal("XML_YA_FIRMADO", exception.CodigoError);
        }

        [Fact]
        public void ErrorFirmaException_DebeIncluirEtapa()
        {
            // Arrange
            var etapa = "calcular_digest";
            var mensaje = "Error al calcular hash";

            // Act
            var exception = new ErrorFirmaException(mensaje, etapa);

            // Assert
            Assert.Equal(etapa, exception.Etapa);
            Assert.Contains(mensaje, exception.Message);
            Assert.Equal("FIRMA_FALLO", exception.CodigoError);
            Assert.True(exception.DatosAdicionales.ContainsKey("etapa"));
        }

        [Fact]
        public void ErrorCalculoDigestException_DebeContenerTipoDigest()
        {
            // Arrange
            var tipoDigest = "SignedProperties";
            var mensaje = "Hash inválido";

            // Act
            var exception = new ErrorCalculoDigestException(tipoDigest, mensaje);

            // Assert
            Assert.Equal(tipoDigest, exception.TipoDigest);
            Assert.Contains(tipoDigest, exception.Message);
            Assert.Equal("DIGEST_ERROR", exception.CodigoError);
        }

        [Fact]
        public void ErrorFirmaRsaException_DebeContenerKeySize()
        {
            // Arrange
            var keySize = 1024;
            var innerException = new Exception("RSA error");

            // Act
            var exception = new ErrorFirmaRsaException("Firma falló", innerException, keySize);

            // Assert
            Assert.Equal(keySize, exception.KeySize);
            Assert.NotNull(exception.InnerException);
            Assert.Equal("RSA_ERROR", exception.CodigoError);
        }

        [Fact]
        public void FirmaInvalidaException_DebeContenerRazones()
        {
            // Arrange
            var razones = new List<string>
            {
                "Digest no coincide",
                "Certificado expirado",
                "Firma RSA inválida"
            };

            // Act
            var exception = new FirmaInvalidaException(razones);

            // Assert
            Assert.Equal(3, exception.RazonesInvalidez.Count);
            Assert.Contains("3", exception.Message); // "Errores encontrados: 3"
            Assert.Equal("FIRMA_INVALIDA", exception.CodigoError);
        }

        [Fact]
        public void ConfiguracionInvalidaException_DebeContenerParametro()
        {
            // Arrange
            var parametro = "RutaCertificado";
            var mensaje = "La ruta no existe";

            // Act
            var exception = new ConfiguracionInvalidaException(parametro, mensaje);

            // Assert
            Assert.Equal(parametro, exception.ParametroInvalido);
            Assert.Contains(parametro, exception.Message);
            Assert.Contains(mensaje, exception.Message);
            Assert.Equal("CONFIG_INVALIDA", exception.CodigoError);
        }

        [Fact]
        public void FirmaElectronicaException_PuedeAgregarDatosAdicionales()
        {
            // Arrange
            var exception = new FirmaElectronicaException("Error de prueba");

            // Act
            exception.AgregarDato("usuario", "Pedro");
            exception.AgregarDato("fecha", DateTime.Now);
            exception.AgregarDato("intentos", 3);

            // Assert
            Assert.Equal(3, exception.DatosAdicionales.Count);
            Assert.Equal("Pedro", exception.DatosAdicionales["usuario"]);
            Assert.True(exception.DatosAdicionales.ContainsKey("intentos"));
        }

        // ============================================================
        // MÉTODOS AUXILIARES
        // ============================================================

        private X509Certificate2 CrearCertificadoMock(
            bool tieneClavePrivada,
            DateTime notBefore,
            DateTime notAfter)
        {
            var mock = new Mock<X509Certificate2>();
            mock.Setup(c => c.HasPrivateKey).Returns(tieneClavePrivada);
            mock.Setup(c => c.NotBefore).Returns(notBefore);
            mock.Setup(c => c.NotAfter).Returns(notAfter);
            mock.Setup(c => c.Subject).Returns("CN=Test Cert");
            mock.Setup(c => c.Issuer).Returns("CN=Test CA");
            
            return mock.Object;
        }

        private FirmaElectronicaService CrearServicioConCertificadoValido()
        {
            var certificadoValido = CrearCertificadoMock(
                tieneClavePrivada: true,
                notBefore: DateTime.Now.AddDays(-30),
                notAfter: DateTime.Now.AddDays(365)
            );

            _certificadoServiceMock
                .Setup(s => s.CargarCertificado())
                .Returns(certificadoValido);

            _certificadoServiceMock
                .Setup(s => s.ValidarYRegistrarCertificado(It.IsAny<X509Certificate2>()))
                .Returns(true);

            _certificadoServiceMock
                .Setup(s => s.ObtenerInformacionCertificado(It.IsAny<X509Certificate2>()))
                .Returns(new InformacionCertificado
                {
                    Subject = "CN=Test",
                    ValidoHasta = DateTime.Now.AddDays(365),
                    DiasRestantes = 365
                });

            return new FirmaElectronicaService(_certificadoServiceMock.Object, _loggerMock.Object);
        }
    }
}