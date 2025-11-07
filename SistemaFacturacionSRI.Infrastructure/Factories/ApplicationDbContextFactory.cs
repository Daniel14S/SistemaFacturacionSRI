using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using SistemaFacturacionSRI.Infrastructure.Data;
using Microsoft.Extensions.Configuration;

using System.IO;

namespace SistemaFacturacionSRI.Infrastructure.Factories
{
    public class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
    {
        public ApplicationDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();

            // Cargar la cadena de conexión desde el appsettings del proyecto WebUI
            var basePath = Directory.GetCurrentDirectory();
            // Intentar resolver ruta al WebUI (asumiendo estructura de solución estándar)
            var webUiPath = Path.Combine(basePath, "..", "SistemaFacturacionSRI.WebUI");
            if (!Directory.Exists(webUiPath))
            {
                // fallback al directorio actual (por si el comando se ejecuta desde el WebUI)
                webUiPath = basePath;
            }

            var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";

            var config = new ConfigurationBuilder()
                .SetBasePath(webUiPath)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
                .AddJsonFile($"appsettings.{environment}.json", optional: true, reloadOnChange: false)
                .AddEnvironmentVariables()
                .Build();

            var connectionString = config.GetConnectionString("DefaultConnection")
                ?? "Server=(localdb)\\MSSQLLocalDB;Database=SistemaFacturacionSRI;Trusted_Connection=True;MultipleActiveResultSets=true;Encrypt=False";

            optionsBuilder.UseSqlServer(connectionString);

            return new ApplicationDbContext(optionsBuilder.Options);
        }
    }
}
