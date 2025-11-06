# 📦 Guía de Instalación - Sistema de Facturación Electrónica SRI

> **Versión:** 1.0  
> **Fecha:** Octubre 2025  
> **Sprint:** 1  
> **Equipo:** Pedro Supe, Patricio Tisalema, Kerly Chicaiza, Melany Cevallos

---

## 📋 Tabla de Contenidos

1. [Requisitos Previos](#requisitos-previos)
2. [Instalación de Herramientas](#instalación-de-herramientas)
3. [Configuración del Proyecto](#configuración-del-proyecto)
4. [Configuración de Base de Datos](#configuración-de-base-de-datos)
5. [Ejecución del Proyecto](#ejecución-del-proyecto)
6. [Verificación de la Instalación](#verificación-de-la-instalación)
7. [Solución de Problemas](#solución-de-problemas)
8. [Estructura del Proyecto](#estructura-del-proyecto)

---

## 🔧 Requisitos Previos

### Sistema Operativo
- **Windows 10/11** (64-bit)
- Al menos **8GB de RAM**
- **10GB de espacio libre** en disco

### Conocimientos Básicos
- Uso de línea de comandos (CMD/PowerShell)
- Conceptos básicos de Git
- Conocimientos básicos de C# (recomendado)

---

## 📥 Instalación de Herramientas

### 1. Instalar .NET 8 SDK

**Verificar si ya está instalado:**
```bash
dotnet --version
```

**Si no está instalado o la versión es menor a 8.0:**

1. Descargar desde: https://dotnet.microsoft.com/download/dotnet/8.0
2. Ejecutar el instalador
3. Verificar instalación:
```bash
   dotnet --version
   # Debe mostrar: 8.0.xxx
```

---

### 2. Instalar SQL Server Express 2022

**Paso a paso:**

1. Descargar desde: https://www.microsoft.com/es-es/sql-server/sql-server-downloads
2. Seleccionar **Express** (gratuita)
3. Ejecutar el instalador
4. Seleccionar instalación **"Básica"**
5. Aceptar términos de licencia
6. Esperar la instalación (5-10 minutos)

**Anotar la información de conexión:**
- **Nombre del servidor:** `localhost\SQLEXPRESS`
- **Autenticación:** Windows Authentication

**Verificar instalación:**
```bash
sqlcmd -S localhost\SQLEXPRESS -E -Q "SELECT @@VERSION"
```

---

### 3. Instalar SQL Server Management Studio (SSMS)

**Opcional pero recomendado para administrar la BD:**

1. Descargar: https://aka.ms/ssmsfullsetup
2. Ejecutar instalador
3. Esperar instalación (5-10 minutos)
4. Abrir SSMS y conectar a `localhost\SQLEXPRESS`

---

### 4. Instalar Visual Studio Code

1. Descargar: https://code.visualstudio.com/
2. Ejecutar instalador
3. Durante instalación, marcar:
   - ✅ Agregar "Abrir con Code" al menú contextual
   - ✅ Agregar a PATH

**Extensiones recomendadas para VS Code:**

Abrir VS Code y instalar (Ctrl + Shift + X):

- **C# Dev Kit** (Microsoft) - Esencial para C#
- **C#** (Microsoft) - IntelliSense y debugging
- **NuGet Package Manager** - Gestión de paquetes
- **GitLens** - Mejora la integración con Git
- **Prettier** - Formateo de código
- **Material Icon Theme** - Iconos bonitos (opcional)

---

### 5. Instalar Git

**Verificar si ya está instalado:**
```bash
git --version
```

**Si no está instalado:**

1. Descargar: https://git-scm.com/download/win
2. Ejecutar instalador con opciones por defecto
3. Verificar:
```bash
   git --version
```

**Configurar Git (primera vez):**
```bash
git config --global user.name "Tu Nombre Completo"
git config --global user.email "tuemail@ejemplo.com"
```

---

### 6. Instalar Entity Framework Tools
```bash
dotnet tool install --global dotnet-ef
```

**Verificar instalación:**
```bash
dotnet ef --version
```

---

## 📂 Configuración del Proyecto

### 1. Clonar el Repositorio
```bash
# Navegar a donde quieres guardar el proyecto
cd C:\workspace

# Clonar el repositorio
git clone https://github.com/TU_USUARIO/SistemaFacturacionSRI.git

# Entrar al directorio
cd SistemaFacturacionSRI
```

---

### 2. Cambiar a la rama develop
```bash
git checkout develop
git pull origin develop
```

---

### 3. Restaurar paquetes NuGet
```bash
dotnet restore
```

Este comando descarga todas las dependencias del proyecto:
- Entity Framework Core
- AutoMapper
- BCrypt.Net
- Etc.

---

### 4. Compilar el proyecto
```bash
dotnet build
```

**Resultado esperado:**
```
Compilación correcta.
    0 Advertencia(s)
    0 Errores
```

---

## 🗄️ Configuración de Base de Datos

### 1. Configurar cadena de conexión

Abrir el archivo:
```
SistemaFacturacionSRI.WebUI/appsettings.json
```

Verificar que tenga esta configuración:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost\\SQLEXPRESS;Database=FacturacionSRI;Trusted_Connection=True;TrustServerCertificate=True;"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*"
}
```

**⚠️ IMPORTANTE:** Si tu instancia de SQL Server tiene otro nombre, ajusta la cadena de conexión.

**Verificar nombre de tu instancia:**
```bash
sqlcmd -L
```

---

### 2. Crear la base de datos (Migraciones)

**Crear migración inicial:**
```bash
dotnet ef migrations add MigracionInicial --project SistemaFacturacionSRI.Infrastructure --startup-project SistemaFacturacionSRI.WebUI
```

**Aplicar migración (crear tablas):**
```bash
dotnet ef database update --project SistemaFacturacionSRI.Infrastructure --startup-project SistemaFacturacionSRI.WebUI
```

**¿Qué hace esto?**
- Crea la base de datos `FacturacionSRI` en SQL Server
- Crea las tablas: `Productos`, etc.
- Aplica las configuraciones de Fluent API

---

### 3. Verificar que la BD se creó

**Opción A: Con SSMS**
1. Abrir SQL Server Management Studio
2. Conectar a `localhost\SQLEXPRESS`
3. Expandir **Databases**
4. Deberías ver: **FacturacionSRI**
5. Expandir **Tables**
6. Deberías ver: **dbo.Productos**

**Opción B: Con comando**
```bash
sqlcmd -S localhost\SQLEXPRESS -E -Q "SELECT name FROM sys.databases WHERE name = 'FacturacionSRI'"
```

---

## 🚀 Ejecución del Proyecto

### 1. Ejecutar el proyecto
```bash
dotnet run --project SistemaFacturacionSRI.WebUI
```

**O con hot-reload (recarga automática):**
```bash
dotnet watch run --project SistemaFacturacionSRI.WebUI
```

---

### 2. Abrir en el navegador

El proyecto estará disponible en:
```
https://localhost:7001
```

O la URL que muestre en la terminal:
```
Now listening on: https://localhost:XXXX
```

---

### 3. Probar el sistema

**En el navegador:**
1. Navega a "Productos" (si existe en el menú)
2. Prueba crear un producto de prueba
3. Verifica que se guarde en la base de datos

---

## ✅ Verificación de la Instalación

### Checklist de verificación

- [ ] .NET 8 SDK instalado (`dotnet --version`)
- [ ] SQL Server corriendo (`sqlcmd -S localhost\SQLEXPRESS -E`)
- [ ] Repositorio clonado y en rama `develop`
- [ ] Compilación exitosa (`dotnet build`)
- [ ] Base de datos creada (verificar en SSMS)
- [ ] Proyecto ejecutándose sin errores
- [ ] Navegador muestra la aplicación

---

## 🆘 Solución de Problemas

### Problema 1: "dotnet no se reconoce como comando"

**Causa:** .NET SDK no está en el PATH

**Solución:**
1. Reiniciar la terminal
2. O reiniciar Windows
3. Verificar instalación de .NET SDK

---

### Problema 2: "Cannot open database 'FacturacionSRI'"

**Causa:** La migración no se aplicó

**Solución:**
```bash
dotnet ef database update --project SistemaFacturacionSRI.Infrastructure --startup-project SistemaFacturacionSRI.WebUI
```

---

### Problema 3: "A network-related error occurred while establishing a connection to SQL Server"

**Causa:** SQL Server no está corriendo o el nombre es incorrecto

**Solución 1:** Verificar nombre del servidor
```bash
sqlcmd -L
```

**Solución 2:** Iniciar servicio SQL Server
1. Abrir "Servicios" (services.msc)
2. Buscar "SQL Server (SQLEXPRESS)"
3. Click derecho → Iniciar

**Solución 3:** Ajustar cadena de conexión en `appsettings.json`

---

### Problema 4: Puerto en uso

**Error:**
```
Failed to bind to address https://localhost:7001
```

**Solución:** Ejecutar en otro puerto
```bash
dotnet run --project SistemaFacturacionSRI.WebUI --urls "https://localhost:7002"
```

---

### Problema 5: Error de compilación

**Solución:**
```bash
# Limpiar
dotnet clean

# Restaurar paquetes
dotnet restore

# Compilar nuevamente
dotnet build
```

---

### Problema 6: "No DbContext was found"

**Causa:** Falta configuración en `Program.cs`

**Solución:** Verificar que `Program.cs` tenga:
```csharp
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
```

---

## 📁 Estructura del Proyecto
```
SistemaFacturacionSRI/
│
├── SistemaFacturacionSRI.sln              # Solución principal
├── .gitignore                              # Archivos ignorados por Git
├── README.md                               # Documentación general
├── GUIA_INSTALACION.md                     # Este archivo
├── GUIA_GIT_EQUIPO.md                      # Guía de Git
│
├── SistemaFacturacionSRI.Domain/          # Capa de Dominio (núcleo)
│   ├── Entities/
│   │   ├── EntidadBase.cs                  # Clase base para entidades
│   │   └── Producto.cs                     # Entidad Producto
│   ├── Enums/
│   │   ├── TipoIVA.cs                      # Enum tipos de IVA
│   │   └── TipoIVAExtensions.cs            # Métodos de extensión
│   └── SistemaFacturacionSRI.Domain.csproj
│
├── SistemaFacturacionSRI.Application/     # Capa de Aplicación (lógica)
│   ├── DTOs/
│   │   └── Producto/
│   │       ├── ProductoDto.cs
│   │       ├── CrearProductoDto.cs
│   │       └── ActualizarProductoDto.cs
│   ├── Interfaces/
│   │   ├── Repositories/
│   │   │   ├── IRepositoryBase.cs
│   │   │   └── IProductoRepository.cs
│   │   └── Services/
│   │       └── IProductoService.cs
│   ├── Mappings/
│   │   └── ProductoProfile.cs              # Configuración AutoMapper
│   ├── Services/
│   │   └── ProductoService.cs              # Lógica de negocio
│   └── SistemaFacturacionSRI.Application.csproj
│
├── SistemaFacturacionSRI.Infrastructure/  # Capa de Infraestructura (BD)
│   ├── Data/
│   │   ├── ApplicationDbContext.cs         # Contexto EF Core
│   │   └── Configurations/
│   │       └── ProductoConfiguration.cs    # Fluent API
│   ├── Repositories/
│   │   ├── RepositoryBase.cs
│   │   └── ProductoRepository.cs
│   └── SistemaFacturacionSRI.Infrastructure.csproj
│
└── SistemaFacturacionSRI.WebUI/           # Capa de Presentación (web)
    ├── Components/
    │   ├── Layout/                         # Layouts de la app
    │   └── Pages/                          # Páginas Blazor
    │       ├── Home.razor
    │       └── Productos/
    ├── wwwroot/                            # Archivos estáticos (CSS, JS)
    ├── appsettings.json                    # Configuración (cadena conexión)
    ├── Program.cs                          # Configuración de servicios
    └── SistemaFacturacionSRI.WebUI.csproj
```

---

## 🎯 Stack Tecnológico

### Backend
- **.NET 8.0** - Framework principal
- **C# 12** - Lenguaje de programación
- **Entity Framework Core 9.0** - ORM para base de datos
- **AutoMapper 12.0** - Mapeo objeto-objeto
- **BCrypt.Net 4.0** - Encriptación de contraseñas

### Frontend
- **Blazor Server** - Framework UI interactivo
- **Bootstrap 5** - Framework CSS
- **Razor Components** - Componentes reutilizables

### Base de Datos
- **SQL Server 2022 Express** - Motor de base de datos

### Herramientas de Desarrollo
- **Visual Studio Code** - Editor de código
- **Git** - Control de versiones
- **GitHub** - Repositorio remoto
- **SSMS** - Administración de BD

---

## 📚 Recursos Adicionales

### Documentación Oficial
- [.NET Documentation](https://docs.microsoft.com/dotnet/)
- [Entity Framework Core](https://docs.microsoft.com/ef/core/)
- [Blazor Documentation](https://docs.microsoft.com/aspnet/core/blazor/)
- [SQL Server Documentation](https://docs.microsoft.com/sql/)

### Tutoriales Recomendados
- [Blazor Tutorial](https://dotnet.microsoft.com/learn/aspnet/blazor-tutorial/intro)
- [EF Core Getting Started](https://docs.microsoft.com/ef/core/get-started/)
- [Git Basics](https://git-scm.com/book/es/v2)

---

## 👥 Soporte

### Equipo de Desarrollo

**Contacto en caso de problemas:**
- **Pedro Supe** - Arquitectura y Backend
- **Patricio Tisalema** - Base de Datos
- **Kerly Chicaiza** - API Controllers
- **Melany Cevallos** - Frontend

**Canales de comunicación:**
- WhatsApp del equipo
- Daily Scrum (9:00 AM)
- GitHub Issues

---

## 📝 Notas Importantes

### Seguridad
- ⚠️ **NO SUBIR** archivos `appsettings.json` con contraseñas reales a GitHub
- ⚠️ Usar `appsettings.Development.json` para desarrollo local
- ⚠️ La cadena de conexión actual usa Windows Authentication (sin contraseña)

### Buenas Prácticas
- ✅ Hacer commits frecuentes (cada 1-2 horas)
- ✅ Actualizar desde develop antes de trabajar (`git pull`)
- ✅ Probar localmente antes de hacer push
- ✅ Escribir mensajes de commit descriptivos

### Datos de Prueba
- El sistema inicia sin datos
- Necesitarás crear productos manualmente para pruebas
- (Futuro: Agregar seed data en las migraciones)

---

## 🔄 Actualizar el Proyecto

Cuando hay nuevos cambios del equipo:
```bash
# Ir a develop
git checkout develop

# Descargar cambios
git pull origin develop

# Si hay cambios en la BD, aplicar migraciones
dotnet ef database update --project SistemaFacturacionSRI.Infrastructure --startup-project SistemaFacturacionSRI.WebUI

# Compilar
dotnet build

# Ejecutar
dotnet run --project SistemaFacturacionSRI.WebUI
```

---

## ✨ ¡Listo!

Si llegaste hasta aquí y todo funciona:

🎉 **¡Felicitaciones!** El ambiente de desarrollo está configurado correctamente.

Ahora puedes:
- Explorar el código
- Hacer cambios
- Crear nuevas funcionalidades
- Colaborar con el equipo

**¡A programar!** 💻🚀

---

**Última actualización:** 31 de Octubre de 2025  
**Versión del documento:** 1.0  
**Autor:** Pedro Supe