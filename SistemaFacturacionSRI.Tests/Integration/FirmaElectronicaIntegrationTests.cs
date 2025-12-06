// Tests/Integration/FirmaElectronicaIntegrationTests.cs
// T-067: Pruebas con certificado real

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using SistemaFacturacionSRI.Domain.Configuration;
using SistemaFacturacionSRI.Domain.DTOs.Configuracion;
using SistemaFacturacionSRI.Domain.Interfaces.Services;
using SistemaFacturacionSRI.Infrastructure.Services;
using SistemaFacturacionSRI.Infrastructure.Tools;
using Xunit;

namespace Tests.Integration
{
    /// <summary>
    /// T-067: Tests de integración con certificado real
    /// IMPORTANTE: Estos tests requieren el certificado certificado-sri-pruebas.p12
    /// </summary>
    [Collection("Integration Tests")]
    public class FirmaElectronicaIntegrationTests : IDisposable
    {
        private readonly CertificadoDigitalService _certificadoService;
        private readonly FirmaElectronicaService _firmaService;
        private readonly ILogger<CertificadoDigitalService> _certLogger;
        private readonly ILogger<FirmaElectronicaService> _firmaLogger;
        private readonly Mock<ICertificadoDigitalStorageService> _certStorageMock = new();
        private readonly string _rutaCertificado;
        private readonly string _claveCertificado;

