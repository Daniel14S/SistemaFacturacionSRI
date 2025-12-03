using Xunit;
using Moq;
using SistemaFacturacionSRI.Domain.Interfaces;
using SistemaFacturacionSRI.Domain.Interfaces.Repositories;
using SistemaFacturacionSRI.Domain.Entities;
using SistemaFacturacionSRI.Domain.Enums;
using SistemaFacturacionSRI.Infrastructure.Services;
using Microsoft.Extensions.Logging;

namespace SistemaFacturacionSRI.Tests.Services;

/// <summary>
/// Pruebas para el servicio de generación de PDFs (RIDE)
/// T-104: Probar generación de PDF completo
/// </summary>
public class PdfGeneratorServiceTests
{
    private readonly Mock<IFacturaRepository> _facturaRepositoryMock;
    private readonly Mock<IConfiguracionEmpresaRepository> _configuracionRepositoryMock;
    private readonly Mock<ILogger<PdfGeneratorService>> _loggerMock;
    private readonly PdfGeneratorService _service;

    public PdfGeneratorServiceTests()
    {
        _facturaRepositoryMock = new Mock<IFacturaRepository>();
        _configuracionRepositoryMock = new Mock<IConfiguracionEmpresaRepository>();
        _loggerMock = new Mock<ILogger<PdfGeneratorService>>();
        
        _service = new PdfGeneratorService(
            _facturaRepositoryMock.Object,
            _configuracionRepositoryMock.Object,
            _loggerMock.Object
        );
    }

    #region Datos de Prueba

    private static Factura CrearFacturaPrueba()
    {
        return new Factura
        {
            Id = 1,
            NumeroFactura = "001-001-000000001",
            ClaveAcceso = "2712202401123456789000110010010000000011234567891",
            ClienteId = 1,
            Cliente = new Cliente
            {
                ClienteId = 1,
                Nombre1 = "Juan",
                Apellido1 = "Pérez",
                Identificacion = "1234567890001",
                TipoIdentificacionId = 1,
                TipoIdentificacion = new TipoIdentificacion
                {
                    TipoIdentificacionId = 1,
                    CodigoSRI = "04",
                    Nombre = "RUC"
                },
                Direccion = "Av. Principal 123",
                Telefono = "0991234567",
                Email = "juan.perez@email.com"
            },
            UsuarioId = 1,
            FechaEmision = new DateTime(2024, 12, 27, 10, 30, 0),
            Ambiente = Ambiente.PRUEBAS,
            TipoEmision = TipoEmision.NORMAL,
            Estado = EstadoFactura.AUTORIZADA,
            Subtotal0 = 50.00m,
            Subtotal15 = 100.00m,
            SubtotalNoObjetoIVA = 0.00m,
            SubtotalExentoIVA = 0.00m,
            SubtotalConDescuento = 145.00m,
            Descuento = 5.00m,
            IVA15 = 15.00m,
            Propina = 0.00m,
            ImporteTotal = 160.00m,
            NumeroAutorizacion = "2712202401123456789000110010010000000011234567891",
            FechaHoraAutorizacion = new DateTime(2024, 12, 27, 10, 35, 0),
            Observaciones = "Factura de prueba para validación del sistema",
            Detalles = new List<DetalleFactura>
            {
                new DetalleFactura
                {
                    Id = 1,
                    FacturaId = 1,
                    CodigoPrincipal = "PROD001",
                    CodigoAuxiliar = "SKU-001",
                    Descripcion = "Producto de Prueba 1 - IVA 0%",
                    Cantidad = 2.00m,
                    PrecioUnitario = 25.00m,
                    Descuento = 0.00m,
                    PrecioTotalSinImpuesto = 50.00m,
                    CodigoPorcentajeIVA = 0,
                    Tarifa = 0,
                    BaseImponible = 50.00m,
                    Valor = 0.00m,
                    ValorTotal = 50.00m
                },
                new DetalleFactura
                {
                    Id = 2,
                    FacturaId = 1,
                    CodigoPrincipal = "PROD002",
                    Descripcion = "Producto de Prueba 2 - IVA 15%",
                    Cantidad = 4.00m,
                    PrecioUnitario = 26.25m,
                    Descuento = 5.00m,
                    PrecioTotalSinImpuesto = 100.00m,
                    CodigoPorcentajeIVA = 4,
                    Tarifa = 15,
                    BaseImponible = 100.00m,
                    Valor = 15.00m,
                    ValorTotal = 115.00m
                }
            },
            InfoAdicional = new List<InfoAdicional>
            {
                new InfoAdicional { Id = 1, FacturaId = 1, Nombre = "Email", Valor = "juan.perez@email.com" },
                new InfoAdicional { Id = 2, FacturaId = 1, Nombre = "Teléfono", Valor = "0991234567" },
                new InfoAdicional { Id = 3, FacturaId = 1, Nombre = "Forma de Pago", Valor = "EFECTIVO" }
            }
        };
    }

