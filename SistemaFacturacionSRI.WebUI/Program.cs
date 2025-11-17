using Microsoft.JSInterop;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using SistemaFacturacionSRI.Infrastructure.Data;
using SistemaFacturacionSRI.Application.Interfaces.Repositories;
using SistemaFacturacionSRI.Application.Interfaces;
using SistemaFacturacionSRI.Infrastructure.Repositories;
using SistemaFacturacionSRI.Application.Interfaces.Services;
using SistemaFacturacionSRI.Application.Services;
using SistemaFacturacionSRI.Application.Mappings;
using SistemaFacturacionSRI.Application.Interfaces.Security;
using SistemaFacturacionSRI.Application.Security;
using SistemaFacturacionSRI.WebUI.Services;
using SistemaFacturacionSRI.WebUI.Components;
using SistemaFacturacionSRI.WebUI.Middleware;
using SistemaFacturacionSRI.WebUI.Authorization;

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
builder.Services.AddScoped<IUsuarioRepository, UsuarioRepository>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IUsuarioService, UsuarioService>();
builder.Services.AddScoped<IPasswordHasher, PasswordHasher>();
builder.Services.AddSingleton<JwtTokenGenerator>();
builder.Services.AddSingleton<ITokenStorage, TokenStorage>();
builder.Services.AddScoped<IAutoLoginService, AutoLoginService>();

// 🔐 Configuración de autenticación JWT
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    var jwtSettings = builder.Configuration.GetSection("JwtSettings");
    var secretKey = jwtSettings["SecretKey"];

    if (string.IsNullOrEmpty(secretKey))
    {
        throw new InvalidOperationException("JWT SecretKey no está configurada en appsettings.json");
    }

    options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(
            System.Text.Encoding.UTF8.GetBytes(secretKey)),
        ValidateIssuer = true,
        ValidIssuer = jwtSettings["Issuer"],
        ValidateAudience = true,
        ValidAudience = jwtSettings["Audience"],
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero // Sin tolerancia de tiempo
    };

    // Configuración para APIs
    options.Events = new Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            // Intentar obtener el token del header Authorization
            var authorization = context.Request.Headers["Authorization"].ToString();
            if (!string.IsNullOrEmpty(authorization) && authorization.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            {
                context.Token = authorization.Substring("Bearer ".Length).Trim();
            }

            return Task.CompletedTask;
        },
        OnAuthenticationFailed = context =>
        {
            if (context.Exception.GetType() == typeof(Microsoft.IdentityModel.Tokens.SecurityTokenExpiredException))
            {
                context.Response.Headers["Token-Expired"] = "true";
            }
            return Task.CompletedTask;
        }
    };
});

// 🔐 Configuración de autorización con políticas
builder.Services.AddAuthorization(options =>
{
    // Política para administradores
    options.AddPolicy(AuthorizationPolicies.AdminOnly, policy =>
        policy.RequireRole("Administrador"));

    // Política para vendedores
    options.AddPolicy(AuthorizationPolicies.VendedorOnly, policy =>
        policy.RequireRole("Vendedor"));

    // Política para admin o vendedor
    options.AddPolicy(AuthorizationPolicies.AdminOrVendedor, policy =>
        policy.RequireRole("Administrador", "Vendedor"));
});

builder.Services.AddHttpClient<IProductoHttpService, ProductoHttpService>(client =>
{
    client.BaseAddress = new Uri("http://localhost:5293");
})
.AddHttpMessageHandler<AuthHeaderHandler>();

builder.Services.AddHttpClient<ILoteHttpService, LoteHttpService>(client =>
{
    client.BaseAddress = new Uri("http://localhost:5293");
})
.AddHttpMessageHandler<AuthHeaderHandler>();

builder.Services.AddHttpClient<ICategoriaHttpService, CategoriaHttpService>(client =>
{
    client.BaseAddress = new Uri("http://localhost:5293");
})
.AddHttpMessageHandler<AuthHeaderHandler>();

builder.Services.AddHttpClient<IAuthHttpService, AuthHttpService>(client =>
{
    client.BaseAddress = new Uri("http://localhost:5293");
})
.AddHttpMessageHandler<AuthHeaderHandler>();

builder.Services.AddTransient<AuthHeaderHandler>();

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

// 🔒 JWT Middleware (procesa tokens en cada petición)
app.UseJwtMiddleware();  

// 🔒 Autenticación y Autorización (DESPUÉS de UseRouting)
app.UseAuthentication();
app.UseAuthorization();

app.UseAuthErrorHandling();

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
    db.Database.Migrate();
}

app.Run();