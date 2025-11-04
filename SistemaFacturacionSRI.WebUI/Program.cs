using Microsoft.EntityFrameworkCore;
using SistemaFacturacionSRI.WebUI.Components;
using SistemaFacturacionSRI.Infrastructure.Data;
using SistemaFacturacionSRI.Application.Interfaces.Repositories;
using SistemaFacturacionSRI.Infrastructure.Repositories;
using SistemaFacturacionSRI.Application.Interfaces.Services;
using SistemaFacturacionSRI.Application.Services;
using SistemaFacturacionSRI.Application.Mappings;
using SistemaFacturacionSRI.Application.DTOs.Producto;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Database: SQL Server
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"))
);

// Repositories and Services
builder.Services.AddScoped(typeof(IRepositoryBase<>), typeof(RepositoryBase<>));
builder.Services.AddScoped<IProductoRepository, ProductoRepository>();
builder.Services.AddScoped<IProductoService, ProductoService>();

// AutoMapper
builder.Services.AddAutoMapper(typeof(ProductoProfile).Assembly);

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// Minimal API: Productos
var productosGroup = app.MapGroup("/api/productos").WithTags("Productos");

// POST /api/productos
productosGroup.MapPost("/", async (CrearProductoDto dto, IProductoService servicio, HttpContext http) =>
{
    try
    {
        var creado = await servicio.CrearAsync(dto);
        var location = $"{http.Request.Scheme}://{http.Request.Host}/api/productos/{creado.Id}";
        return Results.Created(location, creado);
    }
    catch (InvalidOperationException ex)
    {
        return Results.Conflict(new { message = ex.Message });
    }
    catch (ArgumentException ex)
    {
        return Results.BadRequest(new { message = ex.Message });
    }
});

// GET /api/productos
productosGroup.MapGet("/", async (IProductoService servicio) =>
{
    var lista = await servicio.ObtenerTodosAsync();
    return Results.Ok(lista);
});

// GET /api/productos/{id}
productosGroup.MapGet("/{id:int}", async (int id, IProductoService servicio) =>
{
    var prod = await servicio.ObtenerPorIdAsync(id);
    return prod is null ? Results.NotFound() : Results.Ok(prod);
});

// PUT /api/productos/{id}
productosGroup.MapPut("/{id:int}", async (int id, ActualizarProductoDto dto, IProductoService servicio) =>
{
    try
    {
        // Forzar coincidencia de ruta y body
        dto.Id = id;
        var actualizado = await servicio.ActualizarAsync(dto);
        return Results.Ok(actualizado);
    }
    catch (KeyNotFoundException ex)
    {
        return Results.NotFound(new { message = ex.Message });
    }
    catch (InvalidOperationException ex)
    {
        return Results.Conflict(new { message = ex.Message });
    }
    catch (ArgumentException ex)
    {
        return Results.BadRequest(new { message = ex.Message });
    }
});

// DELETE /api/productos/{id}
productosGroup.MapDelete("/{id:int}", async (int id, IProductoService servicio) =>
{
    try
    {
        await servicio.EliminarAsync(id);
        return Results.NoContent();
    }
    catch (KeyNotFoundException ex)
    {
        return Results.NotFound(new { message = ex.Message });
    }
});

// Ensure database exists and apply migrations at startup
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    db.Database.Migrate();
}

app.Run();