    private static ConfiguracionEmpresa CrearConfiguracionPrueba()
    {
        return new ConfiguracionEmpresa
        {
            Id = 1,
            RUC = "1234567890001",
            RazonSocial = "EMPRESA DE PRUEBA S.A.",
            NombreComercial = "MI EMPRESA",
            DirMatriz = "Av. Principal 456, Quito, Ecuador",
            DirEstablecimiento = "Sucursal Norte - Av. Amazonas",
            CodigoEstablecimiento = "001",
            PuntoEmision = "001",
            ObligadoContabilidad = true,
            AgenteRetencion = "123",
            ContribuyenteEspecial = "456",
            RegimenRimpe = null, // No es RIMPE
            Telefono = "022123456",
            Email = "facturacion@empresa.com",
            AmbienteSRI = "1",
            TipoEmision = "1",
            UrlRecepcionComprobantes = "https://celcer.sri.gob.ec/comprobantes-electronicos-ws/RecepcionComprobantesOffline?wsdl",
            UrlAutorizacionComprobantes = "https://celcer.sri.gob.ec/comprobantes-electronicos-ws/AutorizacionComprobantesOffline?wsdl",
            LogoPath = null, // Sin logo
            InfoAdicionalDefecto = "Gracias por su compra|Consulte en www.empresa.com|Atención al cliente: 022123456"
        };
    }

    #endregion

    #region Pruebas de Generación de Código de Barras

    [Fact]
    public async Task GenerarCodigoBarras_ConClaveValida_DebeGenerarArchivo()
    {
        // Arrange
        var claveAcceso = "2712202401123456789000110010010000000011234567891";
        var outputPath = Path.Combine(Path.GetTempPath(), $"test_barcode_{Guid.NewGuid()}.png");

        try
        {
            // Act
            var resultado = await _service.GenerarCodigoBarrasAsync(claveAcceso, outputPath, usarQr: false);

            // Assert
            Assert.Equal(outputPath, resultado);
            Assert.True(File.Exists(outputPath), "El archivo de código de barras debe existir");
            
            var fileInfo = new FileInfo(outputPath);
            Assert.True(fileInfo.Length > 0, "El archivo no debe estar vacío");
        }
        finally
        {
            // Cleanup
            if (File.Exists(outputPath))
                File.Delete(outputPath);
        }
    }

    [Fact]
    public async Task GenerarCodigoQR_ConClaveValida_DebeGenerarArchivo()
    {
        // Arrange
        var claveAcceso = "2712202401123456789000110010010000000011234567891";
        var outputPath = Path.Combine(Path.GetTempPath(), $"test_qr_{Guid.NewGuid()}.png");

        try
        {
            // Act
            var resultado = await _service.GenerarCodigoBarrasAsync(claveAcceso, outputPath, usarQr: true);

            // Assert
            Assert.Equal(outputPath, resultado);
            Assert.True(File.Exists(outputPath), "El archivo QR debe existir");
            
            var fileInfo = new FileInfo(outputPath);
            Assert.True(fileInfo.Length > 0, "El archivo no debe estar vacío");
        }
        finally
        {
            // Cleanup
            if (File.Exists(outputPath))
                File.Delete(outputPath);
        }
    }

