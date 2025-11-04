# SistemaFacturacionSRI

Sistema de Facturación orientado al SRI (Ecuador), construido con ASP.NET Core (Blazor Server/Minimal APIs) y Entity Framework Core sobre SQL Server.

## Estructura del proyecto

Monorepo con arquitectura por capas (Domain, Application, Infrastructure, WebUI):

```text
SistemaFacturacionSRI.sln
SistemaFacturacionSRI.Domain/           # Entidades de dominio y enums
SistemaFacturacionSRI.Application/      # DTOs, interfaces de repos/servicios, servicios de negocio, AutoMapper
SistemaFacturacionSRI.Infrastructure/   # EF Core: DbContext, Configurations (Fluent API), Migrations, Repositorios
SistemaFacturacionSRI.WebUI/            # Host ASP.NET Core (Razor Components + Minimal APIs)
```

Principales piezas:
- `ApplicationDbContext` (Infrastructure/Data) configura las tablas con Fluent API (incluye Productos, Roles, Usuarios, Clientes, TiposIdentificacion, TiposIVA, Categorias, Lotes, Facturas, FacturaDetalles).
- Repositorios genéricos y específicos (por ejemplo `ProductoRepository`).
- Servicios de aplicación (por ejemplo `ProductoService`) con AutoMapper.
- Endpoints Minimal API en `WebUI/Program.cs` bajo `/api/*`.

## Tecnologías

- .NET 8 (TargetFramework: net8.0)
- ASP.NET Core (Razor Components + Minimal APIs)
- Entity Framework Core (SqlServer, Design, Tools)
- SQL Server (LocalDB por defecto)
- AutoMapper, BCrypt.Net-Next

## Prerrequisitos

- .NET SDK 8.0 o superior
- SQL Server local (recomendado: LocalDB o SQLEXPRESS)

## Configuración de base de datos

La cadena de conexión se define en `SistemaFacturacionSRI.WebUI/appsettings.Development.json` como `ConnectionStrings:DefaultConnection`.

Valores de ejemplo:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\MSSQLLocalDB;Database=SistemaFacturacionSRI;Trusted_Connection=True;MultipleActiveResultSets=true;Encrypt=False"
  }
}
```

Alternativas comunes:
- SQLEXPRESS: `Server=.\\SQLEXPRESS;Database=SistemaFacturacionSRI;Trusted_Connection=True;MultipleActiveResultSets=true;Encrypt=False`
- SQL Auth: `Server=SERVIDOR;Database=SistemaFacturacionSRI;User Id=usuario;Password=clave;TrustServerCertificate=True;MultipleActiveResultSets=true`

El host aplica migraciones automáticamente al iniciar (`Database.Migrate()`).

## Migraciones (EF Core)

Comandos (PowerShell) desde la raíz del repo:

```powershell
# Crear una nueva migración
dotnet ef migrations add NombreDeMigracion `
  -s .\SistemaFacturacionSRI.WebUI\SistemaFacturacionSRI.WebUI.csproj `
  -p .\SistemaFacturacionSRI.Infrastructure\SistemaFacturacionSRI.Infrastructure.csproj

# Aplicar migraciones a la base
dotnet ef database update `
  -s .\SistemaFacturacionSRI.WebUI\SistemaFacturacionSRI.WebUI.csproj `
  -p .\SistemaFacturacionSRI.Infrastructure\SistemaFacturacionSRI.Infrastructure.csproj

# Listar migraciones
dotnet ef migrations list `
  -s .\SistemaFacturacionSRI.WebUI\SistemaFacturacionSRI.WebUI.csproj `
  -p .\SistemaFacturacionSRI.Infrastructure\SistemaFacturacionSRI.Infrastructure.csproj
```

Migraciones actuales aplicadas:
- `InitialCreate` (Productos)
- `AddAuthBillingSchema` (Roles, Usuarios, TiposIdentificacion, Clientes, TiposIVA, Categorias, Lotes, Facturas, FacturaDetalles)

## Cómo ejecutar

```powershell
# Restaurar y compilar
dotnet restore .\SistemaFacturacionSRI.sln
dotnet build .\SistemaFacturacionSRI.sln -c Debug

# Ejecutar el host web (Razor + Minimal APIs)
dotnet run --project .\SistemaFacturacionSRI.WebUI\SistemaFacturacionSRI.WebUI.csproj
```

Por defecto escuchará en un puerto localhost (por ejemplo, `http://localhost:5293`).

## Endpoints (Productos)

Base: `/api/productos`

- POST `/api/productos` — Crea un producto
  - Body JSON:

```json
{
  "codigo": "PROD-001",
  "nombre": "Mouse Inalámbrico",
  "descripcion": "Mouse Bluetooth",
  "precio": 15.99,
  "tipoIVA": 12,
  "stock": 50,
  "unidadMedida": "Unidad"
}
```

Respuestas: 201 Created | 409 Conflict | 400 Bad Request


- GET `/api/productos` — Lista productos activos
- GET `/api/productos/{id}` — Obtiene un producto por Id (200 | 404)
- PUT `/api/productos/{id}` — Actualiza un producto (200 | 404 | 409)
  - Body JSON: igual a crear, pero incluye `id` y permite cambiar campos
- DELETE `/api/productos/{id}` — Eliminación lógica (204 | 404)

## Notas de desarrollo

- AutoMapper está registrado (`ProductoProfile`).
- Repositorios y servicios inyectados en DI (`IRepositoryBase<>`, `IProductoRepository`, `IProductoService`).
- Las propiedades calculadas de `Producto` no se mapean en la BD (se ignoran en Fluent API).

## Contribución

Flujo sugerido:

1. Crear rama feature a partir de `main` (o rama base): `feature/nombre-tarea`.
2. Commits pequeños y descriptivos.
3. Pull Request hacia la rama base, con descripción de cambios y pasos de prueba.

