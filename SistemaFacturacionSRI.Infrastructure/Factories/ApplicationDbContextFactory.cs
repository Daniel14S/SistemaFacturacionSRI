using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using SistemaFacturacionSRI.Infrastructure.Data;

namespace SistemaFacturacionSRI.Infrastructure.Factories
{
    public class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
    {
        public ApplicationDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();

            // ⚠️ Usa la misma cadena que tienes en appsettings.json
            var connectionString = "Server=localhost\\SQLEXPRESS;Database=SistemaFacturacionSRI;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True;Encrypt=False";

            optionsBuilder.UseSqlServer(connectionString);

            return new ApplicationDbContext(optionsBuilder.Options);
        }
    }
}
