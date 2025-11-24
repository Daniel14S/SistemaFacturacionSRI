using Microsoft.JSInterop;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using SistemaFacturacionSRI.Infrastructure.Data;
using SistemaFacturacionSRI.Infrastructure.Contexts;  // ⭐ AGREGADO
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
using Blazored.LocalStorage;  
using Microsoft.AspNetCore.Components.Authorization;

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
// ⭐ SistemaContext - Context principal generado por scaffold
builder.Services.AddDbContext<SistemaContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// ⭐ ApplicationDbContext - Si es el mismo, considera eliminarlo para evitar duplicados
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
builder.Services.AddScoped<WebAuthService>();
builder.Services.AddScoped<IUsuarioService, UsuarioService>();
builder.Services.AddScoped<IPasswordHasher, PasswordHasher>();
builder.Services.AddSingleton<JwtTokenGenerator>();
builder.Services.AddSingleton<ITokenStorage, TokenStorage>();
builder.Services.AddScoped<IClienteRepository, ClienteRepository>();
builder.Services.AddScoped<IClienteService, ClienteService>();
builder.Services.AddBlazoredLocalStorage();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddAuthorizationCore();


// ✅ CORREGIDO: CustomAuthenticationStateProvider como servicio único
builder.Services.AddScoped<CustomAuthenticationStateProvider>();
builder.Services.AddScoped<AuthenticationStateProvider>(provider =>
    provider.GetRequiredService<CustomAuthenticationStateProvider>());


// Después de las líneas existentes, agregar:
builder.Services.AddScoped<ToastService>();

builder.Services.AddHttpClient<IUsuarioHttpService, UsuarioHttpService>(client =>
{
    client.BaseAddress = new Uri("http://localhost:5293");
})
.AddHttpMessageHandler<AuthHeaderHandler>();


// ✅ CORS - Configuración para desarrollo local
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowBlazorDevelopment", policy =>
    {
        policy.WithOrigins(
            "https://localhost:5293",
            "http://localhost:5292",
            "https://localhost:7001"
        )
        .AllowAnyMethod()
        .AllowAnyHeader()
        .AllowCredentials();
    });
});

// 🔐 Configuración de autenticación JWT
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    var jwtSettings = builder.Configuration.GetSection("JwtSettings");
    var secretKey = jwtSettings["SecretKey"];

    if (string.IsNullOrEmpty(secretKey))
    {
        throw new InvalidOperationException("JWT SecretKey no está configurada en appsettings.json");
    }

    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
        ValidateIssuer = true,
        ValidIssuer = jwtSettings["Issuer"],
        ValidateAudience = true,
        ValidAudience = jwtSettings["Audience"],
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };

    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            var authorization = context.Request.Headers["Authorization"].ToString();
            if (!string.IsNullOrEmpty(authorization) && authorization.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            {
                context.Token = authorization.Substring("Bearer ".Length).Trim();
            }
            return Task.CompletedTask;
        },
        OnAuthenticationFailed = context =>
        {
            if (context.Exception.GetType() == typeof(SecurityTokenExpiredException))
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
    options.AddPolicy(AuthorizationPolicies.AdminOnly, policy =>
        policy.RequireRole("Administrador"));

    options.AddPolicy(AuthorizationPolicies.VendedorOnly, policy =>
        policy.RequireRole("Vendedor"));

    options.AddPolicy(AuthorizationPolicies.AdminOrVendedor, policy =>
        policy.RequireRole("Administrador", "Vendedor"));
});

// ✅ AuthHeaderHandler debe estar ANTES de los HttpClients
builder.Services.AddTransient<AuthHeaderHandler>();

// ✅ HttpClients CON AuthHeaderHandler para adjuntar token
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

// ⚠️ AuthHttpService NO debe tener AuthHeaderHandler (es para login)
builder.Services.AddHttpClient<IAuthHttpService, AuthHttpService>(client =>
{
    client.BaseAddress = new Uri("http://localhost:5293");
});

// ✅ Cliente HttpClient con AuthHeaderHandler
builder.Services.AddHttpClient<IClienteHttpService, ClienteHttpService>(client =>
{
    client.BaseAddress = new Uri("http://localhost:5293");
})
.AddHttpMessageHandler<AuthHeaderHandler>();

// Controladores (para los endpoints API)
builder.Services.AddControllers();

// AutoMapper (para mapear DTOs ↔ entidades)
builder.Services.AddAutoMapper(typeof(ProductoProfile).Assembly);

var app = builder.Build();

// ===========================
// CONFIGURACIÓN DE MIDDLEWARE
// ===========================

// ✅ Headers de seguridad CSP
app.Use(async (context, next) =>
{
    context.Response.Headers["Content-Security-Policy"] =
        "default-src 'self' 'unsafe-inline' 'unsafe-eval' " +
        "https://localhost:* http://localhost:* " +
        "ws://localhost:* wss://localhost:*; " +
        "font-src 'self' data:; " +
        "img-src 'self' data: https:; " +
        "style-src 'self' 'unsafe-inline'; " +
        "connect-src 'self' " +
        "ws://localhost:* wss://localhost:* " +
        "https://localhost:* http://localhost:*;";

    await next();
});

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseCors("AllowBlazorDevelopment");

app.UseRouting();

// 🔒 JWT Middleware
app.UseJwtMiddleware();  

// 🔒 Autenticación y Autorización
app.UseAuthentication();
app.UseAuthorization();

app.UseAuthErrorHandling();

app.UseAntiforgery();

// ✅ Mapea controladores (endpoints API)
app.MapControllers();

// ✅ Mapea los componentes Blazor
app.MapRazorComponents<SistemaFacturacionSRI.WebUI.Components.App>()
    .AddInteractiveServerRenderMode().AllowAnonymous();

// ===========================
// INICIALIZACIÓN DE BASE DE DATOS
// ===========================
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    db.Database.Migrate();
}

app.Run();