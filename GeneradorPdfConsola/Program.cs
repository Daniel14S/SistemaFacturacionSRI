// Script para generar PDF RIDE sin servidor web
// Ejecutar: dotnet run --project GeneradorPdfConsola

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SistemaFacturacionSRI.Domain.Interfaces;
using SistemaFacturacionSRI.Domain.Interfaces.Repositories;
using SistemaFacturacionSRI.Infrastructure.Data;
using SistemaFacturacionSRI.Infrastructure.Repositories;
using SistemaFacturacionSRI.Infrastructure.Services;

class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("╔═══════════════════════════════════════════════════╗");
        Console.WriteLine("║     GENERADOR DE PDF RIDE - Sistema Facturación   ║");
        Console.WriteLine("╚═══════════════════════════════════════════════════╝");
        Console.WriteLine();

        // Configurar servicios
        var services = new ServiceCollection();
        
        // Agregar logging
        services.AddLogging(builder => builder.AddConsole());
        
        // Agregar DbContext
        var connectionString = "Server=localhost\\SQLEXPRESS;Database=SistemaFacturacionSRI;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True;Encrypt=False";
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(connectionString));
        
        // Agregar repositorios y servicios
        services.AddScoped<IFacturaRepository, FacturaRepository>();
        services.AddScoped<IConfiguracionEmpresaRepository, ConfiguracionEmpresaRepository>();
        services.AddScoped<IPdfGeneratorService, PdfGeneratorService>();
        
        var provider = services.BuildServiceProvider();
        
        using var scope = provider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var pdfService = scope.ServiceProvider.GetRequiredService<IPdfGeneratorService>();
        
        Console.WriteLine("📦 Conectando a la base de datos...");
        
        // Obtener una factura con todos sus datos
        var factura = await dbContext.Facturas
            .Include(f => f.Cliente)
                .ThenInclude(c => c.TipoIdentificacion)
            .Include(f => f.Detalles)
            .Include(f => f.InfoAdicional)
            .FirstOrDefaultAsync();
        
        if (factura == null)
        {
            Console.WriteLine("❌ No hay facturas en la base de datos.");
            return;
        }
        
        Console.WriteLine();
        Console.WriteLine("📄 FACTURA ENCONTRADA:");
        Console.WriteLine($"   ID: {factura.Id}");
        Console.WriteLine($"   Número: {factura.NumeroFactura}");
        Console.WriteLine($"   Cliente: {factura.Cliente?.Nombre1 ?? "N/A"} {factura.Cliente?.Apellido1 ?? ""}");
        Console.WriteLine($"   Fecha: {factura.FechaEmision:dd/MM/yyyy}");
        Console.WriteLine($"   Total: ${factura.ImporteTotal:N2}");
        Console.WriteLine($"   Estado: {factura.Estado}");
        Console.WriteLine($"   Clave de Acceso: {factura.ClaveAcceso}");
        Console.WriteLine();
        
        // Definir ruta de salida
        var outputDir = Path.Combine(Environment.CurrentDirectory, "wwwroot", "comprobantes", "pdf");
        Directory.CreateDirectory(outputDir);
        
        var outputPath = Path.Combine(outputDir, $"RIDE_{factura.ClaveAcceso}.pdf");
        
        Console.WriteLine("🔄 Generando PDF RIDE...");
        
        try
        {
            var rutaPdf = await pdfService.GenerarRideAsync(factura.Id, outputPath);
            
            var fileInfo = new FileInfo(rutaPdf);
            
            Console.WriteLine();
            Console.WriteLine("═══════════════════════════════════════════════════");
            Console.WriteLine("✅ PDF GENERADO EXITOSAMENTE!");
            Console.WriteLine("═══════════════════════════════════════════════════");
            Console.WriteLine();
            Console.WriteLine($"📁 Ubicación del archivo:");
            Console.WriteLine($"   {rutaPdf}");
            Console.WriteLine();
            Console.WriteLine($"📊 Información del archivo:");
            Console.WriteLine($"   Tamaño: {fileInfo.Length / 1024.0:N2} KB");
            Console.WriteLine($"   Creado: {fileInfo.CreationTime:dd/MM/yyyy HH:mm:ss}");
            Console.WriteLine();
            
            // Abrir el archivo
            Console.WriteLine("¿Desea abrir el PDF? (S/N): ");
            var respuesta = Console.ReadLine();
            if (respuesta?.ToUpper() == "S")
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = rutaPdf,
                    UseShellExecute = true
                });
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine();
            Console.WriteLine("❌ ERROR AL GENERAR PDF:");
            Console.WriteLine($"   {ex.Message}");
            Console.WriteLine();
            Console.WriteLine("Detalles:");
            Console.WriteLine(ex.ToString());
        }
    }
}