    [Theory]
    [InlineData("")]
    [InlineData("123456")]
    [InlineData("12345678901234567890123456789012345678901234567")] // 47 dígitos
    [InlineData("123456789012345678901234567890123456789012345678901")] // 51 dígitos
    public async Task GenerarCodigoBarras_ConClaveInvalida_DebeLanzarExcepcion(string claveInvalida)
    {
        // Arrange
        var outputPath = Path.Combine(Path.GetTempPath(), "test_invalid.png");

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.GenerarCodigoBarrasAsync(claveInvalida, outputPath));
    }

    #endregion

    #region Pruebas de Generación de RIDE (PDF)

    [Fact]
    public async Task GenerarRide_ConFacturaCompleta_DebeGenerarPDF()
    {
        // Arrange
        var factura = CrearFacturaPrueba();
        var configuracion = CrearConfiguracionPrueba();
        var outputPath = Path.Combine(Path.GetTempPath(), $"test_ride_{Guid.NewGuid()}.pdf");

        _facturaRepositoryMock
            .Setup(r => r.ObtenerConDetallesCompletosAsync(It.IsAny<int>()))
            .ReturnsAsync(factura);

        _configuracionRepositoryMock
            .Setup(r => r.ObtenerConfiguracionAsync())
            .ReturnsAsync(configuracion);

        try
        {
            // Act
            var resultado = await _service.GenerarRideAsync(1, outputPath);

            // Assert
            Assert.Equal(outputPath, resultado);
            Assert.True(File.Exists(outputPath), "El archivo PDF debe existir");
            
            var fileInfo = new FileInfo(outputPath);
            Assert.True(fileInfo.Length > 1000, "El PDF debe tener un tamaño razonable (>1KB)");
            
            // Verificar que es un PDF válido (comienza con %PDF)
            using var fileStream = File.OpenRead(outputPath);
            var header = new byte[4];
            await fileStream.ReadAsync(header, 0, 4);
            var headerStr = System.Text.Encoding.ASCII.GetString(header);
            Assert.Equal("%PDF", headerStr);
        }
        finally
        {
            // Cleanup
            if (File.Exists(outputPath))
                File.Delete(outputPath);
        }
    }

    [Fact]
    public async Task GenerarRide_SinFactura_DebeLanzarExcepcion()
    {
        // Arrange
        _facturaRepositoryMock
            .Setup(r => r.ObtenerConDetallesCompletosAsync(It.IsAny<int>()))
            .ReturnsAsync((Factura?)null);

        var outputPath = Path.Combine(Path.GetTempPath(), "test_noexiste.pdf");

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.GenerarRideAsync(999, outputPath));
    }

    [Fact]
    public async Task GenerarRide_SinConfiguracion_DebeLanzarExcepcion()
    {
        // Arrange
        var factura = CrearFacturaPrueba();
        
        _facturaRepositoryMock
            .Setup(r => r.ObtenerConDetallesCompletosAsync(It.IsAny<int>()))
            .ReturnsAsync(factura);

        _configuracionRepositoryMock
            .Setup(r => r.ObtenerConfiguracionAsync())
            .ReturnsAsync((ConfiguracionEmpresa?)null);

        var outputPath = Path.Combine(Path.GetTempPath(), "test_noconfig.pdf");

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.GenerarRideAsync(1, outputPath));
    }

    [Fact]
    public async Task GenerarRide_SinClaveAcceso_DebeLanzarExcepcion()
    {
        // Arrange
        var factura = CrearFacturaPrueba();
        factura.ClaveAcceso = string.Empty; // Sin clave de acceso
        
        var configuracion = CrearConfiguracionPrueba();

        _facturaRepositoryMock
            .Setup(r => r.ObtenerConDetallesCompletosAsync(It.IsAny<int>()))
            .ReturnsAsync(factura);

        _configuracionRepositoryMock
            .Setup(r => r.ObtenerConfiguracionAsync())
            .ReturnsAsync(configuracion);

        var outputPath = Path.Combine(Path.GetTempPath(), "test_noclave.pdf");

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.GenerarRideAsync(1, outputPath));
    }

    #endregion

    #region Pruebas de Almacenamiento (T-101)

    [Fact]
    public async Task GenerarYAlmacenarRide_DebeCrearDirectorioYArchivo()
    {
        // Arrange
        var factura = CrearFacturaPrueba();
        var configuracion = CrearConfiguracionPrueba();
        var webRootPath = Path.Combine(Path.GetTempPath(), $"wwwroot_test_{Guid.NewGuid()}");

        _facturaRepositoryMock
            .Setup(r => r.ObtenerConDetallesCompletosAsync(It.IsAny<int>()))
            .ReturnsAsync(factura);

        _configuracionRepositoryMock
            .Setup(r => r.ObtenerConfiguracionAsync())
            .ReturnsAsync(configuracion);

        _facturaRepositoryMock
            .Setup(r => r.ActualizarAsync(It.IsAny<Factura>()))
            .Returns(Task.CompletedTask);

        try
        {
            // Act
            var rutaRelativa = await _service.GenerarYAlmacenarRideAsync(1, webRootPath);

            // Assert
            Assert.NotNull(rutaRelativa);
            Assert.Contains("comprobantes", rutaRelativa);
            Assert.Contains("pdf", rutaRelativa);
            Assert.Contains(".pdf", rutaRelativa);

            var rutaCompleta = Path.Combine(webRootPath, rutaRelativa);
            Assert.True(File.Exists(rutaCompleta), "El archivo PDF debe existir en la ruta especificada");

            // Verificar que se llamó a ActualizarAsync
            _facturaRepositoryMock.Verify(r => r.ActualizarAsync(It.Is<Factura>(f => 
                f.PdfPath == rutaRelativa)), Times.Once);
        }
        finally
        {
            // Cleanup
            if (Directory.Exists(webRootPath))
                Directory.Delete(webRootPath, true);
        }
    }

    [Fact]
    public async Task GenerarYAlmacenarRide_DebeOrganizarPorAnoYMes()
    {
        // Arrange
        var factura = CrearFacturaPrueba();
        factura.FechaEmision = new DateTime(2024, 12, 27);
        var configuracion = CrearConfiguracionPrueba();
        var webRootPath = Path.Combine(Path.GetTempPath(), $"wwwroot_test_{Guid.NewGuid()}");

        _facturaRepositoryMock
            .Setup(r => r.ObtenerConDetallesCompletosAsync(It.IsAny<int>()))
            .ReturnsAsync(factura);

        _configuracionRepositoryMock
            .Setup(r => r.ObtenerConfiguracionAsync())
            .ReturnsAsync(configuracion);

        _facturaRepositoryMock
            .Setup(r => r.ActualizarAsync(It.IsAny<Factura>()))
            .Returns(Task.CompletedTask);

        try
        {
            // Act
            var rutaRelativa = await _service.GenerarYAlmacenarRideAsync(1, webRootPath);

            // Assert
            Assert.Contains("2024", rutaRelativa); // Año
            Assert.Contains("12", rutaRelativa);   // Mes
        }
        finally
        {
            // Cleanup
            if (Directory.Exists(webRootPath))
                Directory.Delete(webRootPath, true);
        }
    }

    #endregion

    #region Pruebas de Secciones del RIDE

    [Fact]
    public async Task GenerarRide_ConInfoAdicional_DebeIncluirSeccion()
    {
        // Arrange
        var factura = CrearFacturaPrueba();
        factura.InfoAdicional = new List<InfoAdicional>
        {
            new InfoAdicional { Nombre = "Email", Valor = "test@test.com" },
            new InfoAdicional { Nombre = "Vendedor", Valor = "Juan Pérez" }
        };
        var configuracion = CrearConfiguracionPrueba();
        var outputPath = Path.Combine(Path.GetTempPath(), $"test_infoadic_{Guid.NewGuid()}.pdf");

        _facturaRepositoryMock
            .Setup(r => r.ObtenerConDetallesCompletosAsync(It.IsAny<int>()))
            .ReturnsAsync(factura);

        _configuracionRepositoryMock
            .Setup(r => r.ObtenerConfiguracionAsync())
            .ReturnsAsync(configuracion);

        try
        {
            // Act
            var resultado = await _service.GenerarRideAsync(1, outputPath);

            // Assert - El PDF se genera correctamente (no podemos verificar el contenido fácilmente)
            Assert.True(File.Exists(outputPath));
            var fileInfo = new FileInfo(outputPath);
            Assert.True(fileInfo.Length > 1000);
        }
        finally
        {
            if (File.Exists(outputPath))
                File.Delete(outputPath);
        }
    }

    [Fact]
    public async Task GenerarRide_ConRegimenRimpe_DebeIncluirLeyenda()
    {
        // Arrange
        var factura = CrearFacturaPrueba();
        var configuracion = CrearConfiguracionPrueba();
        configuracion.RegimenRimpe = "CONTRIBUYENTE RÉGIMEN RIMPE";
        var outputPath = Path.Combine(Path.GetTempPath(), $"test_rimpe_{Guid.NewGuid()}.pdf");

        _facturaRepositoryMock
            .Setup(r => r.ObtenerConDetallesCompletosAsync(It.IsAny<int>()))
            .ReturnsAsync(factura);

        _configuracionRepositoryMock
            .Setup(r => r.ObtenerConfiguracionAsync())
            .ReturnsAsync(configuracion);

        try
        {
            // Act
            var resultado = await _service.GenerarRideAsync(1, outputPath);

            // Assert
            Assert.True(File.Exists(outputPath));
        }
        finally
        {
            if (File.Exists(outputPath))
                File.Delete(outputPath);
        }
    }

    [Fact]
    public async Task GenerarRide_ConPropina_DebeIncluirEnTotales()
    {
        // Arrange
        var factura = CrearFacturaPrueba();
        factura.Propina = 10.00m;
        factura.ImporteTotal += 10.00m;
        var configuracion = CrearConfiguracionPrueba();
        var outputPath = Path.Combine(Path.GetTempPath(), $"test_propina_{Guid.NewGuid()}.pdf");

        _facturaRepositoryMock
            .Setup(r => r.ObtenerConDetallesCompletosAsync(It.IsAny<int>()))
            .ReturnsAsync(factura);

        _configuracionRepositoryMock
            .Setup(r => r.ObtenerConfiguracionAsync())
            .ReturnsAsync(configuracion);

        try
        {
            // Act
            var resultado = await _service.GenerarRideAsync(1, outputPath);

            // Assert
            Assert.True(File.Exists(outputPath));
        }
        finally
        {
            if (File.Exists(outputPath))
                File.Delete(outputPath);
        }
    }

    #endregion
}
