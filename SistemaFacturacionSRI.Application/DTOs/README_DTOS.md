# 📦 DTOs (Data Transfer Objects)

## ¿Qué son los DTOs?

Los DTOs son objetos que se usan para transferir datos entre capas (especialmente entre API y cliente).

**NO son lo mismo que las entidades:**
- **Entidad:** Representa la tabla en la base de datos
- **DTO:** Representa los datos en la API (entrada/salida)

---

## DTOs de Producto

### 1. ProductoDto (Lectura - GET)
**Propósito:** Devolver información de productos al cliente

**Se usa en:**
- `GET /api/productos` (listar todos)
- `GET /api/productos/{id}` (obtener uno)

**Contiene:**
- Todos los campos que el cliente necesita VER
- Propiedades calculadas ya resueltas (ValorIVA, PrecioConIVA)
- NO contiene campos internos (Activo)

**Ejemplo de respuesta JSON:**
```json
{
  "id": 1,
  "codigo": "PROD-001",
  "nombre": "Laptop HP Pavilion",
  "descripcion": "Laptop con 8GB RAM",
  "precio": 1000.00,
  "tipoIVA": 12,
  "tipoIVADescripcion": "IVA 12%",
  "stock": 10,
  "unidadMedida": "Unidad",
  "valorIVA": 120.00,
  "precioConIVA": 1120.00,
  "tieneStock": true,
  "valorInventario": 10000.00,
  "fechaCreacion": "2025-10-31T10:00:00",
  "fechaModificacion": null
}
```

---

### 2. CrearProductoDto (Creación - POST)
**Propósito:** Recibir datos para crear un nuevo producto

**Se usa en:**
- `POST /api/productos`

**Contiene:**
- Solo los campos que el cliente DEBE enviar
- NO incluye Id (se genera automáticamente)
- NO incluye fechas (se establecen automáticamente)
- NO incluye propiedades calculadas

**Ejemplo de petición JSON:**
```json
{
  "codigo": "PROD-002",
  "nombre": "Mouse Logitech",
  "descripcion": "Mouse inalámbrico",
  "precio": 25.50,
  "tipoIVA": 12,
  "stock": 50,
  "unidadMedida": "Unidad"
}
```

---

### 3. ActualizarProductoDto (Actualización - PUT)
**Propósito:** Recibir datos para actualizar un producto existente

**Se usa en:**
- `PUT /api/productos/{id}`

**Contiene:**
- Todos los campos editables
- SÍ incluye Id (para saber cuál producto actualizar)
- NO incluye fechas (FechaModificacion se actualiza automáticamente)

**Ejemplo de petición JSON:**
```json
{
  "id": 1,
  "codigo": "PROD-001",
  "nombre": "Laptop HP Pavilion 15 (Actualizado)",
  "descripcion": "Laptop con 16GB RAM",
  "precio": 1200.00,
  "tipoIVA": 12,
  "stock": 8,
  "unidadMedida": "Unidad"
}
```

---

## Flujo de conversión
```
CREAR:
Cliente → CrearProductoDto → [Service] → Entidad Producto → Base de Datos

LEER:
Base de Datos → Entidad Producto → [Service] → ProductoDto → Cliente

ACTUALIZAR:
Cliente → ActualizarProductoDto → [Service] → Entidad Producto → Base de Datos
```

---

## Validaciones automáticas

Los DTOs tienen validaciones con Data Annotations:

- `[Required]`: Campo obligatorio
- `[StringLength]`: Longitud máxima/mínima
- `[Range]`: Rango numérico válido
- `[RegularExpression]`: Formato específico (ej: código)
- `[EnumDataType]`: Valor de enum válido

Si una petición no cumple las validaciones, la API automáticamente retorna error 400 (Bad Request) con los mensajes de error.