        public FirmaElectronicaIntegrationTests()
        {
            // Configuración del certificado de pruebas
            _rutaCertificado = "Infrastructure/Resources/Certificados/pruebas/certificado-sri-pruebas.p12";
            _claveCertificado = "tu_clave_aqui"; // CAMBIAR POR LA CLAVE REAL

            // Loggers
            var certLoggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
            _certLogger = certLoggerFactory.CreateLogger<CertificadoDigitalService>();

            var firmaLoggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
            _firmaLogger = firmaLoggerFactory.CreateLogger<FirmaElectronicaService>();

            // Opciones de certificado
            var options = new CertificadoDigitalOptions
            {
                RutaCertificado = _rutaCertificado,
                ClaveCertificado = _claveCertificado,
                TipoCertificado = "PRUEBAS",
                ValidarVigencia = true,
                ValidarCadenaConfianza = false // false para ambiente de pruebas
            };

            var optionsMock = Options.Create(options);

            // Crear servicios
            _certStorageMock.Setup(s => s.ObtenerCertificadoActivoAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync((CertificadoDigitalActivoDto?)null);

            _certificadoService = new CertificadoDigitalService(optionsMock, _certLogger, _certStorageMock.Object);
            _firmaService = new FirmaElectronicaService(_certificadoService, _firmaLogger);
        }

        // ============================================================
        // T-067: TESTS CON CERTIFICADO REAL
        // ============================================================

        [Fact(Skip = "Requiere certificado real y clave configurada")]
        // Remover Skip cuando tengas el certificado configurado
        public void Test01_CargarCertificadoReal_DebeCargarCorrectamente()
        {
            // Act
            var certificado = _certificadoService.CargarCertificado();

            // Assert
            Assert.NotNull(certificado);
            Assert.True(certificado.HasPrivateKey, "El certificado debe tener clave privada");
            Assert.True(DateTime.Now >= certificado.NotBefore, "Certificado aún no válido");
            Assert.True(DateTime.Now <= certificado.NotAfter, "Certificado expirado");

            // Log información
            var info = _certificadoService.ObtenerInformacionCertificado(certificado);
            Assert.NotNull(info);
            Assert.NotEmpty(info.Subject);
            
            Console.WriteLine("═══════════════════════════════════════");
            Console.WriteLine("INFORMACIÓN DEL CERTIFICADO");
            Console.WriteLine("═══════════════════════════════════════");
            Console.WriteLine($"Subject: {info.Subject}");
            Console.WriteLine($"Issuer: {info.Issuer}");
            Console.WriteLine($"Válido desde: {info.ValidoDesde:dd/MM/yyyy}");
            Console.WriteLine($"Válido hasta: {info.ValidoHasta:dd/MM/yyyy}");
            Console.WriteLine($"Días restantes: {info.DiasRestantes}");
            Console.WriteLine($"Tiene clave privada: {info.TieneClavePrivada}");
            Console.WriteLine("═══════════════════════════════════════");
        }

        [Fact(Skip = "Requiere certificado real y clave configurada")]
        public void Test02_ValidarCertificadoReal_DebeSerValido()
        {
            // Arrange
            var certificado = _certificadoService.CargarCertificado();

            // Act
            var esValido = _certificadoService.ValidarYRegistrarCertificado(certificado);

            // Assert
            Assert.True(esValido, "El certificado debe ser válido para firma SRI");
        }

        [Fact(Skip = "Requiere certificado real y clave configurada")]
        public void Test03_ImprimirReporteCertificado_DebeMostrarInformacionCompleta()
        {
            // Arrange
            var certificado = _certificadoService.CargarCertificado();

            // Act
            CertificadoDiagnosticTool.ImprimirReporte(certificado);

            // Assert
            Assert.True(certificado.HasPrivateKey);
        }

        [Fact(Skip = "Requiere certificado real y clave configurada")]
        public async Task Test04_FirmarFacturaSimple_DebeGenerarXmlFirmado()
        {
            // Arrange
            var xmlFactura = CrearXmlFacturaEjemplo();

            // Act
            var xmlFirmado = await _firmaService.FirmarXml(xmlFactura);

            // Assert
            Assert.NotNull(xmlFirmado);
            Assert.NotEmpty(xmlFirmado);
            Assert.Contains("<ds:Signature", xmlFirmado);
            Assert.Contains("<ds:SignedInfo", xmlFirmado);
            Assert.Contains("<ds:SignatureValue", xmlFirmado);
            Assert.Contains("<ds:KeyInfo", xmlFirmado);
            Assert.Contains("<etsi:SignedProperties", xmlFirmado);

            // Verificar que el XML es válido
            var doc = new XmlDocument();
            doc.LoadXml(xmlFirmado); // No debe lanzar excepción

            // Guardar para inspección manual
            var rutaSalida = "factura_firmada_test.xml";
            File.WriteAllText(rutaSalida, xmlFirmado);
            
            Console.WriteLine($"✅ XML firmado guardado en: {rutaSalida}");
            Console.WriteLine($"📄 Tamaño: {xmlFirmado.Length} bytes");
        }

        [Fact(Skip = "Requiere certificado real y clave configurada")]
        public async Task Test05_FirmarYValidar_DebeSerFirmaValida()
        {
            // Arrange
            var xmlFactura = CrearXmlFacturaEjemplo();

            // Act - Firmar
            var xmlFirmado = await _firmaService.FirmarXml(xmlFactura);

            // Act - Validar
            var esValida = await _firmaService.ValidarFirma(xmlFirmado);

            // Assert
            Assert.True(esValida, "La firma debe ser válida");
        }

        [Fact(Skip = "Requiere certificado real y clave configurada")]
        public async Task Test06_ValidarFirmaDetallada_DebeRetornarInformacionCompleta()
        {
            // Arrange
            var xmlFactura = CrearXmlFacturaEjemplo();
            var xmlFirmado = await _firmaService.FirmarXml(xmlFactura);

            // Act
            var resultado = await _firmaService.ValidarFirmaDetallada(xmlFirmado);

            // Assert
            Assert.True(resultado.EsValida);
            Assert.True(resultado.FirmaVerificada);
            Assert.True(resultado.CertificadoValido);
            Assert.NotNull(resultado.FechaFirma);
            Assert.NotNull(resultado.SubjectCertificado);
            Assert.Empty(resultado.Errores);

            Console.WriteLine("═══════════════════════════════════════");
            Console.WriteLine("RESULTADO VALIDACIÓN DE FIRMA");
            Console.WriteLine("═══════════════════════════════════════");
            Console.WriteLine($"Es válida: {resultado.EsValida}");
            Console.WriteLine($"Firma verificada: {resultado.FirmaVerificada}");
            Console.WriteLine($"Certificado válido: {resultado.CertificadoValido}");
            Console.WriteLine($"Fecha firma: {resultado.FechaFirma}");
            Console.WriteLine($"Certificado: {resultado.SubjectCertificado}");
            Console.WriteLine($"Mensaje: {resultado.Mensaje}");
            Console.WriteLine("═══════════════════════════════════════");
        }

        [Fact(Skip = "Requiere certificado real y clave configurada")]
        public async Task Test07_ObtenerInformacionCertificadoFirma_DebeExtraerDatos()
        {
            // Arrange
            var xmlFactura = CrearXmlFacturaEjemplo();
            var xmlFirmado = await _firmaService.FirmarXml(xmlFactura);

            // Act
            var info = await _firmaService.ObtenerInformacionCertificadoFirma(xmlFirmado);

            // Assert
            Assert.NotNull(info);
            Assert.NotEmpty(info.Subject);
            Assert.NotEmpty(info.Issuer);
            Assert.NotEmpty(info.SerialNumber);
            Assert.NotNull(info.FechaFirma);
            Assert.True(info.EstaVigente);
            Assert.Equal("RSA-SHA1", info.AlgoritmoFirma);

            Console.WriteLine("═══════════════════════════════════════");
            Console.WriteLine("INFORMACIÓN CERTIFICADO EN XML FIRMADO");
            Console.WriteLine("═══════════════════════════════════════");
            Console.WriteLine($"Subject: {info.Subject}");
            Console.WriteLine($"Issuer: {info.Issuer}");
            Console.WriteLine($"Serial: {info.SerialNumber}");
            Console.WriteLine($"Fecha firma: {info.FechaFirma}");
            Console.WriteLine($"Vigente: {info.EstaVigente}");
            Console.WriteLine($"Algoritmo: {info.AlgoritmoFirma}");
            Console.WriteLine("═══════════════════════════════════════");
        }

        [Fact(Skip = "Requiere certificado real y clave configurada")]
        public async Task Test08_ExtraerXmlOriginal_DebeQuitarFirma()
        {
            // Arrange
            var xmlOriginal = CrearXmlFacturaEjemplo();
            var xmlFirmado = await _firmaService.FirmarXml(xmlOriginal);

            // Act
            var xmlExtraido = _firmaService.ExtraerXmlOriginal(xmlFirmado);

            // Assert
            Assert.NotNull(xmlExtraido);
            Assert.DoesNotContain("<ds:Signature", xmlExtraido);
            Assert.Contains("<factura", xmlExtraido);
            Assert.Contains("<infoTributaria>", xmlExtraido);
        }

        [Fact(Skip = "Requiere certificado real y clave configurada")]
        public async Task Test09_FirmarMultiplesDocumentos_DebeFuncionar()
        {
            // Arrange
            var facturas = new[]
            {
                CrearXmlFacturaEjemplo("001-001-000000001"),
                CrearXmlFacturaEjemplo("001-001-000000002"),
                CrearXmlFacturaEjemplo("001-001-000000003")
            };

            // Act
            var firmadasExitosas = 0;
            foreach (var factura in facturas)
            {
                var xmlFirmado = await _firmaService.FirmarXml(factura);
                var esValida = await _firmaService.ValidarFirma(xmlFirmado);
                
                if (esValida)
                {
                    firmadasExitosas++;
                }
            }

            // Assert
            Assert.Equal(3, firmadasExitosas);
            Console.WriteLine($"✅ {firmadasExitosas}/3 facturas firmadas y validadas correctamente");
        }

        [Fact(Skip = "Requiere certificado real y clave configurada")]
        public void Test10_ValidarCertificadoParaSRI_DebeSerCompatible()
        {
            // Arrange
            var certificado = _certificadoService.CargarCertificado();

            // Act
            var (esValido, errores, advertencias) = 
                CertificadoDiagnosticTool.ValidarCertificadoParaSRI(certificado);

            // Assert
            Assert.True(esValido, $"Certificado no válido para SRI. Errores: {string.Join(", ", errores)}");
            
            // Log resultados
            Console.WriteLine("═══════════════════════════════════════");
            Console.WriteLine("VALIDACIÓN PARA SRI");
            Console.WriteLine("═══════════════════════════════════════");
            Console.WriteLine($"Es válido: {esValido}");
            
            if (errores.Any())
            {
                Console.WriteLine("\n❌ ERRORES:");
                foreach (var error in errores)
                {
                    Console.WriteLine($"  {error}");
                }
            }
            
            if (advertencias.Any())
            {
                Console.WriteLine("\n⚠️  ADVERTENCIAS:");
                foreach (var advertencia in advertencias)
                {
                    Console.WriteLine($"  {advertencia}");
                }
            }
            Console.WriteLine("═══════════════════════════════════════");
        }

        // ============================================================
        // MÉTODOS AUXILIARES
        // ============================================================

        private string CrearXmlFacturaEjemplo(string secuencial = "001-001-000000001")
        {
            var fechaEmision = DateTime.Now;
            
            return $@"<?xml version=""1.0"" encoding=""UTF-8""?>
<factura id=""comprobante"" version=""1.0.0"">
    <infoTributaria>
        <ambiente>1</ambiente>
        <tipoEmision>1</tipoEmision>
        <razonSocial>EMPRESA DE PRUEBAS S.A.</razonSocial>
        <nombreComercial>PRUEBAS</nombreComercial>
        <ruc>1234567890001</ruc>
        <claveAcceso>{GenerarClaveAcceso()}</claveAcceso>
        <codDoc>01</codDoc>
        <estab>001</estab>
        <ptoEmi>001</ptoEmi>
        <secuencial>{secuencial}</secuencial>
        <dirMatriz>AV. PRINCIPAL 123 Y SECUNDARIA</dirMatriz>
    </infoTributaria>
    <infoFactura>
        <fechaEmision>{fechaEmision:dd/MM/yyyy}</fechaEmision>
        <dirEstablecimiento>AV. PRINCIPAL 123</dirEstablecimiento>
        <obligadoContabilidad>SI</obligadoContabilidad>
        <tipoIdentificacionComprador>05</tipoIdentificacionComprador>
        <razonSocialComprador>CONSUMIDOR FINAL</razonSocialComprador>
        <identificacionComprador>9999999999999</identificacionComprador>
        <totalSinImpuestos>100.00</totalSinImpuestos>
        <totalDescuento>0.00</totalDescuento>
        <totalConImpuestos>
            <totalImpuesto>
                <codigo>2</codigo>
                <codigoPorcentaje>2</codigoPorcentaje>
                <baseImponible>100.00</baseImponible>
                <valor>15.00</valor>
            </totalImpuesto>
        </totalConImpuestos>
        <propina>0.00</propina>
        <importeTotal>115.00</importeTotal>
        <moneda>DOLAR</moneda>
    </infoFactura>
    <detalles>
        <detalle>
            <codigoPrincipal>PROD001</codigoPrincipal>
            <descripcion>PRODUCTO DE PRUEBA</descripcion>
            <cantidad>1.00</cantidad>
            <precioUnitario>100.00</precioUnitario>
            <descuento>0.00</descuento>
            <precioTotalSinImpuesto>100.00</precioTotalSinImpuesto>
            <impuestos>
                <impuesto>
                    <codigo>2</codigo>
                    <codigoPorcentaje>2</codigoPorcentaje>
                    <tarifa>15</tarifa>
                    <baseImponible>100.00</baseImponible>
                    <valor>15.00</valor>
                </impuesto>
            </impuestos>
        </detalle>
    </detalles>
    <infoAdicional>
        <campoAdicional nombre=""Email"">test@test.com</campoAdicional>
        <campoAdicional nombre=""Telefono"">0999999999</campoAdicional>
    </infoAdicional>
</factura>";
        }

        private string GenerarClaveAcceso()
        {
            // Generar una clave de acceso de prueba (49 dígitos)
            var fecha = DateTime.Now.ToString("ddMMyyyy");
            var random = new Random();
            var digitos = new string(Enumerable.Range(0, 41).Select(_ => random.Next(0, 10).ToString()[0]).ToArray());
            return fecha + digitos;
        }

        public void Dispose()
        {
            // Cleanup si es necesario
        }
    }
}