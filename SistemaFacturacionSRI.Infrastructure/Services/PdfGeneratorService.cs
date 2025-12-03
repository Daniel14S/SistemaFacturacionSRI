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
                                
                                // Contribuyente Especial (obligatorio si aplica según Ficha Técnica RIDE V2.32)
                                if (!string.IsNullOrEmpty(configuracion.ContribuyenteEspecial))
                                {
                                    empresaColumn.Item().Text($"Contribuyente Especial Nro. {configuracion.ContribuyenteEspecial}").FontSize(9);
                                }
                                
                                // Agente de Retención (obligatorio desde XSD V2.1.0)
                                if (!string.IsNullOrEmpty(configuracion.AgenteRetencion))
                                {
                                    empresaColumn.Item().Text($"Agente de Retención Resolución N° {configuracion.AgenteRetencion}").FontSize(9);
                                }
                                
                                // Régimen RIMPE (obligatorio si aplica según normativa SRI)
                                if (!string.IsNullOrEmpty(configuracion.RegimenRimpe))
                                {
                                    empresaColumn.Item().Text(configuracion.RegimenRimpe).FontSize(9).Bold();
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

                            // Sección: Información Tributaria
                            column.Item()
                                .Border(1)
                                .BorderColor(Colors.Grey.Lighten1)
                                .Padding(10)
                                .Column(tributariaColumn =>
                                {
                                    tributariaColumn.Item()
                                        .Background(Colors.Grey.Lighten3)
                                        .Padding(5)
                                        .Text("INFORMACIÓN TRIBUTARIA")
                                        .Bold()
                                        .FontSize(11);

                                    tributariaColumn.Item().PaddingTop(8).Row(row =>
                                    {
                                        row.RelativeItem().Column(col =>
                                        {
                                            col.Item().Row(r =>
                                            {
                                                r.ConstantItem(120).Text("Ambiente:").Bold().FontSize(9);
                                                r.RelativeItem().Text(factura.Ambiente == Ambiente.PRUEBAS ? "PRUEBAS" : "PRODUCCIÓN").FontSize(9);
                                            });
                                            col.Item().PaddingTop(3).Row(r =>
                                            {
                                                r.ConstantItem(120).Text("Tipo Emisión:").Bold().FontSize(9);
                                                r.RelativeItem().Text(factura.TipoEmision == TipoEmision.NORMAL ? "NORMAL" : "CONTINGENCIA").FontSize(9);
                                            });
                                            col.Item().PaddingTop(3).Row(r =>
                                            {
                                                r.ConstantItem(120).Text("Fecha Emisión:").Bold().FontSize(9);
                                                r.RelativeItem().Text($"{factura.FechaEmision:dd/MM/yyyy HH:mm:ss}").FontSize(9);
                                            });
                                        });
                                    });

                                    tributariaColumn.Item().PaddingTop(8).Row(row =>
                                    {
                                        row.ConstantItem(120).Text("Clave de Acceso:").Bold().FontSize(9);
                                        row.RelativeItem().Text(factura.ClaveAcceso).FontSize(8).FontFamily("Courier New");
                                    });

                                    tributariaColumn.Item().PaddingTop(5).Row(row =>
                                    {
                                        row.ConstantItem(120).Text("N° Autorización:").Bold().FontSize(9);
                                        row.RelativeItem().Text(factura.NumeroAutorizacion ?? "PENDIENTE DE AUTORIZACIÓN").FontSize(8).FontFamily("Courier New");
                                    });

                                    if (factura.FechaHoraAutorizacion.HasValue)
                                    {
                                        tributariaColumn.Item().PaddingTop(5).Row(row =>
                                        {
                                            row.ConstantItem(120).Text("Fecha Autorización:").Bold().FontSize(9);
                                            row.RelativeItem().Text($"{factura.FechaHoraAutorizacion:dd/MM/yyyy HH:mm:ss}").FontSize(9);
                                        });
                                    }

                                    tributariaColumn.Item().PaddingTop(5).Row(row =>
                                    {
                                        row.ConstantItem(120).Text("Estado:").Bold().FontSize(9);
                                        row.RelativeItem().Text(factura.Estado.ToString().Replace("_", " "))
                                            .FontSize(9)
                                            .FontColor(factura.Estado == EstadoFactura.AUTORIZADA ? Colors.Green.Darken2 : 
                                                      factura.Estado == EstadoFactura.NO_AUTORIZADA || factura.Estado == EstadoFactura.DEVUELTA ? Colors.Red.Darken2 : 
                                                      Colors.Orange.Darken2);
                                    });
                                });

                            // Sección: Información del Cliente (T-096)
                            column.Item().PaddingTop(10)
                                .Border(1)
                                .BorderColor(Colors.Grey.Lighten1)
                                .Padding(10)
                                .Column(clienteColumn =>
                                {
                                    clienteColumn.Item()
                                        .Background(Colors.Grey.Lighten3)
                                        .Padding(5)
                                        .Text("INFORMACIÓN DEL CLIENTE / RECEPTOR")
                                        .Bold()
                                        .FontSize(11);

                                    // Fila 1: Razón Social y Fecha de Emisión
                                    clienteColumn.Item().PaddingTop(8).Row(row =>
                                    {
                                        row.RelativeItem().Row(innerRow =>
                                        {
                                            innerRow.ConstantItem(150).Text("Razón Social / Nombres:").Bold().FontSize(9);
                                            innerRow.RelativeItem().Text(factura.Cliente?.NombreCompleto() ?? "CONSUMIDOR FINAL").FontSize(9);
                                        });
                                        row.ConstantItem(200).Row(innerRow =>
                                        {
                                            innerRow.ConstantItem(100).Text("Fecha Emisión:").Bold().FontSize(9);
                                            innerRow.RelativeItem().Text($"{factura.FechaEmision:dd/MM/yyyy}").FontSize(9);
                                        });
                                    });

                                    // Fila 2: Identificación y Tipo
                                    clienteColumn.Item().PaddingTop(5).Row(row =>
                                    {
                                        row.RelativeItem().Row(innerRow =>
                                        {
                                            innerRow.ConstantItem(150).Text("Identificación:").Bold().FontSize(9);
                                            var tipoId = factura.Cliente?.TipoIdentificacion?.Nombre ?? "RUC/CÉDULA";
                                            innerRow.RelativeItem().Text($"{factura.Cliente?.Identificacion ?? "9999999999999"} ({tipoId})").FontSize(9);
                                        });
                                        row.ConstantItem(200).Row(innerRow =>
                                        {
                                            innerRow.ConstantItem(100).Text("Guía Remisión:").Bold().FontSize(9);
                                            innerRow.RelativeItem().Text(factura.GuiaRemision ?? "-").FontSize(9);
                                        });
                                    });

                                    // Fila 3: Dirección
                                    clienteColumn.Item().PaddingTop(5).Row(row =>
                                    {
                                        row.ConstantItem(150).Text("Dirección:").Bold().FontSize(9);
                                        row.RelativeItem().Text(factura.Cliente?.Direccion ?? "S/N").FontSize(9);
                                    });

                                    // Fila 4: Teléfono y Email (en la misma línea)
                                    clienteColumn.Item().PaddingTop(5).Row(row =>
                                    {
                                        row.RelativeItem().Row(innerRow =>
                                        {
                                            innerRow.ConstantItem(150).Text("Teléfono:").Bold().FontSize(9);
                                            innerRow.RelativeItem().Text(factura.Cliente?.Telefono ?? "-").FontSize(9);
                                        });
                                        row.ConstantItem(200).Row(innerRow =>
                                        {
                                            innerRow.ConstantItem(100).Text("Email:").Bold().FontSize(9);
                                            innerRow.RelativeItem().Text(factura.Cliente?.Email ?? "-").FontSize(9);
                                        });
                                    });
                                });

                            // Sección: Detalle de Productos/Servicios (T-097)
                            column.Item().PaddingTop(10)
                                .Border(1)
                                .BorderColor(Colors.Grey.Lighten1)
                                .Column(detalleColumn =>
                                {
                                    detalleColumn.Item()
                                        .Background(Colors.Grey.Lighten3)
                                        .Padding(5)
                                        .Text("DETALLE DE PRODUCTOS / SERVICIOS")
                                        .Bold()
                                        .FontSize(11);

                                    // Tabla de detalles completa según Ficha Técnica RIDE
                                    detalleColumn.Item().Padding(5).Table(table =>
                                    {
                                        table.ColumnsDefinition(columns =>
                                        {
                                            columns.ConstantColumn(60);   // Código
                                            columns.RelativeColumn(3);    // Descripción
                                            columns.ConstantColumn(45);   // Cantidad
                                            columns.ConstantColumn(60);   // P. Unitario
                                            columns.ConstantColumn(55);   // Descuento
                                            columns.ConstantColumn(65);   // Subtotal
                                            columns.ConstantColumn(40);   // IVA %
                                            columns.ConstantColumn(65);   // Total
                                        });

                                        // Encabezado de la tabla
                                        table.Header(header =>
                                        {
                                            header.Cell().Element(HeaderCellStyle).AlignCenter().Text("Código").Bold().FontSize(8);
                                            header.Cell().Element(HeaderCellStyle).Text("Descripción").Bold().FontSize(8);
                                            header.Cell().Element(HeaderCellStyle).AlignCenter().Text("Cant.").Bold().FontSize(8);
                                            header.Cell().Element(HeaderCellStyle).AlignRight().Text("P. Unit.").Bold().FontSize(8);
                                            header.Cell().Element(HeaderCellStyle).AlignRight().Text("Desc.").Bold().FontSize(8);
                                            header.Cell().Element(HeaderCellStyle).AlignRight().Text("Subtotal").Bold().FontSize(8);
                                            header.Cell().Element(HeaderCellStyle).AlignCenter().Text("IVA").Bold().FontSize(8);
                                            header.Cell().Element(HeaderCellStyle).AlignRight().Text("Total").Bold().FontSize(8);

                                            static IContainer HeaderCellStyle(IContainer container)
                                            {
                                                return container
                                                    .Background(Colors.Blue.Lighten4)
                                                    .BorderBottom(1)
                                                    .BorderColor(Colors.Blue.Medium)
                                                    .PaddingVertical(5)
                                                    .PaddingHorizontal(3);
                                            }
                                        });

                                        // Filas de detalle
                                        foreach (var detalle in factura.Detalles)
                                        {
                                            // Código Principal
                                            table.Cell().Element(DataCellStyle).AlignCenter()
                                                .Text(detalle.CodigoPrincipal).FontSize(7);
                                            
                                            // Descripción (con código auxiliar si existe)
                                            var descripcion = detalle.Descripcion;
                                            if (!string.IsNullOrEmpty(detalle.CodigoAuxiliar))
                                            {
                                                descripcion += $" [{detalle.CodigoAuxiliar}]";
                                            }
                                            table.Cell().Element(DataCellStyle)
                                                .Text(descripcion).FontSize(8);
                                            
                                            // Cantidad (hasta 6 decimales según XSD V1.1.0+)
                                            table.Cell().Element(DataCellStyle).AlignCenter()
                                                .Text(detalle.Cantidad.ToString("G")).FontSize(8);
                                            
                                            // Precio Unitario
                                            table.Cell().Element(DataCellStyle).AlignRight()
                                                .Text($"${detalle.PrecioUnitario:F4}").FontSize(8);
                                            
                                            // Descuento
                                            table.Cell().Element(DataCellStyle).AlignRight()
                                                .Text($"${detalle.Descuento:F2}").FontSize(8);
                                            
                                            // Subtotal (Precio sin impuesto)
                                            table.Cell().Element(DataCellStyle).AlignRight()
                                                .Text($"${detalle.PrecioTotalSinImpuesto:F2}").FontSize(8);
                                            
                                            // IVA % (tarifa del detalle)
                                            table.Cell().Element(DataCellStyle).AlignCenter()
                                                .Text($"{detalle.Tarifa:F0}%").FontSize(8);
                                            
                                            // Total (con IVA)
                                            table.Cell().Element(DataCellStyle).AlignRight()
                                                .Text($"${detalle.ValorTotal:F2}").FontSize(8).Bold();

                                            static IContainer DataCellStyle(IContainer container)
                                            {
                                                return container
                                                    .BorderBottom(1)
                                                    .BorderColor(Colors.Grey.Lighten3)
                                                    .PaddingVertical(4)
                                                    .PaddingHorizontal(3);
                                            }
                                        }
                                    });
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
