// Script temporal para generar un PDF RIDE de prueba
// Ejecutar con: dotnet script GenerarPdfRide.cs

using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SistemaFacturacionSRI.Infrastructure.Data;
using SistemaFacturacionSRI.Application.Services;
using SistemaFacturacionSRI.Domain.Interfaces;
using SistemaFacturacionSRI.Infrastructure.Repositories;

class Program
{
    static async Task Main()
    {
        Console.WriteLine("=== Generador de PDF RIDE ===\n");
        
        // Configurar servicios
        var services = new ServiceCollection();
        
        // Agregar DbContext
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer("Server=localhost;Database=SistemaFacturacionSRI;Trusted_Connection=True;TrustServerCertificate=True;"));
        
        // Agregar repositorios y servicios
        services.AddScoped<IFacturaRepository, FacturaRepository>();
        services.AddScoped<IConfiguracionEmpresaRepository, ConfiguracionEmpresaRepository>();
        services.AddScoped<IPdfGeneratorService, PdfGeneratorService>();
        
        var provider = services.BuildServiceProvider();
        
        using var scope = provider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var pdfService = scope.ServiceProvider.GetRequiredService<IPdfGeneratorService>();
        
        // Obtener una factura
        var factura = await dbContext.Facturas
            .Include(f => f.Cliente)
                .ThenInclude(c => c.TipoIdentificacion)
            .Include(f => f.Detalles)
            .Include(f => f.InfoAdicional)
            .FirstOrDefaultAsync();
        
        if (factura == null)
        {
            Console.WriteLine("No hay facturas en la base de datos.");
            return;
        }
        
        Console.WriteLine($"Factura encontrada: {factura.NumeroFactura}");
        Console.WriteLine($"Cliente: {factura.Cliente?.RazonSocial ?? "N/A"}");
        Console.WriteLine($"Total: ${factura.ImporteTotal:N2}");
        
        // Generar PDF
        Console.WriteLine("\nGenerando PDF...");
        
        var pdfBytes = await pdfService.GenerarRideAsync(factura.Id);
        
        // Guardar en disco
        var outputPath = Path.Combine(Environment.CurrentDirectory, "wwwroot", "rides");
        Directory.CreateDirectory(outputPath);
        
        var fileName = $"RIDE_{factura.ClaveAcceso}.pdf";
        var fullPath = Path.Combine(outputPath, fileName);
        
        await File.WriteAllBytesAsync(fullPath, pdfBytes);
        
        Console.WriteLine($"\n✅ PDF generado exitosamente!");
        Console.WriteLine($"📁 Ubicación: {fullPath}");
        Console.WriteLine($"📄 Tamaño: {pdfBytes.Length / 1024.0:N2} KB");
    }
}
