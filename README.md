# 🧾 Sistema de Facturación Electrónica SRI

> Sistema de facturación orientado al SRI (Ecuador), construido con ASP.NET Core (Blazor Server/Minimal APIs) y Entity Framework Core sobre SQL Server.

[![.NET](https://img.shields.io/badge/.NET-8.0-512BD4?logo=dotnet)](https://dotnet.microsoft.com/)
[![SQL Server](https://img.shields.io/badge/SQL%20Server-2022-CC2927?logo=microsoftsqlserver)](https://www.microsoft.com/sql-server)
[![License](https://img.shields.io/badge/License-Academic-green)](LICENSE)

---

## 📋 Tabla de Contenidos

- [Descripción](#-descripción)
- [Características](#-características)
- [Tecnologías](#-tecnologías)
- [Arquitectura](#-arquitectura)
- [Inicio Rápido](#-inicio-rápido)
- [Instalación Detallada](#-instalación-detallada)
- [Configuración de Base de Datos](#-configuración-de-base-de-datos)
- [API Endpoints](#-api-endpoints)
- [Migraciones](#-migraciones-ef-core)
- [Equipo](#-equipo)
- [Documentación](#-documentación)

---

## 📝 Descripción

Sistema de facturación electrónica conforme a las normativas del **SRI (Servicio de Rentas Internas)** de Ecuador. Implementado con arquitectura Onion (Domain-Driven Design) para máxima escalabilidad y mantenibilidad.

### 🎯 Módulos Implementados

#### Sprint 1 ✅
- ✅ **Gestión de Productos** - CRUD completo con validaciones
- ✅ **Tipos de IVA** - 0%, 12%, 15% según normativa ecuatoriana
- ✅ **Auditoría automática** - Tracking de fechas de creación/modificación
- ✅ **Eliminación lógica** - Soft delete para mantener historial

#### Próximos Sprints 🚧
- 🚧 Gestión de Clientes y Usuarios
- 🚧 Generación de Facturas Electrónicas
- 🚧 Integración con API del SRI
- 🚧 Reportes y Dashboard

---

## ✨ Características

- 🏗️ **Arquitectura Onion** - Separación clara de responsabilidades
- 🔒 **Seguridad** - Encriptación BCrypt, validaciones robustas
- 📊 **Auditoría** - Tracking automático de cambios
- 🔄 **API REST** - Minimal APIs con Swagger
- 💻 **UI Moderna** - Blazor Server con componentes interactivos
- 🗄️ **ORM Robusto** - Entity Framework Core con Fluent API
- 🔀 **AutoMapper** - Mapeo automático entre DTOs y entidades
- 📱 **Responsive** - Diseño adaptable con Bootstrap 5

---

## 🛠️ Tecnologías

### Backend
- **.NET 8.0** - Framework principal
- **C# 12** - Lenguaje de programación
- **Entity Framework Core 9.0** - ORM
- **AutoMapper 12.0** - Mapeo objeto-objeto
- **BCrypt.Net 4.0** - Hash de contraseñas

### Frontend
- **Blazor Server** - UI interactiva con C#
- **Razor Components** - Componentes reutilizables
- **Bootstrap 5** - Framework CSS

### Base de Datos
- **SQL Server 2022 Express** - Motor de BD
- **LocalDB / SQLEXPRESS** - Instancias locales

### Herramientas
- **Git & GitHub** - Control de versiones
- **Visual Studio Code** - Editor
- **SSMS** - Administración de BD (opcional)

---

## 🏛️ Arquitectura

### Estructura del Proyecto (Onion Architecture)
```text
SistemaFacturacionSRI/
│
├── SistemaFacturacionSRI.sln                 # Solución principal
│
├── 📦 Domain/                                # ⭕ NÚCLEO (sin dependencias)
│   ├── Entities/                             # Entidades de dominio
│   │   ├── EntidadBase.cs
│   │   └── Producto.cs
│   └── Enums/                                # Enumeraciones
│       ├── TipoIVA.cs                        # 0%, 12%, 15%
│       └── TipoIVAExtensions.cs
│
├── 📦 Application/                           # ⭕ LÓGICA DE NEGOCIO
│   ├── DTOs/                                 # Data Transfer Objects
│   │   └── Producto/
│   │       ├── ProductoDto.cs                # Para lectura (GET)
│   │       ├── CrearProductoDto.cs           # Para creación (POST)
│   │       └── ActualizarProductoDto.cs      # Para actualización (PUT)
│   ├── Interfaces/                           # Contratos
│   │   ├── Repositories/
│   │   │   ├── IRepositoryBase.cs
│   │   │   └── IProductoRepository.cs
│   │   └── Services/
│   │       └── IProductoService.cs
│   ├── Mappings/                             # AutoMapper Profiles
│   │   └── ProductoProfile.cs
│   └── Services/                             # Implementación de servicios
│       └── ProductoService.cs
│
├── 📦 Infrastructure/                        # ⭕ IMPLEMENTACIONES
│   ├── Data/
│   │   ├── ApplicationDbContext.cs           # EF Core DbContext
│   │   └── Configurations/                   # Fluent API
│   │       └── ProductoConfiguration.cs
│   └── Repositories/                         # Implementación de repositorios
│       ├── RepositoryBase.cs
│       └── ProductoRepository.cs
│
└── 📦 WebUI/                                 # ⭕ PRESENTACIÓN
    ├── Components/
    │   ├── Layout/                           # Layouts de la app
    │   └── Pages/                            # Páginas Blazor
    │       ├── Home.razor
    │       └── Productos/
    ├── wwwroot/                              # Assets estáticos
    ├── appsettings.json                      # Configuración
    └── Program.cs                            # Configuración de servicios + Minimal APIs
```

### Flujo de Dependencias (Onion)
```
┌─────────────────────────────────────────┐
│           WebUI (Presentación)          │
│  Razor Components + Minimal APIs        │
└──────────────────┬──────────────────────┘
                   │
        ┌──────────▼──────────┐
        │   Infrastructure    │
        │  DbContext, Repos   │
        └──────────┬──────────┘
                   │
        ┌──────────▼──────────┐
        │    Application      │
        │  Services, DTOs     │
        └──────────┬──────────┘
                   │
        ┌──────────▼──────────┐
        │      Domain         │
        │  Entities, Enums    │  ← NÚCLEO (sin dependencias)
        └─────────────────────┘
```

---

## 🚀 Inicio Rápido

### Prerrequisitos

- ✅ .NET SDK 8.0 o superior
- ✅ SQL Server local (LocalDB o SQLEXPRESS)
- ✅ Git

### Instalación Express
```bash
# 1. Clonar repositorio
git clone https://github.com/TU_USUARIO/SistemaFacturacionSRI.git
cd SistemaFacturacionSRI

# 2. Cambiar a rama develop
git checkout develop

# 3. Restaurar dependencias
dotnet restore

# 4. Configurar base de datos (ver sección Configuración de BD)
# Editar: SistemaFacturacionSRI.WebUI/appsettings.json

# 5. Crear y aplicar migraciones
dotnet ef database update \
  -s .\SistemaFacturacionSRI.WebUI\SistemaFacturacionSRI.WebUI.csproj \
  -p .\SistemaFacturacionSRI.Infrastructure\SistemaFacturacionSRI.Infrastructure.csproj

# 6. Ejecutar
dotnet run --project SistemaFacturacionSRI.WebUI
```

🌐 **Abre tu navegador en:** `https://localhost:7001`

---

## 📖 Instalación Detallada

Para una guía paso a paso completa, consulta:

👉 **[GUIA_INSTALACION.md](GUIA_INSTALACION.md)**

Incluye:
- Instalación de .NET SDK 8.0
- Instalación de SQL Server Express
- Instalación de SQL Server Management Studio (SSMS)
- Configuración de Visual Studio Code
- Instalación de Entity Framework Tools
- Solución de problemas comunes

---

## 🗄️ Configuración de Base de Datos

### Cadena de Conexión

Editar: `SistemaFacturacionSRI.WebUI/appsettings.json` o `appsettings.Development.json`

**Opción 1: LocalDB (por defecto)**
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\MSSQLLocalDB;Database=SistemaFacturacionSRI;Trusted_Connection=True;MultipleActiveResultSets=true;Encrypt=False"
  }
}
```

**Opción 2: SQLEXPRESS**
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost\\SQLEXPRESS;Database=FacturacionSRI;Trusted_Connection=True;TrustServerCertificate=True;"
  }
}
```

**Opción 3: SQL Authentication**
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=SERVIDOR;Database=SistemaFacturacionSRI;User Id=usuario;Password=clave;TrustServerCertificate=True;MultipleActiveResultSets=true"
  }
}
```

### Migración Automática

El host aplica migraciones automáticamente al iniciar (`Database.Migrate()`). No es necesario ejecutar comandos manualmente en producción.

---

## 🔌 API Endpoints

### Productos API

**Base URL:** `/api/productos`

| Método | Endpoint | Descripción | Autenticación |
|--------|----------|-------------|---------------|
| `POST` | `/api/productos` | Crear producto | No requerida |
| `GET` | `/api/productos` | Listar todos los productos activos | No requerida |
| `GET` | `/api/productos/{id}` | Obtener producto por ID | No requerida |
| `PUT` | `/api/productos/{id}` | Actualizar producto | No requerida |
| `DELETE` | `/api/productos/{id}` | Eliminar producto (soft delete) | No requerida |

### Ejemplos de Peticiones

#### Crear Producto (POST)
```bash
curl -X POST https://localhost:7001/api/productos \
  -H "Content-Type: application/json" \
  -d '{
    "codigo": "PROD-001",
    "nombre": "Mouse Inalámbrico",
    "descripcion": "Mouse Bluetooth",
    "precio": 15.99,
    "tipoIVA": 12,
    "stock": 50,
    "unidadMedida": "Unidad"
  }'
```

**Respuestas:**
- `201 Created` - Producto creado exitosamente
- `409 Conflict` - El código ya existe
- `400 Bad Request` - Datos inválidos

#### Listar Productos (GET)
```bash
curl https://localhost:7001/api/productos
```

**Respuesta:** `200 OK`
```json
[
  {
    "id": 1,
    "codigo": "PROD-001",
    "nombre": "Mouse Inalámbrico",
    "precio": 15.99,
    "tipoIVA": 12,
    "tipoIVADescripcion": "IVA 12%",
    "valorIVA": 1.92,
    "precioConIVA": 17.91,
    "stock": 50,
    "tieneStock": true
  }
]
```

#### Actualizar Producto (PUT)
```bash
curl -X PUT https://localhost:7001/api/productos/1 \
  -H "Content-Type: application/json" \
  -d '{
    "id": 1,
    "codigo": "PROD-001",
    "nombre": "Mouse Inalámbrico Pro",
    "precio": 19.99,
    "tipoIVA": 12,
    "stock": 45,
    "unidadMedida": "Unidad"
  }'
```

**Respuestas:**
- `200 OK` - Actualizado exitosamente
- `404 Not Found` - Producto no existe
- `409 Conflict` - El nuevo código ya existe

#### Eliminar Producto (DELETE)
```bash
curl -X DELETE https://localhost:7001/api/productos/1
```

**Respuestas:**
- `204 No Content` - Eliminado exitosamente
- `404 Not Found` - Producto no existe

---

## 🗃️ Migraciones (EF Core)

### Comandos Principales

**Crear nueva migración:**
```powershell
dotnet ef migrations add NombreDeMigracion `
  -s .\SistemaFacturacionSRI.WebUI\SistemaFacturacionSRI.WebUI.csproj `
  -p .\SistemaFacturacionSRI.Infrastructure\SistemaFacturacionSRI.Infrastructure.csproj
```

**Aplicar migraciones:**
```powershell
dotnet ef database update `
  -s .\SistemaFacturacionSRI.WebUI\SistemaFacturacionSRI.WebUI.csproj `
  -p .\SistemaFacturacionSRI.Infrastructure\SistemaFacturacionSRI.Infrastructure.csproj
```

**Listar migraciones:**
```powershell
dotnet ef migrations list `
  -s .\SistemaFacturacionSRI.WebUI\SistemaFacturacionSRI.WebUI.csproj `
  -p .\SistemaFacturacionSRI.Infrastructure\SistemaFacturacionSRI.Infrastructure.csproj
```

**Revertir última migración:**
```powershell
dotnet ef migrations remove `
  -s .\SistemaFacturacionSRI.WebUI\SistemaFacturacionSRI.WebUI.csproj `
  -p .\SistemaFacturacionSRI.Infrastructure\SistemaFacturacionSRI.Infrastructure.csproj
```

### Migraciones Aplicadas

- ✅ `InitialCreate` - Tabla Productos
- ✅ `AddAuthBillingSchema` - Roles, Usuarios, Clientes, TiposIdentificacion, TiposIVA, Categorías, Lotes, Facturas, FacturaDetalles

---

# Esquemas XSD del SRI - Facturación Electrónica

## 📚 Descripción

Esta carpeta contiene los esquemas XSD oficiales del Servicio de Rentas Internas (SRI) de Ecuador para la validación de comprobantes electrónicos.

## 📁 Archivos

### factura_v1.1.0.xsd
Esquema principal para facturas electrónicas versión 1.1.0

**Elementos principales:**
- `<factura>` - Elemento raíz
- `<infoTributaria>` - Información del emisor
- `<infoFactura>` - Información de la transacción
- `<detalles>` - Productos/servicios
- `<infoAdicional>` - Campos personalizados (opcional)

### comun_v1.0.xsd
Tipos de datos comunes compartidos entre diferentes comprobantes

**Define:**
- Tipos básicos (RUC, cédula, fechas, importes)
- Enumeraciones (ambiente, tipo emisión, impuestos)
- Restricciones (longitudes, patrones, formatos)

## 🔍 Uso en el Sistema

Los esquemas XSD se utilizan para:

1. **Validación antes de firmar**: Verificar que el XML cumple con el estándar antes de aplicar la firma electrónica
2. **Detección temprana de errores**: Identificar problemas de formato antes de enviar al SRI
3. **Documentación**: Referencia de la estructura correcta de los comprobantes

## ⚠️ Importante

- Estos esquemas son proporcionados por el SRI y NO deben ser modificados
- Cualquier actualización debe obtenerse del portal oficial del SRI
- Versión actual: 1.1.0 (vigente desde 2015)

## 🌐 Referencias

- Portal SRI: https://www.sri.gob.ec/facturacion-electronica
- Ficha Técnica: Disponible en el portal del SRI
- Documentación Desarrolladores: https://www.sri.gob.ec/web/guest/documentacion-desarrolladores

---

## 👥 Equipo

| Integrante | Rol | Responsabilidades |
|------------|-----|-------------------|
| **Pedro Supe** | Arquitecto / Backend Lead | Arquitectura, servicios, repositorios |
| **Patricio Tisalema** | Database Engineer | Base de datos, migraciones, configuraciones |
| **Kerly Chicaiza** | Backend Developer | API Controllers, validaciones |
| **Melany Cevallos** | Frontend Developer | UI Blazor, componentes, UX |

### Metodología

- 📋 **Framework:** SCRUM
- 🏃 **Sprint Duration:** 12 días
- 🕐 **Daily Scrum:** 9:00 AM (15 minutos)
- 📅 **Sprint Review:** Final de cada sprint
- 🔄 **Retrospectiva:** Post sprint review

---

## 📚 Documentación

### Guías Principales

- 📖 **[GUIA_INSTALACION.md](GUIA_INSTALACION.md)** - Instalación paso a paso completa
- 🌿 **[GUIA_GIT_EQUIPO.md](GUIA_GIT_EQUIPO.md)** - Flujo de trabajo con Git
- 📋 **[SPRINT 1 - PLANIFICACION.pdf](SPRINT%201%20-%20PLANIFICACION.pdf)** - Plan del Sprint 1

### Recursos Externos

- [Documentación .NET](https://docs.microsoft.com/dotnet/)
- [Entity Framework Core](https://docs.microsoft.com/ef/core/)
- [Blazor](https://docs.microsoft.com/aspnet/core/blazor/)
- [Normativas SRI Ecuador](https://www.sri.gob.ec/)

---

## 🤝 Contribución

### Flujo de Trabajo (Git Flow)
```
main (producción - protegida)
  └── develop (desarrollo)
       ├── feature/pedro-backend
       ├── feature/patricio-database
       ├── feature/kerly-controllers
       └── feature/melany-frontend
```

### Proceso de Contribución

1. **Crear rama feature:**
```bash
   git checkout develop
   git pull origin develop
   git checkout -b feature/nombre-tarea
```

2. **Desarrollar y commitear:**
```bash
   git add .
   git commit -m "T-XX: Descripción clara de la tarea"
```

3. **Push y Pull Request:**
```bash
   git push origin feature/nombre-tarea
   # Crear PR en GitHub: feature/nombre-tarea → develop
```

4. **Code Review y Merge:**
   - Revisión por al menos un compañero
   - Merge a develop después de aprobación

**Consulta:** [GUIA_GIT_EQUIPO.md](GUIA_GIT_EQUIPO.md) para más detalles

---

## 🧪 Testing

### Ejecutar Tests (próximamente)
```bash
dotnet test
```

### Cobertura de Tests

- [ ] Unit Tests - Servicios
- [ ] Integration Tests - Repositorios
- [ ] E2E Tests - Frontend

---

## 🐛 Solución de Problemas

### Problemas Comunes

**1. Error de conexión a SQL Server**
```
A network-related error occurred while establishing a connection to SQL Server
```
**Solución:** Verificar que SQL Server esté corriendo y la cadena de conexión sea correcta.

**2. Cannot open database**
```
Cannot open database "SistemaFacturacionSRI" requested by the login
```
**Solución:** Ejecutar `dotnet ef database update`

**3. Puerto en uso**
```
Failed to bind to address https://localhost:7001
```
**Solución:** Usar otro puerto: `dotnet run --urls "https://localhost:7002"`

**Más soluciones:** Consulta [GUIA_INSTALACION.md](GUIA_INSTALACION.md) sección "Solución de Problemas"

---

## 📄 Licencia

**Proyecto Académico**  
Universidad Técnica de Ambato  
Facultad de Ingeniería en Sistemas, Electrónica e Industrial  
Carrera de Software  

**Materia:** Metodologías Ágiles  
**Periodo:** Agosto 2025 - Enero 2026  
**Semestre:** Cuarto "A"

---

## 📞 Contacto

**Canales de Comunicación:**
- WhatsApp del equipo
- GitHub Issues
- Daily Scrum (9:00 AM, Lunes a Sábado)

---

## ⭐ Agradecimientos

- Universidad Técnica de Ambato
- Docente de Metodologías Ágiles
- Comunidad .NET Ecuador

---

**Última actualización:** 28 de Noviembre de 2025  
**Versión:** 1.0.0  
**Estado del proyecto:** 🟢 En desarrollo activo (Sprint 1)

---

<div align="center">

**Hecho con ❤️ en Ecuador** 🇪🇨

[![Universidad Técnica de Ambato](https://img.shields.io/badge/UTA-Ambato-blue)](https://www.uta.edu.ec/)

</div>