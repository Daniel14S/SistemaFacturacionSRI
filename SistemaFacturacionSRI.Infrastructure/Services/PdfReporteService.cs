using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using SistemaFacturacionSRI.Domain.DTOs.Reportes;

namespace SistemaFacturacionSRI.Infrastructure.Services;

public class PdfReporteService
{
    public byte[] GenerarPdfProductosMasVendidos(List<ReporteProductoMasVendidoDto> datos, DateTime? fechaInicio, DateTime? fechaFin)
    {
        var documento = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(2, Unit.Centimetre);
                page.DefaultTextStyle(x => x.FontSize(10));

                page.Header().Row(row =>
                {
                    row.RelativeItem().Column(column =>
                    {
                        column.Item().Text("REPORTE DE PRODUCTOS MÁS VENDIDOS")
                            .FontSize(16)
                            .Bold()
                            .FontColor(Colors.Blue.Darken2);
                        
                        if (fechaInicio.HasValue && fechaFin.HasValue)
                        {
                            column.Item().Text($"Período: {fechaInicio:dd/MM/yyyy} - {fechaFin:dd/MM/yyyy}")
                                .FontSize(10);
                        }
                        
                        column.Item().Text($"Fecha de generación: {DateTime.Now:dd/MM/yyyy HH:mm}")
                            .FontSize(8)
                            .FontColor(Colors.Grey.Darken1);
                    });
                });

                page.Content().PaddingVertical(1, Unit.Centimetre).Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.ConstantColumn(50);
                        columns.RelativeColumn(2);
                        columns.RelativeColumn(4);
                        columns.RelativeColumn(2);
                        columns.RelativeColumn(2);
                        columns.RelativeColumn(2);
                    });

                    table.Header(header =>
                    {
                        header.Cell().Background(Colors.Blue.Darken2).Padding(5).Text("#").FontColor(Colors.White).Bold();
                        header.Cell().Background(Colors.Blue.Darken2).Padding(5).Text("Código").FontColor(Colors.White).Bold();
                        header.Cell().Background(Colors.Blue.Darken2).Padding(5).Text("Producto").FontColor(Colors.White).Bold();
                        header.Cell().Background(Colors.Blue.Darken2).Padding(5).Text("Cantidad").FontColor(Colors.White).Bold();
                        header.Cell().Background(Colors.Blue.Darken2).Padding(5).Text("Monto Total").FontColor(Colors.White).Bold();
                        header.Cell().Background(Colors.Blue.Darken2).Padding(5).Text("# Facturas").FontColor(Colors.White).Bold();
                    });

                    int index = 1;
                    foreach (var item in datos)
                    {
                        var backgroundColor = index % 2 == 0 ? Colors.Grey.Lighten3 : Colors.White;

                        table.Cell().Background(backgroundColor).Padding(5).Text(index.ToString());
                        table.Cell().Background(backgroundColor).Padding(5).Text(item.Codigo);
                        table.Cell().Background(backgroundColor).Padding(5).Text(item.Nombre);
                        table.Cell().Background(backgroundColor).Padding(5).Text($"{item.CantidadVendida:N2}");
                        table.Cell().Background(backgroundColor).Padding(5).Text($"${item.MontoTotal:N2}");
                        table.Cell().Background(backgroundColor).Padding(5).Text(item.NumeroFacturas.ToString());

                        index++;
                    }

                    var totalCantidad = datos.Sum(d => d.CantidadVendida);
                    var totalMonto = datos.Sum(d => d.MontoTotal);
                    var totalFacturas = datos.Sum(d => d.NumeroFacturas);

                    table.Cell().ColumnSpan(3).Background(Colors.Blue.Lighten3).Padding(5).Text("TOTALES").Bold();
                    table.Cell().Background(Colors.Blue.Lighten3).Padding(5).Text($"{totalCantidad:N2}").Bold();
                    table.Cell().Background(Colors.Blue.Lighten3).Padding(5).Text($"${totalMonto:N2}").Bold();
                    table.Cell().Background(Colors.Blue.Lighten3).Padding(5).Text(totalFacturas.ToString()).Bold();
                });

                page.Footer().AlignCenter().Text(text =>
                {
                    text.Span("Página ");
                    text.CurrentPageNumber();
                    text.Span(" de ");
                    text.TotalPages();
                });
            });
        });

        return documento.GeneratePdf();
    }

    public byte[] GenerarPdfVentasPorMes(List<ReporteVentasMesDto> datos, int año)
    {
        var documento = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(2, Unit.Centimetre);
                page.DefaultTextStyle(x => x.FontSize(10));

                page.Header().Row(row =>
                {
                    row.RelativeItem().Column(column =>
                    {
                        column.Item().Text($"REPORTE DE VENTAS POR MES - AÑO {año}")
                            .FontSize(16)
                            .Bold()
                            .FontColor(Colors.Blue.Darken2);
                        
                        column.Item().Text($"Fecha de generación: {DateTime.Now:dd/MM/yyyy HH:mm}")
                            .FontSize(8)
                            .FontColor(Colors.Grey.Darken1);
                    });
                });

                page.Content().PaddingVertical(1, Unit.Centimetre).Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.RelativeColumn(3);
                        columns.RelativeColumn(2);
                        columns.RelativeColumn(2);
                        columns.RelativeColumn(2);
                    });

                    table.Header(header =>
                    {
                        header.Cell().Background(Colors.Blue.Darken2).Padding(5).Text("Mes").FontColor(Colors.White).Bold();
                        header.Cell().Background(Colors.Blue.Darken2).Padding(5).Text("# Facturas").FontColor(Colors.White).Bold();
                        header.Cell().Background(Colors.Blue.Darken2).Padding(5).Text("Total Facturado").FontColor(Colors.White).Bold();
                        header.Cell().Background(Colors.Blue.Darken2).Padding(5).Text("IVA Recaudado").FontColor(Colors.White).Bold();
                    });

                    foreach (var item in datos)
                    {
                        table.Cell().Padding(5).Text(item.NombreMes);
                        table.Cell().Padding(5).Text(item.NumeroFacturas.ToString());
                        table.Cell().Padding(5).Text($"${item.TotalFacturado:N2}");
                        table.Cell().Padding(5).Text($"${item.IVARecaudado:N2}");
                    }

                    var totalFacturas = datos.Sum(d => d.NumeroFacturas);
                    var totalFacturado = datos.Sum(d => d.TotalFacturado);
                    var totalIVA = datos.Sum(d => d.IVARecaudado);

                    table.Cell().Background(Colors.Blue.Lighten3).Padding(5).Text("TOTALES").Bold();
                    table.Cell().Background(Colors.Blue.Lighten3).Padding(5).Text(totalFacturas.ToString()).Bold();
                    table.Cell().Background(Colors.Blue.Lighten3).Padding(5).Text($"${totalFacturado:N2}").Bold();
                    table.Cell().Background(Colors.Blue.Lighten3).Padding(5).Text($"${totalIVA:N2}").Bold();
                });

                page.Footer().AlignCenter().Text(text =>
                {
                    text.Span("Página ");
                    text.CurrentPageNumber();
                    text.Span(" de ");
                    text.TotalPages();
                });
            });
        });

        return documento.GeneratePdf();
    }
}