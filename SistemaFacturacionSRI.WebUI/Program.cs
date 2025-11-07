using Microsoft.EntityFrameworkCore;
using SistemaFacturacionSRI.WebUI;
using SistemaFacturacionSRI.Infrastructure.Data;
using SistemaFacturacionSRI.Application.Interfaces.Repositories;
using SistemaFacturacionSRI.Infrastructure.Repositories;
using SistemaFacturacionSRI.Application.Interfaces.Services;
using SistemaFacturacionSRI.Application.Services;
using SistemaFacturacionSRI.Application.Mappings;
using System;
using System.Data;

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
app.MapRazorComponents<SistemaFacturacionSRI.WebUI.Components.App>()
    .AddInteractiveServerRenderMode();

// ===========================
// INICIALIZACIÓN DE BASE DE DATOS (MIGRATIONS)
// ===========================
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    // Aplica migraciones pendientes en cualquier entorno
    db.Database.Migrate();
}

// ===========================
app.Run();
