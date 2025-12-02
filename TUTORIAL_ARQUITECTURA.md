# 🎓 Tutorial: Arquitectura y Flujo del Sistema de Facturación SRI

> **Documento de capacitación técnica para el equipo de desarrollo**  
> **Presentado por:** Pedro Supe (Arquitecto de Software)  
> **Fecha:** Sprint 1 - Octubre 2025  
> **Duración estimada:** 2-3 horas

---

## 📋 Objetivos del Tutorial

Al finalizar este tutorial, cada miembro del equipo será capaz de:

1. ✅ Entender la arquitectura Onion y sus capas
2. ✅ Navegar por el código del proyecto con confianza
3. ✅ Crear nuevas funcionalidades siguiendo el patrón establecido
4. ✅ Comprender el flujo de datos de punta a punta
5. ✅ Resolver problemas comunes de forma autónoma

---

## 📚 Tabla de Contenidos

1. [¿Qué es Onion Architecture?](#1-qué-es-onion-architecture)
2. [Estructura del Proyecto](#2-estructura-del-proyecto)
3. [Flujo de Datos Completo](#3-flujo-de-datos-completo)
4. [Capa Domain (Núcleo)](#4-capa-domain-núcleo)
5. [Capa Application (Lógica de Negocio)](#5-capa-application-lógica-de-negocio)
6. [Capa Infrastructure (Implementación)](#6-capa-infrastructure-implementación)
7. [Capa WebUI (Presentación)](#7-capa-webui-presentación)
8. [Ejemplo Práctico: Crear un Producto](#8-ejemplo-práctico-crear-un-producto)
9. [Patrones de Diseño Utilizados](#9-patrones-de-diseño-utilizados)
10. [Ejercicios Prácticos](#10-ejercicios-prácticos)
11. [Buenas Prácticas](#11-buenas-prácticas)
12. [Preguntas Frecuentes](#12-preguntas-frecuentes)

---

## 1. ¿Qué es Onion Architecture?

### 🧅 Analogía de la Cebolla

Imagina una cebolla con capas concéntricas:
```
        ╔═════════════════════╗
        ║     WebUI (UI)      ║  ← Capa externa (puede cambiar)
        ╠═════════════════════╣
        ║  Infrastructure     ║  ← Implementaciones (BD, APIs)
        ╠═════════════════════╣
        ║    Application      ║  ← Lógica de negocio
        ╠═════════════════════╣
        ║   🎯 DOMAIN 🎯     ║  ← NÚCLEO (nunca cambia)
        ╚═════════════════════╝
```

### 🎯 Principio Fundamental

> **"Las dependencias fluyen HACIA ADENTRO, nunca hacia afuera"**

**Significado:**
- ✅ Application puede usar Domain
- ✅ Infrastructure puede usar Application y Domain
- ✅ WebUI puede usar Infrastructure, Application y Domain
- ❌ Domain NO puede usar ninguna otra capa
- ❌ Application NO puede usar Infrastructure

### 🤔 ¿Por qué Onion Architecture?

#### Arquitectura Tradicional (❌ Problemas):
```
UI → Business Logic → Data Access → Database
```

**Problema:** Si cambias la base de datos, afecta TODO el sistema.

#### Onion Architecture (✅ Solución):
```
UI → Infrastructure → Application → Domain
                         ↓
                     Database
```

**Ventaja:** Si cambias la BD, solo cambias Infrastructure. El resto queda intacto.

### 💡 Beneficios Reales

1. **Testeable:** Puedes probar la lógica sin base de datos
2. **Mantenible:** Cambios en una capa no afectan a otras
3. **Escalable:** Fácil agregar nuevas funcionalidades
4. **Independiente de frameworks:** No estás "casado" con EF Core o Blazor

---

## 2. Estructura del Proyecto

### 📂 Vista General
```
SistemaFacturacionSRI/
│
├── 🎯 Domain/                    ← Reglas de negocio puras
│   ├── Entities/                 ← ¿QUÉ es?
│   └── Enums/                    ← Valores constantes
│
├── 💼 Application/               ← ¿CÓMO usar Domain?
│   ├── DTOs/                     ← Objetos de transferencia
│   ├── Interfaces/               ← Contratos
│   ├── Services/                 ← Lógica de negocio
│   └── Mappings/                 ← Conversiones
│
├── 🔧 Infrastructure/            ← ¿DÓNDE guardamos?
│   ├── Data/                     ← Base de datos
│   └── Repositories/             ← Acceso a datos
│
└── 🌐 WebUI/                     ← ¿QUÉ ve el usuario?
    ├── Components/               ← UI Blazor
    ├── Pages/                    ← Páginas
    └── Program.cs                ← Configuración + API
```

### 🗺️ Mapa Mental de Dependencias
```
┌─────────────────────────────────────────────────────┐
│                      WebUI                          │
│  - Páginas Blazor                                   │
│  - Minimal APIs (/api/*)                            │
│  - Program.cs (DI, configuración)                   │
└────────────────┬────────────────────────────────────┘
                 │ depende de ↓
┌────────────────▼────────────────────────────────────┐
│                 Infrastructure                      │
│  - ApplicationDbContext                             │
│  - Repositorios (ProductoRepository)                │
│  - Configuraciones Fluent API                       │
└────────────────┬────────────────────────────────────┘
                 │ depende de ↓
┌────────────────▼────────────────────────────────────┐
│                  Application                        │
│  - Servicios (ProductoService)                      │
│  - DTOs (ProductoDto, CrearProductoDto)             │
│  - Interfaces (IProductoService, IProductoRepo)     │
│  - AutoMapper (ProductoProfile)                     │
└────────────────┬────────────────────────────────────┘
                 │ depende de ↓
┌────────────────▼────────────────────────────────────┐
│              🎯 DOMAIN (núcleo) 🎯                  │
│  - Entidades (Producto, EntidadBase)                │
│  - Enums (TipoIVA)                                  │
│  - NO TIENE DEPENDENCIAS EXTERNAS                   │
└─────────────────────────────────────────────────────┘
```

---

## 3. Flujo de Datos Completo

### 🔄 Ejemplo: Crear un Producto (POST)

Vamos a seguir el flujo completo desde que el usuario hace clic hasta que se guarda en la BD.
```
┌─────────────────────────────────────────────────────────────┐
│ 1. USUARIO (Frontend)                                       │
│    Click en "Guardar Producto"                              │
└────────────────────┬────────────────────────────────────────┘
                     ↓
┌────────────────────▼────────────────────────────────────────┐
│ 2. BLAZOR PAGE (WebUI)                                      │
│    FormularioProducto.razor                                 │
│    - Captura datos del formulario                           │
│    - Valida DataAnnotations                                 │
│    - Llama al servicio HTTP                                 │
└────────────────────┬────────────────────────────────────────┘
                     ↓
┌────────────────────▼────────────────────────────────────────┐
│ 3. MINIMAL API (WebUI/Program.cs)                           │
│    POST /api/productos                                      │
│    - Recibe CrearProductoDto                                │
│    - Valida modelo ([FromBody])                             │
│    - Inyecta IProductoService                               │
└────────────────────┬────────────────────────────────────────┘
                     ↓
┌────────────────────▼────────────────────────────────────────┐
│ 4. SERVICE (Application)                                    │
│    ProductoService.CrearAsync(dto)                          │
│    - Valida código único (lógica de negocio)                │
│    - Mapea DTO → Entidad (AutoMapper)                       │
│    - Llama al repositorio                                   │
└────────────────────┬────────────────────────────────────────┘
                     ↓
┌────────────────────▼────────────────────────────────────────┐
│ 5. REPOSITORY (Infrastructure)                              │
│    ProductoRepository.AgregarAsync(producto)                │
│    - Agrega entidad al DbSet                                │
│    - Llama a SaveChangesAsync                               │
└────────────────────┬────────────────────────────────────────┘
                     ↓
┌────────────────────▼────────────────────────────────────────┐
│ 6. DB CONTEXT (Infrastructure)                              │
│    ApplicationDbContext.SaveChanges()                       │
│    - Establece FechaCreacion = DateTime.Now                 │
│    - Establece Activo = true                                │
│    - Genera SQL INSERT                                      │
└────────────────────┬────────────────────────────────────────┘
                     ↓
┌────────────────────▼────────────────────────────────────────┐
│ 7. SQL SERVER (Base de Datos)                               │
│    INSERT INTO Productos (...) VALUES (...)                 │
│    - Genera Id (IDENTITY)                                   │
│    - Retorna producto con Id                                │
└────────────────────┬────────────────────────────────────────┘
                     ↓
┌────────────────────▼────────────────────────────────────────┐
│ 8. RESPUESTA (camino de vuelta)                             │
│    Repository → Service → API → Blazor → Usuario            │
│    - Mapea Entidad → ProductoDto                            │
│    - HTTP 201 Created                                       │
│    - Muestra mensaje de éxito                               │
└─────────────────────────────────────────────────────────────┘
```

---

## 4. Capa Domain (Núcleo)

### 🎯 Propósito

> **"Domain es el corazón del negocio. Contiene las reglas que NUNCA cambian."**

### 📍 Ubicación
```
SistemaFacturacionSRI.Domain/
├── Entities/
│   ├── EntidadBase.cs
│   └── Producto.cs
└── Enums/
    ├── TipoIVA.cs
    └── TipoIVAExtensions.cs
```

### 📘 Componentes Principales

#### 1. EntidadBase.cs

**¿Qué es?**  
Clase abstracta que todas las entidades heredan.

**¿Para qué?**  
Proporciona propiedades comunes a todas las tablas.
```csharp
public abstract class EntidadBase
{
    public int Id { get; set; }              // Clave primaria
    public DateTime FechaCreacion { get; set; }    // Auditoría
    public DateTime? FechaModificacion { get; set; }
    public bool Activo { get; set; } = true;      // Soft delete
}
```

**🤔 ¿Por qué abstracta?**  
No puedes hacer `new EntidadBase()`. Solo puedes heredar de ella.

**💡 Ventaja:**  
Si necesitas agregar una propiedad a TODAS las tablas (ej: `UsuarioCreacion`), la agregas aquí y TODAS la heredan automáticamente.

---

#### 2. Producto.cs

**¿Qué es?**  
Representa un producto en el sistema.

**Estructura:**
```csharp
public class Producto : EntidadBase  // Hereda Id, Fechas, Activo
{
    // Propiedades de datos (se guardan en BD)
    public string Codigo { get; set; }
    public string Nombre { get; set; }
    public string? Descripcion { get; set; }
    public decimal Precio { get; set; }
    public TipoIVA TipoIVA { get; set; }
    public int Stock { get; set; }
    public string UnidadMedida { get; set; }
    
    // Propiedades calculadas (NO se guardan en BD)
    public decimal ValorIVA => TipoIVA.CalcularIVA(Precio);
    public decimal PrecioConIVA => TipoIVA.CalcularTotal(Precio);
    public bool TieneStock => Stock > 0;
    public decimal ValorInventario => Stock * Precio;
}
```

**🎨 Propiedades Calculadas (con `=>`)**
```csharp
public decimal ValorIVA => TipoIVA.CalcularIVA(Precio);
```

**Significado:**
- `=>`: Lambda, es una propiedad de solo lectura
- Se calcula cada vez que se accede
- NO se guarda en la base de datos
- Siempre está actualizada

**Ejemplo:**
```csharp
var producto = new Producto { Precio = 100, TipoIVA = TipoIVA.IVA_15 };
Console.WriteLine(producto.ValorIVA);  // 15 (se calcula automáticamente)
```

---

#### 3. TipoIVA.cs (Enum)

**¿Qué es?**  
Lista de valores constantes para tipos de IVA.
```csharp
public enum TipoIVA
{
    IVA_0 = 0,    // 0%
    IVA_15 = 15   // 15%
}
```

**¿Por qué enum?**  
- ✅ Solo valores válidos (0, 12, 15)
- ✅ IntelliSense en el IDE
- ✅ Imposible equivocarse con strings ("12%", "doce", etc.)
- ✅ Se guarda como INT en la BD (eficiente)

---

#### 4. TipoIVAExtensions.cs

**¿Qué son métodos de extensión?**  
Agregan métodos a tipos existentes sin modificarlos.
```csharp
public static class TipoIVAExtensions
{
    public static decimal ObtenerPorcentaje(this TipoIVA tipoIVA)
    {
        return (decimal)tipoIVA / 100;
    }
    
    public static decimal CalcularIVA(this TipoIVA tipoIVA, decimal monto)
    {
        return monto * tipoIVA.ObtenerPorcentaje();
    }
    
    public static string ObtenerDescripcion(this TipoIVA tipoIVA)
    {
        return tipoIVA switch
        {
            TipoIVA.IVA_0 => "IVA 0%",
            TipoIVA.IVA_15 => "IVA 15%",
            _ => "IVA Desconocido"
        };
    }
}
```

**Uso:**
```csharp
TipoIVA iva = TipoIVA.IVA_15;
decimal porcentaje = iva.ObtenerPorcentaje();  // 0.15
decimal valor = iva.CalcularIVA(100);          // 15
string desc = iva.ObtenerDescripcion();        // "IVA 15%"
```

**🔑 Palabra clave `this`**  
En el primer parámetro indica que es método de extensión.

---

### 🚫 Reglas de Domain

**Domain NO puede:**
- ❌ Referenciar Entity Framework
- ❌ Referenciar ASP.NET Core
- ❌ Tener lógica de BD o HTTP
- ❌ Depender de otras capas

**Domain SÍ puede:**
- ✅ Tener lógica de negocio pura
- ✅ Validaciones básicas
- ✅ Propiedades calculadas
- ✅ Métodos de la entidad

---

## 5. Capa Application (Lógica de Negocio)

### 💼 Propósito

> **"Application orquesta Domain. Define CÓMO usar las entidades."**

### 📍 Ubicación
```
SistemaFacturacionSRI.Application/
├── DTOs/
│   └── Producto/
│       ├── ProductoDto.cs
│       ├── CrearProductoDto.cs
│       └── ActualizarProductoDto.cs
├── Interfaces/
│   ├── Repositories/
│   │   └── IProductoRepository.cs
│   └── Services/
│       └── IProductoService.cs
├── Mappings/
│   └── ProductoProfile.cs
└── Services/
    └── ProductoService.cs
```

---

### 📘 Componentes Principales

#### 1. DTOs (Data Transfer Objects)

**¿Qué son?**  
Objetos que se transfieren entre capas (especialmente API ↔ Cliente).

**¿Por qué NO usar la entidad directamente?**

❌ **Problemas de usar Producto (entidad) en API:**
```csharp
// API devuelve Producto directamente
public IActionResult Get() => Ok(producto);

// JSON que se envía:
{
  "id": 1,
  "codigo": "PROD-001",
  "nombre": "Laptop",
  "activo": true,           // ❌ Campo interno expuesto
  "fechaCreacion": "...",
  "fechaModificacion": "..."
}
```

**Problemas:**
1. Expone campos internos (`Activo`, fechas de auditoría)
2. No puedes controlar qué se muestra
3. Cambios en la entidad rompen la API

✅ **Solución con DTOs:**
```csharp
// API devuelve ProductoDto
public IActionResult Get() => Ok(productoDto);

// JSON que se envía:
{
  "id": 1,
  "codigo": "PROD-001",
  "nombre": "Laptop",
  "precio": 1000,
  "precioConIVA": 1120      // ✅ Ya calculado
}
```

**Ventajas:**
1. Control total de qué se expone
2. API estable (cambios internos no afectan)
3. Diferentes DTOs para diferentes operaciones

---

**Tipos de DTOs:**

**a) ProductoDto (para GET - lectura)**
```csharp
public class ProductoDto
{
    public int Id { get; set; }
    public string Codigo { get; set; }
    public string Nombre { get; set; }
    public decimal Precio { get; set; }
    public decimal PrecioConIVA { get; set; }  // Ya calculado
    public string TipoIVADescripcion { get; set; }  // "IVA 12%"
    // ... más campos para mostrar
}
```

**b) CrearProductoDto (para POST - crear)**
```csharp
public class CrearProductoDto
{
    [Required]
    public string Codigo { get; set; }
    
    [Required]
    public string Nombre { get; set; }
    
    [Range(0.01, double.MaxValue)]
    public decimal Precio { get; set; }
    
    // SIN Id (se genera automáticamente)
    // SIN fechas (se establecen automáticamente)
}
```

**c) ActualizarProductoDto (para PUT - actualizar)**
```csharp
public class ActualizarProductoDto
{
    [Required]
    public int Id { get; set; }  // SÍ tiene Id (para saber cuál actualizar)
    
    [Required]
    public string Codigo { get; set; }
    
    // ... demás campos editables
}
```

---

#### 2. Interfaces (Contratos)

**¿Qué son?**  
Definen QUÉ métodos debe tener una clase, sin implementación.

**IProductoService.cs**
```csharp
public interface IProductoService
{
    Task<ProductoDto> CrearAsync(CrearProductoDto dto);
    Task<IEnumerable<ProductoDto>> ObtenerTodosAsync();
    Task<ProductoDto?> ObtenerPorIdAsync(int id);
    // ... más métodos
}
```

**¿Por qué interfaces?**

**Sin interfaz (❌ Acoplamiento):**
```csharp
public class ProductoController
{
    private readonly ProductoService _service;  // Acoplado a implementación
    
    public ProductoController(ProductoService service)
    {
        _service = service;
    }
}
```

**Con interfaz (✅ Desacoplamiento):**
```csharp
public class ProductoController
{
    private readonly IProductoService _service;  // Acoplado a contrato
    
    public ProductoController(IProductoService service)
    {
        _service = service;
    }
}
```

**Ventajas:**
- ✅ Puedes cambiar la implementación sin tocar el controller
- ✅ Facilita el testing (mocks)
- ✅ Cumple con principio de Inversión de Dependencias (SOLID)

---

#### 3. AutoMapper (Mappings)

**¿Qué es?**  
Biblioteca que convierte automáticamente entre objetos.

**Sin AutoMapper (❌ Tedioso):**
```csharp
var dto = new ProductoDto
{
    Id = producto.Id,
    Codigo = producto.Codigo,
    Nombre = producto.Nombre,
    Precio = producto.Precio,
    Stock = producto.Stock,
    UnidadMedida = producto.UnidadMedida,
    TipoIVA = producto.TipoIVA,
    ValorIVA = producto.ValorIVA,
    PrecioConIVA = producto.PrecioConIVA,
    // ... 20 líneas más
};
```

**Con AutoMapper (✅ Automático):**
```csharp
var dto = _mapper.Map<ProductoDto>(producto);  // ¡1 línea!
```

**ProductoProfile.cs**
```csharp
public class ProductoProfile : Profile
{
    public ProductoProfile()
    {
        // Entidad → DTO (para GET)
        CreateMap<Producto, ProductoDto>()
            .ForMember(dest => dest.TipoIVADescripcion, 
                opt => opt.MapFrom(src => src.TipoIVA.ObtenerDescripcion()));
        
        // DTO → Entidad (para POST)
        CreateMap<CrearProductoDto, Producto>()
            .ForMember(dest => dest.Id, opt => opt.Ignore());
    }
}
```

**¿Cómo funciona?**
1. AutoMapper escanea las propiedades de ambos objetos
2. Si tienen el mismo nombre → Copia automáticamente
3. Si necesitas lógica custom → `ForMember()`

---

#### 4. Services (ProductoService.cs)

**¿Qué es?**  
Contiene la lógica de negocio.

**Estructura:**
```csharp
public class ProductoService : IProductoService
{
    private readonly IProductoRepository _repository;
    private readonly IMapper _mapper;
    
    public ProductoService(
        IProductoRepository repository,
        IMapper mapper)
    {
        _repository = repository;
        _mapper = mapper;
    }
    
    public async Task<ProductoDto> CrearAsync(CrearProductoDto dto)
    {
        // 1. VALIDAR (lógica de negocio)
        var existe = await _repository.ExisteAsync(p => p.Codigo == dto.Codigo);
        if (existe)
            throw new InvalidOperationException("Código duplicado");
        
        // 2. MAPEAR (DTO → Entidad)
        var producto = _mapper.Map<Producto>(dto);
        
        // 3. GUARDAR (usar repositorio)
        var creado = await _repository.AgregarAsync(producto);
        
        // 4. RETORNAR (Entidad → DTO)
        return _mapper.Map<ProductoDto>(creado);
    }
}
```

**Responsabilidades del Service:**
- ✅ Validaciones de negocio
- ✅ Orquestar operaciones
- ✅ Coordinar entre repositorios
- ❌ NO accede a BD directamente (usa repositorio)
- ❌ NO maneja HTTP (eso es del controller)

---

## 6. Capa Infrastructure (Implementación)

### 🔧 Propósito

> **"Infrastructure implementa las interfaces de Application. Aquí vive la BD."**

### 📍 Ubicación
```
SistemaFacturacionSRI.Infrastructure/
├── Data/
│   ├── ApplicationDbContext.cs
│   └── Configurations/
│       └── ProductoConfiguration.cs
└── Repositories/
    ├── RepositoryBase.cs
    └── ProductoRepository.cs
```

---

### 📘 Componentes Principales

#### 1. ApplicationDbContext.cs

**¿Qué es?**  
La puerta de enlace entre C# y SQL Server.
```csharp
public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }
    
    // DbSets = Tablas
    public DbSet<Producto> Productos { get; set; }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Configurar tablas con Fluent API
        modelBuilder.ApplyConfiguration(new ProductoConfiguration());
    }
    
    public override int SaveChanges()
    {
        // Establecer fechas automáticamente
        var entradas = ChangeTracker.Entries<EntidadBase>();
        
        foreach (var entrada in entradas)
        {
            if (entrada.State == EntityState.Added)
            {
                entrada.Entity.FechaCreacion = DateTime.Now;
                entrada.Entity.Activo = true;
            }
            else if (entrada.State == EntityState.Modified)
            {
                entrada.Entity.FechaModificacion = DateTime.Now;
            }
        }
        
        return base.SaveChanges();
    }
}
```

**🔑 Conceptos clave:**

**DbSet<Producto>**
- Representa la tabla `Productos` en SQL Server
- Permite hacer consultas LINQ: `context.Productos.Where(p => p.Precio > 100)`

**OnModelCreating**
- Se ejecuta UNA vez cuando EF Core construye el modelo
- Aquí configuramos las tablas con Fluent API

**SaveChanges**
- Se ejecuta CADA vez que guardas cambios
- Aquí establecemos fechas de auditoría automáticamente

---

#### 2. ProductoConfiguration.cs (Fluent API)

**¿Qué es?**  
Configuración de cómo `Producto` se mapea a la tabla SQL.
```csharp
public class ProductoConfiguration : IEntityTypeConfiguration<Producto>
{
    public void Configure(EntityTypeBuilder<Producto> builder)
    {
        // Nombre de la tabla
        builder.ToTable("Productos");
        
        // Clave primaria
        builder.HasKey(p => p.Id);
        
        // Configurar columna Codigo
        builder.Property(p => p.Codigo)
            .IsRequired()                // NOT NULL
            .HasMaxLength(50)            // NVARCHAR(50)
            .HasColumnType("NVARCHAR");
        
        // Índice único en Codigo
        builder.HasIndex(p => p.Codigo)
            .IsUnique()                  // UNIQUE
            .HasDatabaseName("IX_Productos_Codigo");
        
        // Configurar Precio
        builder.Property(p => p.Precio)
            .HasColumnType("DECIMAL(18,2)");  // DECIMAL(18,2)
        
        // Enum TipoIVA se guarda como INT
        builder.Property(p => p.TipoIVA)
            .HasConversion<int>();
        
        // Ignorar propiedades calculadas
        builder.Ignore(p => p.ValorIVA);
        builder.Ignore(p => p.PrecioConIVA);
    }
}
```

**SQL generado:**
```sql
CREATE TABLE Productos (
    Id INT PRIMARY KEY IDENTITY(1,1),
    Codigo NVARCHAR(50) NOT NULL,
    Nombre NVARCHAR(200) NOT NULL,
    Precio DECIMAL(18,2) NOT NULL,
    TipoIVA INT NOT NULL,
    Stock INT NOT NULL,
    UnidadMedida NVARCHAR(20),
    FechaCreacion DATETIME NOT NULL,
    FechaModificacion DATETIME NULL,
    Activo BIT NOT NULL,
    CONSTRAINT IX_Productos_Codigo UNIQUE (Codigo)
);
```

---

#### 3. Repositorios

**¿Qué son?**  
Encapsulan el acceso a datos.

**RepositoryBase.cs (genérico)**
```csharp
public class RepositoryBase<T> : IRepositoryBase<T> where T : EntidadBase
{
    protected readonly ApplicationDbContext _context;
    protected readonly DbSet<T> _dbSet;
    
    public RepositoryBase(ApplicationDbContext context)
    {
        _context = context;
        __dbSet = context.Set<T>();
    }
    
    public virtual async Task<IEnumerable<T>> ObtenerTodosAsync()
    {
        return await _dbSet
            .Where(e => e.Activo)  // Solo activos
            .ToListAsync();
    }
    
    public virtual async Task<T?> ObtenerPorIdAsync(int id)
    {
        return await _dbSet
            .FirstOrDefaultAsync(e => e.Id == id && e.Activo);
    }
    
    public virtual async Task<T> AgregarAsync(T entidad)
    {
        await _dbSet.AddAsync(entidad);
        await _context.SaveChangesAsync();
        return entidad;
    }
    
    public virtual async Task ActualizarAsync(T entidad)
    {
        _dbSet.Update(entidad);
        await _context.SaveChangesAsync();
    }
    
    public virtual async Task EliminarAsync(int id)
    {
        var entidad = await ObtenerPorIdAsync(id);
        if (entidad != null)
        {
            entidad.Activo = false;  // Soft delete
            await ActualizarAsync(entidad);
        }
    }
}
```

**ProductoRepository.cs(específico)**
```csharp
public class ProductoRepository : RepositoryBase<Producto>, IProductoRepository
{
    public ProductoRepository(ApplicationDbContext context) : base(context)
    {
    }
    
    // Métodos específicos de Producto
    public async Task<Producto?> ObtenerPorCodigoAsync(string codigo)
    {
        return await _dbSet
            .FirstOrDefaultAsync(p => p.Codigo == codigo && p.Activo);
    }
    
    public async Task<IEnumerable<Producto>> BuscarPorNombreAsync(string nombre)
    {
        return await _dbSet
            .Where(p => p.Nombre.Contains(nombre) && p.Activo)
            .ToListAsync();
    }
}
```

**🎯 Patrón Repository - Ventajas:**

1. **Abstracción:** El Service no sabe que usa EF Core
2. **Testeable:** Fácil crear repositorios falsos para testing
3. **Reutilizable:** Métodos comunes en RepositoryBase
4. **Intercambiable:** Puedes cambiar EF Core por Dapper sin tocar el Service


## 7. Capa WebUI (Presentación)

### 🌐 Propósito

> **"WebUI es lo que el usuario VE y USA. Blazor + Minimal APIs."**

### 📍 Ubicación
```
SistemaFacturacionSRI.WebUI/
├── Components/
│   ├── Layout/
│   │   ├── MainLayout.razor
│   │   └── NavMenu.razor
│   └── Pages/
│       ├── Home.razor
│       └── Productos/
│           ├── ListaProductos.razor
│           └── FormularioProducto.razor
├── wwwroot/
│   ├── css/
│   └── js/
├── appsettings.json
└── Program.cs
```

## 📘 Componentes Principales
1. Program.cs (Configuración)
¿Qué es?
El punto de entrada de la aplicación. Configura todo.

```csharp
var builder = WebApplication.CreateBuilder(args);

// 1. CONFIGURAR SERVICIOS (Dependency Injection)

// Blazor
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// DbContext con SQL Server
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")));

// AutoMapper
builder.Services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());

// Repositorios
builder.Services.AddScoped<IProductoRepository, ProductoRepository>();

// Servicios
builder.Services.AddScoped<IProductoService, ProductoService>();

var app = builder.Build();

// 2. CONFIGURAR MIDDLEWARE (pipeline HTTP)

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAntiforgery();

// 3. MINIMAL APIS (endpoints REST)

app.MapPost("/api/productos", async (
    [FromBody] CrearProductoDto dto,
    IProductoService service) =>
{
    try
    {
        var resultado = await service.CrearAsync(dto);
        return Results.Created($"/api/productos/{resultado.Id}", resultado);
    }
    catch (InvalidOperationException ex)
    {
        return Results.Conflict(new { error = ex.Message });
    }
});

app.MapGet("/api/productos", async (IProductoService service) =>
{
    var productos = await service.ObtenerTodosAsync();
    return Results.Ok(productos);
});

app.MapGet("/api/productos/{id}", async (int id, IProductoService service) =>
{
    var producto = await service.ObtenerPorIdAsync(id);
    return producto is not null ? Results.Ok(producto) : Results.NotFound();
});

// 4. BLAZOR COMPONENTS
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode().AllowAnonymous();

app.Run();
```

### 🔑 Conceptos clave
### Dependency Injection (DI)
```csharp
builder.Services.AddScoped<IProductoService, ProductoService>();
```
- Registra servicios en el contenedor DI
- Scoped: Una instancia por petición HTTP
- Permite inyectar en constructores

### Minimal APIs
```csharp
app.MapPost("/api/productos", async (dto, service) => { ... });
```
- APIs REST sin controllers tradicionales
- Más ligeras y rápidas
- Ideales para microservicios

### 2. Blazor Components (.razor)
**¿Qué es Blazor?**
Framework para crear UIs interactivas con C# (sin JavaScript).
ListaProductos.razor
```csharp
@page "/productos"
@inject IProductoService ProductoService

<h3>Lista de Productos</h3>

@if (productos == null)
{
    <p>Cargando...</p>
}
else
{
    <table class="table">
        <thead>
            <tr>
                <th>Código</th>
                <th>Nombre</th>
                <th>Precio</th>
                <th>Stock</th>
                <th>Acciones</th>
            </tr>
        </thead>
        <tbody>
            @foreach (var producto in productos)
            {
                <tr>
                    <td>@producto.Codigo</td>
                    <td>@producto.Nombre</td>
                    <td>@producto.Precio.ToString("C")</td>
                    <td>@producto.Stock</td>
                    <td>
                        <button @onclick="() => Editar(producto.Id)">
                            Editar
                        </button>
                        <button @onclick="() => Eliminar(producto.Id)">
                            Eliminar
                        </button>
                    </td>
                </tr>
            }
        </tbody>
    </table>
}

@code {
    private IEnumerable<ProductoDto>? productos;
    
    protected override async Task OnInitializedAsync()
    {
        productos = await ProductoService.ObtenerTodosAsync();
    }
    
    private void Editar(int id)
    {
        // Navegar a formulario de edición
    }
    
    private async Task Eliminar(int id)
    {
        await ProductoService.EliminarAsync(id);
        productos = await ProductoService.ObtenerTodosAsync();
    }
}
```

### 🔑 Sintaxis Blazor

- **@page "/productos"** - Define la URL de la página
- **@inject IProductoService ProductoService** - Inyecta el servicio en la página
- **@if, @foreach** - Lógica C# en Razor
- **@onclick="() => Editar(id)"** - Event handler (como onclick en JS)
- **@code { ... }** - Bloque de código C#

### 8. Ejemplo Práctico: Crear un Producto
Vamos a seguir el flujo COMPLETO con código real.
🎬 Escenario
Usuario completa el formulario:

- Código: PROD-001
- Nombre: Laptop HP
- Precio: 1000
- Tipo IVA: 12%

Click en "Guardar"

### 📍 Paso 1: Frontend (Blazor)
**FormularioProducto.razor**

```csharp
@code {
    private CrearProductoDto modelo = new();
    
    private async Task GuardarAsync()
    {
        try
        {
            // Llamar al servicio HTTP
            var response = await Http.PostAsJsonAsync("/api/productos", modelo);
            
            if (response.IsSuccessStatusCode)
            {
                MostrarMensaje("Producto creado exitosamente");
                NavManager.NavigateTo("/productos");
            }
            else if (response.StatusCode == System.Net.HttpStatusCode.Conflict)
            {
                MostrarError("El código ya existe");
            }
        }
        catch (Exception ex)
        {
            MostrarError($"Error: {ex.Message}");
        }
    }
}
```
**HTTP Request generado**
```csharp
POST https://localhost:7001/api/productos
Content-Type: application/json

{
  "codigo": "PROD-001",
  "nombre": "Laptop HP",
  "precio": 1000,
  "tipoIVA": 12,
  "stock": 0,
  "unidadMedida": "Unidad"
}
```

### 📍 Paso 2: API Endpoint (Program.cs)
```csharp
app.MapPost("/api/productos", async (
    [FromBody] CrearProductoDto dto,
    IProductoService service) =>
{
    // 1. ASP.NET valida DataAnnotations automáticamente
    //    Si falla, retorna 400 Bad Request automáticamente
    
    try
    {
        // 2. Llamar al servicio
        var resultado = await service.CrearAsync(dto);
        
        // 3. Retornar 201 Created con el producto
        return Results.Created($"/api/productos/{resultado.Id}", resultado);
    }
    catch (InvalidOperationException ex)
    {
        // 4. Si hay error de negocio, retornar 409 Conflict
        return Results.Conflict(new { error = ex.Message });
    }
});
```

**¿Qué hace [FromBody]?**

- Lee el JSON del body HTTP
- Lo deserializa a CrearProductoDto
- Valida las [DataAnnotations]

### 📍 Paso 3: Service (ProductoService.cs)
```csharp
public async Task<ProductoDto> CrearAsync(CrearProductoDto dto)
{
    // 1. VALIDACIÓN DE NEGOCIO
    var codigoExiste = await _productoRepository.ExisteAsync(
        p => p.Codigo == dto.Codigo);
    
    if (codigoExiste)
    {
        throw new InvalidOperationException(
            "Ya existe un producto con el código 'PROD-001'");
    }
    
    // 2. MAPEAR DTO → ENTIDAD
    var producto = _mapper.Map<Producto>(dto);
    // Resultado:
    // producto = new Producto
    // {
    //     Codigo = "PROD-001",
    //     Nombre = "Laptop HP",
    //     Precio = 1000,
    //     TipoIVA = TipoIVA.IVA_15,
    //     Stock = 0,
    //     UnidadMedida = "Unidad"
    // };
    
    // 3. GUARDAR EN BD
    var productoCreado = await _productoRepository.AgregarAsync(producto);
    // Ahora producto tiene Id = 1 (generado por SQL Server)
    
    // 4. MAPEAR ENTIDAD → DTO
    var resultado = _mapper.Map<ProductoDto>(productoCreado);
    // Resultado:
    // {
    //     Id = 1,
    //     Codigo = "PROD-001",
    //     Nombre = "Laptop HP",
    //     Precio = 1000,
    //     TipoIVA = 12,
    //     TipoIVADescripcion = "IVA 12%",
    //     ValorIVA = 120,
    //     PrecioConIVA = 1120,
    //     Stock = 0,
    //     TieneStock = false
    // }
    
    return resultado;
}
```

### 📍 Paso 4: Repository (ProductoRepository.cs)
```csharp
public async Task<Producto> AgregarAsync(Producto entidad)
{
    // 1. Agregar al DbSet (en memoria, aún NO en BD)
    await _dbSet.AddAsync(entidad);
    
    // 2. Guardar cambios (ejecuta SQL INSERT)
    await _context.SaveChangesAsync();
    // Internamente llama a SaveChanges() override que establece fechas
    
    // 3. Retornar entidad con Id generado
    return entidad;
}
```

### 📍 Paso 5: DbContext (ApplicationDbContext.cs)
```csharp
public override async Task<int> SaveChangesAsync(...)
{
    // 1. Obtener entradas siendo modificadas
    var entradas = ChangeTracker.Entries<EntidadBase>();
    
    foreach (var entrada in entradas)
    {
        if (entrada.State == EntityState.Added)  // Es INSERT
        {
            // Establecer fechas automáticamente
            entrada.Entity.FechaCreacion = DateTime.Now;
            entrada.Entity.Activo = true;
        }
    }
    
    // 2. Ejecutar SQL
    return await base.SaveChangesAsync(cancellationToken);
}
```
**SQL generado por EF Core:**
```csharp
INSERT INTO Productos 
    (Codigo, Nombre, Descripcion, Precio, TipoIVA, Stock, 
     UnidadMedida, FechaCreacion, Activo)
VALUES 
    ('PROD-001', 'Laptop HP', NULL, 1000.00, 12, 0, 
     'Unidad', '2025-10-31 15:30:00', 1);

SELECT SCOPE_IDENTITY();  -- Obtener Id generado
```

### 📍 Paso 6: Respuesta al Cliente
**HTTP Response:**
```csharp
HTTP/1.1 201 Created
Location: /api/productos/1
Content-Type: application/json

{
  "id": 1,
  "codigo": "PROD-001",
  "nombre": "Laptop HP",
  "descripcion": null,
  "precio": 1000.00,
  "tipoIVA": 12,
  "tipoIVADescripcion": "IVA 12%",
  "stock": 0,
  "unidadMedida": "Unidad",
  "valorIVA": 120.00,
  "precioConIVA": 1120.00,
  "tieneStock": false,
  "valorInventario": 0.00,
  "fechaCreacion": "2025-10-31T15:30:00",
  "fechaModificacion": null
}
```

### 📍 Paso 7: Frontend actualiza UI
```csharp
if (response.IsSuccessStatusCode)
{
    var productoCreado = await response.Content.ReadFromJsonAsync<ProductoDto>();
    MostrarMensaje($"Producto {productoCreado.Codigo} creado exitosamente");
    NavManager.NavigateTo("/productos");
}
```
### 9. Patrones de Diseño Utilizados
🎨 Patrones Implementados
**1. Repository Pattern**
¿Qué es?
Abstracción del acceso a datos.
Ventaja:
- Puedes cambiar de EF Core a Dapper sin tocar el Service
- Fácil de testear (mocks)

```csharp
// Service NO sabe que usa EF Core
public class ProductoService
{
    private readonly IProductoRepository _repo;  // Interfaz, no implementación
}
```

**2. Dependency Injection (DI)**
¿Qué es?
Las clases reciben sus dependencias por constructor.
Sin DI (❌ Acoplamiento):
```csharp
public class ProductoService
{
    public ProductoService()
    {
        _repository = new ProductoRepository();  // Acoplado
    }
}
```

**Con DI (✅ Desacoplamiento):**
```csharp
public class ProductoService
{
    public ProductoService(IProductoRepository repository)
    {
        _repository = repository;  // Inyectado
    }
}
```

**3. DTO Pattern**
¿Qué es?
Objetos para transferir datos entre capas.
Ventaja:
- Desacoplamiento entre API y BD
- Control de qué se expone

**4. Unit of Work (implícito en DbContext)**
¿Qué es?
Agrupa múltiples operaciones en una transacción.
```csharp
// Todo o nada (transaction)
context.Productos.Add(producto);
context.Categorias.Add(categoria);
await context.SaveChangesAsync();  // Ambos o ninguno
```

**5. Specification Pattern (parcial en repositorios)**
¿Qué es?
Encapsular consultas complejas.
```csharp
var activos = await _repo.BuscarAsync(p => p.Activo && p.Stock > 0);
```

### 10. Ejercicios Prácticos
**🏋️ Ejercicio 1: Crear entidad Cliente**
Objetivo: Aplicar lo aprendido creando una nueva entidad.
Pasos:
**1. Domain/Entities/Cliente.cs**
```csharp
public class Cliente : EntidadBase
{
    public string Cedula { get; set; }
    public string Nombres { get; set; }
    public string Apellidos { get; set; }
    public string Email { get; set; }
    public string Telefono { get; set; }
}
```

**2. Application/DTOs/Cliente/ClienteDto.cs**
```csharp
public class ClienteDto
{
    public int Id { get; set; }
    public string Cedula { get; set; }
    public string NombreCompleto { get; set; }  // Nombres + Apellidos
    public string Email { get; set; }
}
```

**3. Infrastructure/Configurations/ClienteConfiguration.cs**
```csharp
public class ClienteConfiguration : IEntityTypeConfiguration<Cliente>
{
    public void Configure(EntityTypeBuilder<Cliente> builder)
    {
        builder.ToTable("Clientes");
        builder.HasKey(c => c.Id);
        
        builder.Property(c => c.Cedula)
            .IsRequired()
            .HasMaxLength(13);
        
        builder.HasIndex(c => c.Cedula).IsUnique();
    }
}
```

**4. Crear migración y aplicar**
```csharp
dotnet ef migrations add AgregarCliente
dotnet ef database update
```

**🏋️ Ejercicio 2: Agregar búsqueda por precio**
Objetivo: Extender ProductoRepository
Tarea:
1. Agregar método en IProductoRepository:
```csharp
Task<IEnumerable<Producto>> BuscarPorRangoPrecioAsync(decimal min, decimal max);
```

2. Implementar en ProductoRepository:
```csharp
ppublic async Task<IEnumerable<Producto>> BuscarPorRangoPrecioAsync(
    decimal min, decimal max)
{
    return await _dbSet
        .Where(p => p.Precio >= min && p.Precio <= max && p.Activo)
        .OrderBy(p => p.Precio)
        .ToListAsync();
}
```

3. Agregar endpoint en Program.cs:
```csharp
app.MapGet("/api/productos/buscar", async (
    decimal? min,
    decimal? max,
    IProductoRepository repo) =>
{
    var productos = await repo.BuscarPorRangoPrecioAsync(min ?? 0, max ?? decimal.MaxValue);
    return Results.Ok(productos);
});
```

**🏋️ Ejercicio 3: Agregar validación personalizada**
Objetivo: Validar que el precio no sea múltiplo de 100
Tarea:
Crear CrearProductoDto custom validation:
```csharp
public class CrearProductoDto : IValidatableObject
{
    [Range(0.01, double.MaxValue)]
    public decimal Precio { get; set; }
    
    public IEnumerable<ValidationResult> Validate(ValidationContext context)
    {
        if (Precio % 100 == 0)
        {
            yield return new ValidationResult(
                "El precio no puede ser múltiplo exacto de 100",
                new[] { nameof(Precio) });
        }
    }
}
```
### 11. Buenas Prácticas
✅ DOs (Hacer)
**1. Nombres descriptivos**

```csharp
// ✅ BIEN
public async Task<ProductoDto> ObtenerPorCodigoAsync(string codigo)

// ❌ MAL
public async Task<ProductoDto> Get(string c)
```

**2. Usar async/await**
```csharp
// ✅ BIEN
public async Task<IEnumerable<Producto>> ObtenerTodosAsync()

// ❌ MAL
public IEnumerable<Producto> ObtenerTodos()  // Síncrono
```
**3. Validar en múltiples niveles**
- Frontend: DataAnnotations
- Service: Lógica de negocio
- BD: Constraints

**4. Usar DTOs para APIs**
```csharp
// ✅ BIEN
public IActionResult Get() => Ok(_mapper.Map<ProductoDto>(producto));

// ❌ MAL
public IActionResult Get() => Ok(producto);  // Expone entidad
```

**5. Commits frecuentes**
```csharp
# ✅ BIEN
git commit -m "T-19: Implementado método CrearAsync en ProductoService"

# ❌ MAL
git commit -m "cambios"
```

### ❌ DON'Ts (No hacer)
**1. NO hacer consultas en Domain**
```csharp
// ❌ MAL - Domain no debe tener acceso a BD
public class Producto : EntidadBase
{
    public List<Producto> ObtenerRelacionados()
    {
        return context.Productos.Where(...).ToList();  // ¡NO!
    }
}
```

**2. NO usar entidades en controllers**
```csharp
// ❌ MAL
public IActionResult Post([FromBody] Producto producto)

// ✅ BIEN
public IActionResult Post([FromBody] CrearProductoDto dto)
```

**3. NO poner lógica de negocio en controllers**
```csharp
// ❌ MAL
public async Task<IActionResult> Post(CrearProductoDto dto)
{
    var existe = context.Productos.Any(p => p.Codigo == dto.Codigo);
    if (existe) return Conflict();
    // ... más lógica
}

// ✅ BIEN
public async Task<IActionResult> Post(CrearProductoDto dto)
{
    var resultado = await _service.CrearAsync(dto);  // Service maneja lógica
    return Ok(resultado);
}
```

**4. NO usar Select * innecesariamente**
```csharp
// ❌ MAL
var productos = await _context.Productos.ToListAsync();  // Trae TODO

// ✅ BIEN
var productos = await _context.Productos
    .Where(p => p.Activo)
    .Select(p => new { p.Id, p.Nombre, p.Precio })
    .ToListAsync();
```

**5. NO hacer commits gigantes**
```csharp
# ❌ MAL
git add .
git commit -m "Sprint 1 completo"  # 50 archivos

# ✅ BIEN
git commit -m "T-19: Service Crear" # 2-3 archivos por commit
```

### 12. Preguntas Frecuentes
**❓ ¿Por qué tantas capas? ¿No es complicado?**
R: Al principio parece más trabajo, pero:

Mantenibilidad: Cambios en BD no afectan lógica de negocio
Testeable: Puedes probar cada capa independientemente
Escalable: Fácil agregar features sin romper lo existente
Trabajo en equipo: Cada uno puede trabajar en su capa sin conflictos

**❓ ¿Cuándo usar DTO y cuándo Entidad?**
R:

Entidad: Solo dentro de Application e Infrastructure
DTO: Para comunicación con el exterior (APIs, frontend)

Frontend ↔ DTO ↔ API ↔ Service ↔ Repository ↔ Entidad ↔ BD

**❓ ¿Por qué async/await en todo?**
R:

No bloquea hilos: Mientras espera la BD, el servidor puede atender otras peticiones
Escalabilidad: Más peticiones simultáneas con los mismos recursos
Estándar: Todas las APIs modernas son asíncronas

**❓ ¿Qué es LINQ y por qué usarlo?**
R: Language Integrated Query - Consultas tipo SQL en C#
csharp// LINQ (C#)
var productos = await _context.Productos
    .Where(p => p.Precio > 100)
    .OrderBy(p => p.Nombre)
    .ToListAsync();

// Se convierte a SQL:
// SELECT * FROM Productos 
// WHERE Precio > 100 
// ORDER BY Nombre
Ventajas:

IntelliSense (autocomplete)
Type-safe (errores en compilación, no runtime)
Legible

**❓ ¿Cuál es la diferencia entre Include y Select?**
R:
Include (Eager Loading):
csharpvar facturas = await _context.Facturas
    .Include(f => f.Detalles)  // JOIN en SQL
    .ToListAsync();

Trae datos relacionados
Puede ser ineficiente si no necesitas todo

Select (Projection):
csharpvar facturas = await _context.Facturas
    .Select(f => new { f.Id, f.Numero })  // Solo columnas específicas
    .ToListAsync();

Solo trae columnas necesarias
Más eficiente

**❓ ¿Cuándo usar Scoped vs Transient vs Singleton?**
R:
csharp// Scoped: Una instancia por petición HTTP (recomendado para servicios)
builder.Services.AddScoped<IProductoService, ProductoService>();

// Transient: Nueva instancia cada vez que se solicita
builder.Services.AddTransient<IEmailService, EmailService>();

// Singleton: Una sola instancia para toda la app
builder.Services.AddSingleton<ICacheService, CacheService>();
Regla general:

DbContext: Siempre Scoped
Servicios de negocio: Scoped
Servicios sin estado: Transient
Caché, configuración: Singleton

**❓ ¿Cómo debuggear el código?**
R:
1. Puntos de interrupción (Breakpoints):

Click izquierdo en el margen del editor (punto rojo)
F5 para iniciar debugging
F10 para paso a paso

2. Watch variables:

Hover sobre variables para ver su valor
Panel "Variables" en VS Code

3. Logs:
csharp_logger.LogInformation("Creando producto: {Codigo}", dto.Codigo);
4. SQL Profiler:

Ver qué SQL genera EF Core
En appsettings.Development.json:

json{
  "Logging": {
    "LogLevel": {
      "Microsoft.EntityFrameworkCore.Database.Command": "Information"
    }
  }
}

### 13. Recursos de Estudio
**📚 Documentación Oficial**
- .NET Documentation
- Entity Framework Core
- Blazor
- AutoMapper

**🎥 Tutoriales Recomendados**
- Onion Architecture Explained
- EF Core Deep Dive
- Blazor for Beginners

**📖 Libros**
- "Clean Architecture" - Robert C. Martin
- "Domain-Driven Design" - Eric Evans
- "C# in Depth" - Jon Skeet

### 14. Checklist de Dominio del Tutorial
Marca cuando domines cada concepto:
### Conceptos Fundamentales
- [ ] Entiendo qué es Onion Architecture
- [ ] Sé cuáles son las 4 capas y su propósito
- [ ] Entiendo el flujo de dependencias

### Domain
- [ ] Puedo crear una entidad nueva
- [ ] Entiendo EntidadBase y herencia
- [ ] Sé cuándo usar enums
- [ ] Puedo crear métodos de extensión

### Application
- [ ] Entiendo la diferencia entre DTO y Entidad
- [ ] Puedo crear DTOs para diferentes operaciones
- [ ] Sé configurar AutoMapper profiles
- [ ] Puedo implementar un Service con lógica de negocio

### Infrastructure
- [ ] Entiendo qué es DbContext
- [ ] Puedo configurar entidades con Fluent API
- [ ] Sé crear y aplicar migraciones
- [ ] Puedo implementar repositorios personalizados
- [ ] Entiendo el patrón Repository

### WebUI
- [ ] Entiendo cómo funciona Dependency Injection
- [ ] Puedo crear Minimal APIs
- [ ] Sé crear componentes Blazor básicos
- [ ] Entiendo el ciclo de vida de Blazor

### Prácticas
- [ ] Sé seguir el flujo completo de una operación
- [ ] Puedo debuggear el código efectivamente
- [ ] Entiendo cuándo usar async/await
- [ ] Sé escribir código limpio y mantenible

---

## 15. Glosario de Términos

### 📖 Términos Clave

**Async/Await**  
Programación asíncrona. Permite que el código espere operaciones sin bloquear el hilo.

**AutoMapper**  
Biblioteca para convertir automáticamente entre objetos similares.

**Blazor**  
Framework de Microsoft para crear UIs web interactivas con C# en lugar de JavaScript.

**DbContext**  
Clase de Entity Framework que representa una sesión con la base de datos.

**DbSet<T>**  
Representa una tabla en la base de datos. Permite hacer consultas LINQ.

**Dependency Injection (DI)**  
Patrón donde las dependencias se pasan al constructor en lugar de crearlas internamente.

**DTO (Data Transfer Object)**  
Objeto simple usado para transferir datos entre capas.

**Entity**  
Clase que representa una tabla en la base de datos.

**Fluent API**  
API de configuración de EF Core que usa encadenamiento de métodos.

**Lambda Expression**  
Función anónima expresada con sintaxis `=>`. Ejemplo: `x => x.Precio > 100`

**LINQ (Language Integrated Query)**  
Sintaxis de consultas integrada en C#.

**Migration**  
Archivo que define cambios en el esquema de la base de datos.

**Minimal API**  
Forma ligera de crear APIs REST sin controllers tradicionales.

**Onion Architecture**  
Arquitectura en capas donde las dependencias fluyen hacia el centro.

**ORM (Object-Relational Mapping)**  
Técnica para convertir entre objetos y tablas relacionales. EF Core es un ORM.

**Repository Pattern**  
Patrón que encapsula la lógica de acceso a datos.

**Scoped**  
Tiempo de vida de un servicio: una instancia por petición HTTP.

**Soft Delete**  
Marcar registros como inactivos en lugar de eliminarlos físicamente.

**Unit of Work**  
Patrón que agrupa operaciones en una transacción.

---

## 16. Plan de Acción Post-Tutorial

### 🎯 Próximos Pasos para el Equipo

#### Para Patricio (Database)
1. ✅ Estudiar sección de Infrastructure
2. ✅ Practicar creación de migraciones
3. ✅ Crear entidades: Cliente, TipoIdentificacion
4. 📝 Configurar con Fluent API
5. 📝 Aplicar migraciones

**Recursos:**
- `Infrastructure/Data/Configurations/`
- [EF Core Migrations Docs](https://docs.microsoft.com/ef/core/managing-schemas/migrations/)

---

#### Para Kerly (API Controllers)
1. ✅ Estudiar sección de WebUI (Minimal APIs)
2. ✅ Entender inyección de dependencias
3. ✅ Practicar creación de endpoints
4. 📝 Implementar endpoints de Producto (PUT, DELETE)
5. 📝 Crear endpoints de Cliente

**Recursos:**
- `WebUI/Program.cs` - sección de Minimal APIs
- [Minimal APIs Docs](https://docs.microsoft.com/aspnet/core/fundamentals/minimal-apis)

---

#### Para Melany (Frontend)
1. ✅ Estudiar sección de Blazor
2. ✅ Entender componentes .razor
3. ✅ Practicar binding y eventos
4. 📝 Completar páginas de Producto
5. 📝 Crear páginas de Cliente

**Recursos:**
- `WebUI/Components/Pages/`
- [Blazor Tutorial](https://dotnet.microsoft.com/learn/aspnet/blazor-tutorial/intro)

---

#### Para Pedro (Arquitectura y Coordinación)
1. ✅ Revisar PRs del equipo
2. ✅ Resolver dudas técnicas
3. ✅ Mantener arquitectura consistente
4. 📝 Documentar decisiones técnicas
5. 📝 Capacitar en nuevos patrones

---

## 17. Ejercicios de Evaluación

### 🧪 Autoevaluación del Conocimiento

#### Ejercicio 1: Conceptual (Oral/Escrito)

**Pregunta 1:** Explica con tus propias palabras qué es Onion Architecture.

**Pregunta 2:** ¿Cuál es la diferencia entre una Entidad y un DTO?

**Pregunta 3:** ¿Por qué usamos interfaces (IProductoService) en lugar de usar las clases directamente?

**Pregunta 4:** Explica el flujo completo desde que el usuario hace clic en "Guardar" hasta que se guarda en la BD.

**Pregunta 5:** ¿Qué ventajas tiene usar Repository Pattern?

---

#### Ejercicio 2: Código (Práctico)

**Desafío:** Crear módulo de Categorías

**Requisitos:**

1. **Domain/Entities/Categoria.cs**
```csharp
public class Categoria : EntidadBase
{
    public string Codigo { get; set; }     // Ej: "CAT-001"
    public string Nombre { get; set; }     // Ej: "Electrónica"
    public string? Descripcion { get; set; }
}
```

2. **DTOs:**
   - CategoriaDto
   - CrearCategoriaDto
   - ActualizarCategoriaDto

3. **Repository:**
   - ICategoriaRepository
   - CategoriaRepository

4. **Service:**
   - ICategoriaService
   - CategoriaService (con validación de código único)

5. **API:**
   - POST /api/categorias
   - GET /api/categorias
   - GET /api/categorias/{id}
   - PUT /api/categorias/{id}
   - DELETE /api/categorias/{id}

6. **Blazor (opcional):**
   - ListaCategorias.razor
   - FormularioCategoria.razor

---

#### Ejercicio 3: Debugging (Troubleshooting)

**Escenario:** Encuentra y corrige los errores en este código:
```csharp
// ❌ Código con errores
public class ProductoService
{
    private ProductoRepository _repository = new ProductoRepository();
    
    public ProductoDto Crear(CrearProductoDto dto)
    {
        var producto = new Producto
        {
            Codigo = dto.Codigo,
            Nombre = dto.Nombre,
            Precio = dto.Precio
        };
        
        _repository.Agregar(producto);
        
        return new ProductoDto
        {
            Id = producto.Id,
            Codigo = producto.Codigo,
            Nombre = producto.Nombre
        };
    }
}
```

**Errores a identificar:**
1. No usa inyección de dependencias
2. No es asíncrono
3. No valida código único
4. No usa AutoMapper
5. No maneja excepciones

---

## 18. Hoja de Ruta del Proyecto

### 📅 Sprints Planificados

#### Sprint 1 (Actual) ✅
- [x] Estructura base del proyecto
- [x] Módulo Productos (CRUD)
- [x] Arquitectura Onion implementada
- [x] Documentación completa
- [ ] Módulo Clientes
- [ ] Módulo Usuarios y Autenticación
- [ ] Roles y permisos

#### Sprint 2 (Próximo) 🚧
- [ ] Módulo Facturas
- [ ] Generación de XML
- [ ] Cálculos de impuestos
- [ ] Integración con SRI
- [ ] Firma digital
- [ ] Envío de comprobantes

#### Sprint 3
- [ ] Reportes y dashboard
- [ ] Exportación a PDF/Excel
- [ ] Notificaciones por email

---

## 19. Tablero de Conocimientos del Equipo

### 📊 Matriz de Habilidades

| Habilidad | Pedro | Patricio | Kerly | Melany |
|-----------|-------|----------|-------|--------|
| Onion Architecture | ⭐⭐⭐⭐ | ⭐⭐⭐ | ⭐⭐⭐ | ⭐⭐ |
| Entity Framework | ⭐⭐⭐ | ⭐⭐⭐ | ⭐⭐⭐ | ⭐⭐ |
| C# Avanzado | ⭐⭐⭐ | ⭐⭐ | ⭐⭐⭐ | ⭐⭐ |
| SQL Server | ⭐⭐⭐ | ⭐⭐⭐⭐ | ⭐⭐⭐ | ⭐⭐ |
| Blazor | ⭐⭐⭐ | ⭐⭐ | ⭐⭐⭐ | ⭐⭐⭐⭐ |
| Git/GitHub | ⭐⭐⭐⭐ | ⭐⭐⭐ | ⭐⭐⭐ | ⭐⭐⭐ |
| APIs REST | ⭐⭐⭐ | ⭐⭐⭐ | ⭐⭐⭐⭐ | ⭐⭐ |
| Testing | ⭐⭐⭐ | ⭐⭐ | ⭐⭐ | ⭐⭐ |

**Objetivo:** Todos en ⭐⭐⭐⭐ para fin de proyecto.

---

## 20. Conclusión y Próximos Pasos

### 🎓 Resumen del Tutorial

Has aprendido:

✅ **Arquitectura Onion** - Separación en 4 capas  
✅ **Domain** - Entidades puras y reglas de negocio  
✅ **Application** - Lógica de negocio con DTOs y Services  
✅ **Infrastructure** - Implementación con EF Core y Repositorios  
✅ **WebUI** - Presentación con Blazor y Minimal APIs  
✅ **Flujo completo** - De la UI a la BD y vuelta  
✅ **Patrones de diseño** - Repository, DI, DTO  
✅ **Buenas prácticas** - Código limpio y mantenible  

---

### 🚀 Siguientes Acciones

#### Inmediato (Esta Semana)
1. ✅ Repasar este documento
2. ✅ Hacer los ejercicios prácticos
3. ✅ Configurar ambiente de desarrollo
4. ✅ Clonar el proyecto y explorarlo
5. ✅ Hacer preguntas en el Daily Scrum

#### Corto Plazo (Este Sprint)
1. 📝 Completar tareas asignadas en Sprint Backlog
2. 📝 Practicar con código real
3. 📝 Hacer commits frecuentes
4. 📝 Participar en code reviews
5. 📝 Documentar aprendizajes

#### Largo Plazo (Proyecto Completo)
1. 🎯 Dominar todos los conceptos
2. 🎯 Contribuir ideas de arquitectura
3. 🎯 Ayudar a otros miembros
4. 🎯 Mejorar continuamente el código
5. 🎯 Preparar presentación final

---

### 💬 Canales de Soporte

**¿Dudas sobre el tutorial?**

1. **Daily Scrum** (9:00 AM) - Preguntas rápidas
2. **WhatsApp del equipo** - Consultas asíncronas
3. **Pedro (Arquitecto)** - Dudas técnicas profundas
4. **Pair Programming** - Aprender haciendo juntos

**Reglas:**
- ✅ No hay preguntas tontas
- ✅ Pedir ayuda es señal de profesionalismo
- ✅ Compartir conocimiento beneficia a todos
- ✅ Documentar soluciones encontradas

---

### 📝 Feedback del Tutorial

**Ayúdanos a mejorar:**

Después de completar el tutorial, por favor comparte:

1. ¿Qué sección fue más útil?
2. ¿Qué te resultó confuso?
3. ¿Qué te gustaría que se agregue?
4. ¿Cuánto tiempo te tomó completarlo?
5. ¿Te sientes preparado para trabajar en el proyecto?

**Enviar feedback a:** Pedro Supe

---

### 🎉 Mensaje Final

> **"La arquitectura limpia no es sobre hacer las cosas más complicadas.  
> Es sobre hacer que las cosas complejas sean más manejables."**  
> — Robert C. Martin (Uncle Bob)

**Recuerda:**
- 🧅 La cebolla tiene capas, nuestro proyecto también
- 🎯 El Domain es el corazón, protégelo
- 🔄 Los DTOs son tus amigos, úsalos
- 🧪 El código bien estructurado es fácil de testear
- 👥 Trabajamos en equipo, ayúdense mutuamente

---

## 📚 Anexos

### Anexo A: Cheat Sheet de Comandos
```bash
# Git
git status
git add .
git commit -m "T-XX: Descripción"
git push origin feature/mi-rama
git pull origin develop

# .NET
dotnet build
dotnet run --project SistemaFacturacionSRI.WebUI
dotnet watch run --project SistemaFacturacionSRI.WebUI

# EF Core
dotnet ef migrations add NombreMigracion -s WebUI -p Infrastructure
dotnet ef database update -s WebUI -p Infrastructure
dotnet ef migrations list -s WebUI -p Infrastructure
```

---

### Anexo B: Snippets Útiles

**Crear entidad rápidamente:**
```csharp
public class NombreEntidad : EntidadBase
{
    [Required]
    [StringLength(100)]
    public string Propiedad { get; set; } = string.Empty;
}
```

**Crear DTO rápidamente:**
```csharp
public class NombreDto
{
    public int Id { get; set; }
    public string Propiedad { get; set; } = string.Empty;
}
```

**Configurar Fluent API:**
```csharp
builder.ToTable("Tabla");
builder.HasKey(e => e.Id);
builder.Property(e => e.Propiedad)
    .IsRequired()
    .HasMaxLength(100);
```

---

### Anexo C: Estructura de Archivos Completa
```
SistemaFacturacionSRI/
│
├── .git/
├── .gitignore
├── README.md
├── GUIA_INSTALACION.md
├── GUIA_GIT_EQUIPO.md
├── TUTORIAL_ARQUITECTURA.md
├── SistemaFacturacionSRI.sln
│
├── SistemaFacturacionSRI.Domain/
│   ├── Entities/
│   │   ├── EntidadBase.cs
│   │   └── Producto.cs
│   ├── Enums/
│   │   ├── TipoIVA.cs
│   │   └── TipoIVAExtensions.cs
│   └── SistemaFacturacionSRI.Domain.csproj
│
├── SistemaFacturacionSRI.Application/
│   ├── DTOs/
│   │   ├── Producto/
│   │   │   ├── ProductoDto.cs
│   │   │   ├── CrearProductoDto.cs
│   │   │   └── ActualizarProductoDto.cs
│   │   └── README_DTOS.md
│   ├── Interfaces/
│   │   ├── Repositories/
│   │   │   ├── IRepositoryBase.cs
│   │   │   └── IProductoRepository.cs
│   │   └── Services/
│   │       └── IProductoService.cs
│   ├── Mappings/
│   │   └── ProductoProfile.cs
│   ├── Services/
│   │   └── ProductoService.cs
│   └── SistemaFacturacionSRI.Application.csproj
│
├── SistemaFacturacionSRI.Infrastructure/
│   ├── Data/
│   │   ├── ApplicationDbContext.cs
│   │   └── Configurations/
│   │       └── ProductoConfiguration.cs
│   ├── Migrations/
│   │   └── [archivos de migración]
│   ├── Repositories/
│   │   ├── RepositoryBase.cs
│   │   └── ProductoRepository.cs
│   └── SistemaFacturacionSRI.Infrastructure.csproj
│
└── SistemaFacturacionSRI.WebUI/
    ├── Components/
    │   ├── Layout/
    │   │   ├── MainLayout.razor
    │   │   └── NavMenu.razor
    │   └── Pages/
    │       ├── Home.razor
    │       ├── Error.razor
    │       └── Productos/
    │           ├── ListaProductos.razor
    │           └── FormularioProducto.razor
    ├── wwwroot/
    │   ├── css/
    │   │   └── site.css
    │   └── favicon.ico
    ├── appsettings.json
    ├── appsettings.Development.json
    ├── App.razor
    ├── Program.cs
    └── SistemaFacturacionSRI.WebUI.csproj
```

---

## 📞 Información de Contacto

**Instructor/Arquitecto:** Pedro Supe  
**Email:** [tu email]  
**GitHub:** [tu usuario]  
**Disponibilidad:** Daily Scrums (9:00 AM) y por WhatsApp

**Equipo de Desarrollo:**
- Patricio Tisalema - Database Engineer
- Kerly Chicaiza - Backend Developer
- Melany Cevallos - Frontend Developer

---

**Última actualización:** 31 de Octubre de 2025  
**Versión del tutorial:** 1.0  
**Tiempo estimado de estudio:** 2-3 horas  
**Nivel:** Intermedio

---

<div align="center">

## 🎓 ¡Felicitaciones por completar el tutorial!

**Ahora estás listo para contribuir al proyecto con confianza.**

**¡Manos a la obra! 💻🚀**

---

**Hecho con ❤️ para el equipo de Sistema de Facturación SRI**

[![Universidad Técnica de Ambato](https://img.shields.io/badge/UTA-Ambato-blue)](https://www.uta.edu.ec/)

</div>