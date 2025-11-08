using Microsoft.EntityFrameworkCore;
using SistemaFacturacionSRI.Infrastructure.Data;
using SistemaFacturacionSRI.Application.Interfaces.Repositories;
using SistemaFacturacionSRI.Infrastructure.Repositories;
using SistemaFacturacionSRI.Application.Interfaces.Services;
using SistemaFacturacionSRI.Application.Services;
using SistemaFacturacionSRI.Application.Mappings;
using SistemaFacturacionSRI.WebUI.Services;

var builder = WebApplication.CreateBuilder(args);

// ===========================
// CONFIGURACIÓN DE SERVICIOS
// ===========================

// Blazor Server y Razor
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();

// Base de datos (SQL Server)
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"))
);

// Repositorios y Servicios (Inyección de dependencias)
builder.Services.AddScoped(typeof(IRepositoryBase<>), typeof(RepositoryBase<>));
builder.Services.AddScoped<IProductoRepository, ProductoRepository>();
builder.Services.AddScoped<IProductoService, ProductoService>();
builder.Services.AddScoped<ICategoriaRepository, CategoriaRepository>();
builder.Services.AddScoped<ICategoriaService, CategoriaService>();
builder.Services.AddScoped<ITipoIVARepository, TipoIVARepository>();
builder.Services.AddScoped<ITipoIVAService, TipoIVAService>();
builder.Services.AddScoped<ILoteRepository, LoteRepository>();
builder.Services.AddScoped<ILoteService, LoteService>();
builder.Services.AddScoped<ICategoriaService, CategoriaService>();

// ✅ Cliente HTTP para consumir la API desde Blazor
builder.Services.AddScoped<ProductoHttpService>(sp =>
{
    var httpClient = new HttpClient
    {
        BaseAddress = new Uri("http://localhost:5293") // Ajusta según tu puerto
    };
    return new ProductoHttpService(httpClient);
});

// ✅ NUEVO: Cliente HTTP para lotes
builder.Services.AddScoped<LoteHttpService>(sp =>
{
    var httpClient = new HttpClient
    {
        BaseAddress = new Uri("http://localhost:5293") // Ajusta según tu puerto
    };
    return new LoteHttpService(httpClient);
});

builder.Services.AddScoped<CategoriaHttpService>(sp =>
{
    var httpClient = sp.GetRequiredService<HttpClient>();
    var logger = sp.GetRequiredService<ILogger<CategoriaHttpService>>();
    return new CategoriaHttpService(httpClient, logger);
});

// Controladores (para los endpoints API)
builder.Services.AddControllers();

// AutoMapper (para mapear DTOs ↔ entidades)
builder.Services.AddAutoMapper(typeof(ProductoProfile).Assembly);

var app = builder.Build();

// ===========================
// CONFIGURACIÓN DE MIDDLEWARE
// ===========================

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

// ✅ Orden correcto del pipeline
app.UseRouting();

// 🔒 Antiforgery debe ir después de UseRouting()
app.UseAntiforgery();

// ✅ Mapea controladores (endpoints API)
app.MapControllers();

// ✅ Mapea los componentes Blazor
// 🔧 CORREGIDO: Busca el archivo App.razor en la carpeta correcta
app.MapRazorComponents<SistemaFacturacionSRI.WebUI.Components.App>()
    .AddInteractiveServerRenderMode();
// ===========================
// INICIALIZACIÓN DE BASE DE DATOS (MIGRATIONS)
// ===========================
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    db.Database.Migrate();
}

app.Run();