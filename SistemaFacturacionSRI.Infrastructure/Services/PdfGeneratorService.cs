using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using SistemaFacturacionSRI.Domain.Interfaces;
using SistemaFacturacionSRI.Domain.Interfaces.Repositories;
using SistemaFacturacionSRI.Domain.Enums;
using Microsoft.Extensions.Logging;
using ZXing;
using ZXing.Common;
using ZXing.QrCode;
using ZXing.Windows.Compatibility;
using System.Drawing;
using ImageFormat = System.Drawing.Imaging.ImageFormat;

namespace SistemaFacturacionSRI.Infrastructure.Services;

/// <summary>
/// Implementación del servicio de generación de PDFs y códigos de barras/QR
/// </summary>
public class PdfGeneratorService : IPdfGeneratorService
{
    private readonly IFacturaRepository _facturaRepository;
    private readonly IConfiguracionEmpresaRepository _configuracionRepository;
    private readonly ILogger<PdfGeneratorService> _logger;

    public PdfGeneratorService(
        IFacturaRepository facturaRepository,
        IConfiguracionEmpresaRepository configuracionRepository,
        ILogger<PdfGeneratorService> logger)
    {
        _facturaRepository = facturaRepository;
        _configuracionRepository = configuracionRepository;
        _logger = logger;
        
        // Configurar licencia de QuestPDF (Community License)
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public async Task<string> GenerarRideAsync(int facturaId, string outputPath)
    {
        try
        {
            _logger.LogInformation("Generando RIDE para factura {FacturaId}", facturaId);

            // Obtener la factura con todos sus datos
            var factura = await _facturaRepository.ObtenerConDetallesCompletosAsync(facturaId);
            if (factura == null)
            {
                throw new ArgumentException($"No se encontró la factura con ID {facturaId}");
            }

            // Validar que tenga clave de acceso
            if (string.IsNullOrEmpty(factura.ClaveAcceso))
            {
                throw new InvalidOperationException($"La factura {facturaId} no tiene clave de acceso generada");
            }

            // Obtener configuración de la empresa
            var configuracion = await _configuracionRepository.ObtenerConfiguracionAsync();
            if (configuracion == null)
            {
                throw new InvalidOperationException("No se encontró la configuración de la empresa");
            }

            // Crear directorio si no existe
            var directory = Path.GetDirectoryName(outputPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            // Generar código QR en memoria
            var qrImagePath = Path.Combine(Path.GetTempPath(), $"qr_{factura.ClaveAcceso}.png");
            await GenerarCodigoBarrasAsync(factura.ClaveAcceso, qrImagePath, usarQr: true);

            // Generar el PDF usando QuestPDF
            Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(2, Unit.Centimetre);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(10));

                    page.Header()
                        .BorderBottom(2)
                        .BorderColor(Colors.Grey.Lighten1)
                        .PaddingBottom(10)
                        .Row(row =>
                        {
                            // Columna del logo
                            row.ConstantItem(120).Column(logoColumn =>
                            {
                                if (!string.IsNullOrEmpty(configuracion.LogoPath) && File.Exists(configuracion.LogoPath))
                                {
                                    logoColumn.Item().Height(80).Image(configuracion.LogoPath);
                                }
                                else
                                {
                                    logoColumn.Item().Height(80).Border(1).BorderColor(Colors.Grey.Lighten2)
                                        .AlignCenter().AlignMiddle().Text("LOGO").FontSize(12).FontColor(Colors.Grey.Medium);
                                }
                            });

                            // Espaciador
                            row.RelativeItem().PaddingLeft(15);

                            // Columna de información de la empresa
                            row.RelativeItem(2).Column(empresaColumn =>
                            {
                                empresaColumn.Item().Text(configuracion.RazonSocial).Bold().FontSize(14);
                                empresaColumn.Item().Text(configuracion.NombreComercial).FontSize(11).Italic();
                                empresaColumn.Item().PaddingTop(5).Text($"RUC: {configuracion.RUC}").FontSize(10);
                                empresaColumn.Item().Text($"Dir. Matriz: {configuracion.DirMatriz}").FontSize(9);
                                empresaColumn.Item().Text($"Dir. Sucursal: {configuracion.DirEstablecimiento}").FontSize(9);
                                empresaColumn.Item().PaddingTop(3).Text($"Establecimiento: {configuracion.CodigoEstablecimiento} - Pto. Emisión: {configuracion.PuntoEmision}").FontSize(9);
                                empresaColumn.Item().Text($"Obligado a llevar contabilidad: {(configuracion.ObligadoContabilidad ? "SÍ" : "NO")}").FontSize(9);
                                
                                if (!string.IsNullOrEmpty(configuracion.AgenteRetencion))
                                {
                                    empresaColumn.Item().Text($"Agente de Retención: Resolución N° {configuracion.AgenteRetencion}").FontSize(9);
                                }
                            });

                            // Espaciador
                            row.RelativeItem().PaddingLeft(15);

                            // Columna de información de la factura
                            row.RelativeItem(1).Column(facturaInfoColumn =>
                            {
                                facturaInfoColumn.Item()
                                    .Border(2)
                                    .BorderColor(Colors.Blue.Medium)
                                    .Padding(8)
                                    .Column(infoBox =>
                                    {
                                        infoBox.Item().AlignCenter().Text("FACTURA").Bold().FontSize(14).FontColor(Colors.Blue.Darken2);
                                        infoBox.Item().AlignCenter().Text($"N° {factura.NumeroFactura}").Bold().FontSize(12);
                                        infoBox.Item().PaddingTop(5).Text($"Número de Autorización:").FontSize(7).Bold();
                                        infoBox.Item().Text(factura.NumeroAutorizacion ?? "PENDIENTE").FontSize(7);
                                        infoBox.Item().PaddingTop(3).Text($"Fecha Autorización:").FontSize(7).Bold();
                                        infoBox.Item().Text(factura.FechaHoraAutorizacion?.ToString("dd/MM/yyyy HH:mm:ss") ?? "PENDIENTE").FontSize(7);
                                        infoBox.Item().PaddingTop(3).Text($"Ambiente: {(factura.Ambiente == Ambiente.PRUEBAS ? "PRUEBAS" : "PRODUCCIÓN")}").FontSize(7);
                                        infoBox.Item().Text($"Emisión: {(factura.TipoEmision == TipoEmision.NORMAL ? "NORMAL" : "CONTINGENCIA")}").FontSize(7);
                                    });
                            });
                        });

                    page.Content()
                        .PaddingVertical(1, Unit.Centimetre)
                        .Column(column =>
                        {
                            column.Spacing(10);

                            // Información de la empresa
                            column.Item().Text("INFORMACIÓN DEL EMISOR").Bold();
                            column.Item().Text($"Razón Social: {factura.Cliente?.NombreCompleto() ?? "N/A"}");
                            column.Item().Text($"RUC: {factura.Cliente?.Identificacion ?? "N/A"}");

                            // Información de la factura
                            column.Item().PaddingTop(10).Text("DATOS DE LA FACTURA").Bold();
                            column.Item().Text($"Fecha Emisión: {factura.FechaEmision:dd/MM/yyyy}");
                            column.Item().Text($"Clave de Acceso: {factura.ClaveAcceso}");
                            column.Item().Text($"Ambiente: {factura.Ambiente}");
                            column.Item().Text($"Estado: {factura.Estado}");

                            // Detalle de la factura
                            column.Item().PaddingTop(10).Text("DETALLE DE PRODUCTOS/SERVICIOS").Bold();
                            
                            // Tabla de detalles
                            column.Item().Table(table =>
                            {
                                table.ColumnsDefinition(columns =>
                                {
                                    columns.ConstantColumn(50);  // Cant
                                    columns.RelativeColumn(3);    // Descripción
                                    columns.RelativeColumn(1);    // P.Unit
                                    columns.RelativeColumn(1);    // Total
                                });

                                // Encabezado
                                table.Header(header =>
                                {
                                    header.Cell().Element(CellStyle).Text("Cant").Bold();
                                    header.Cell().Element(CellStyle).Text("Descripción").Bold();
                                    header.Cell().Element(CellStyle).Text("P.Unit").Bold();
                                    header.Cell().Element(CellStyle).Text("Total").Bold();

                                    static IContainer CellStyle(IContainer container)
                                    {
                                        return container.BorderBottom(1).BorderColor(Colors.Grey.Lighten2).PaddingVertical(5);
                                    }
                                });

                                // Filas de detalle
                                foreach (var detalle in factura.Detalles)
                                {
                                    table.Cell().Element(CellStyle).Text(detalle.Cantidad.ToString());
                                    table.Cell().Element(CellStyle).Text(detalle.Producto?.Nombre ?? "N/A");
                                    table.Cell().Element(CellStyle).Text($"${detalle.PrecioUnitario:F2}");
                                    table.Cell().Element(CellStyle).Text($"${detalle.ValorTotal:F2}");

                                    static IContainer CellStyle(IContainer container)
                                    {
                                        return container.BorderBottom(1).BorderColor(Colors.Grey.Lighten3).PaddingVertical(5);
                                    }
                                }
                            });

                            // Totales
                            column.Item().PaddingTop(10).AlignRight().Column(totalColumn =>
                            {
                                totalColumn.Item().Text($"Subtotal 0%: ${factura.Subtotal0:F2}");
                                totalColumn.Item().Text($"Subtotal 15%: ${factura.Subtotal15:F2}");
                                totalColumn.Item().Text($"IVA 15%: ${factura.IVA15:F2}");
                                totalColumn.Item().Text($"TOTAL: ${factura.ImporteTotal:F2}").Bold().FontSize(12);
                            });

                            // Código QR
                            if (File.Exists(qrImagePath))
                            {
                                column.Item().PaddingTop(20).AlignCenter().Width(150).Image(qrImagePath);
                            }
                        });

                    page.Footer()
                        .AlignCenter()
                        .Text(x =>
                        {
                            x.Span("Documento Electrónico - ");
                            x.Span($"Generado el {DateTime.Now:dd/MM/yyyy HH:mm:ss}");
                        });
                });
            })
            .GeneratePdf(outputPath);

            // Limpiar archivo temporal del QR
            if (File.Exists(qrImagePath))
            {
                try
                {
                    File.Delete(qrImagePath);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "No se pudo eliminar archivo temporal de QR: {Path}", qrImagePath);
                }
            }

            _logger.LogInformation("RIDE generado exitosamente en: {OutputPath}", outputPath);
            return outputPath;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al generar RIDE para factura {FacturaId}", facturaId);
            throw;
        }
    }

    public async Task<string> GenerarCodigoBarrasAsync(string claveAcceso, string outputPath, bool usarQr = true)
    {
        try
        {
            _logger.LogInformation("Generando código {Tipo} para clave de acceso: {ClaveAcceso}", 
                usarQr ? "QR" : "de barras", claveAcceso);

            // Validar clave de acceso (debe tener 49 dígitos)
            if (string.IsNullOrEmpty(claveAcceso) || claveAcceso.Length != 49)
            {
                throw new ArgumentException("La clave de acceso debe tener 49 dígitos", nameof(claveAcceso));
            }

            // Crear directorio si no existe
            var directory = Path.GetDirectoryName(outputPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            // Configurar el writer según el tipo de código
            BarcodeWriter<Bitmap> writer;
            
            if (usarQr)
            {
                writer = new BarcodeWriter<Bitmap>
                {
                    Format = BarcodeFormat.QR_CODE,
                    Options = new QrCodeEncodingOptions
                    {
                        Height = 300,
                        Width = 300,
                        Margin = 1,
                        CharacterSet = "UTF-8"
                    },
                    Renderer = new BitmapRenderer()
                };
            }
            else
            {
                writer = new BarcodeWriter<Bitmap>
                {
                    Format = BarcodeFormat.CODE_128,
                    Options = new EncodingOptions
                    {
                        Height = 100,
                        Width = 400,
                        Margin = 10,
                        PureBarcode = false
                    },
                    Renderer = new BitmapRenderer()
                };
            }

            // Generar el código
            using (var bitmap = writer.Write(claveAcceso))
            {
                // Guardar la imagen
                bitmap.Save(outputPath, ImageFormat.Png);
            }

            _logger.LogInformation("Código {Tipo} generado exitosamente en: {OutputPath}", 
                usarQr ? "QR" : "de barras", outputPath);

            return await Task.FromResult(outputPath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al generar código {Tipo}", usarQr ? "QR" : "de barras");
            throw;
        }
    }
}
