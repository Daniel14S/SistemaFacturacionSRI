# **Guía Técnica de Implementación del Formato RIDE y Esquema Offline SRI (V2.32)**

Este documento detalla las especificaciones para la generación de la **Representación Impresa de Documento Electrónico (RIDE)** y la lógica de negocio asociada para el esquema de facturación "Offline" del Servicio de Rentas Internas (SRI) del Ecuador.

## ---

**1\. Introducción y Normativa Vigente**

El esquema "Offline" permite la emisión de comprobantes sin necesidad de conexión inmediata, donde la validez tributaria reside en el archivo **XML firmado y autorizado**. El RIDE es la representación visual obligatoria que debe entregarse al cliente (PDF o impreso).1  
**Documentación Base:**

* **Ficha Técnica Offline V2.32 (Octubre 2025):** Define la estructura del XML, la clave de acceso y los requisitos del RIDE.1  
* **Esquemas XSD:** Definen la validación estricta de tipos de datos (V1.0.0 a V2.1.0).1

## ---

**2\. Generación de la Clave de Acceso (ID Único)**

La **Clave de Acceso** de 49 dígitos es el componente más crítico. En el esquema Offline, esta clave se convierte automáticamente en el **Número de Autorización**. Debe generarse antes de firmar el XML.1

### **2.1 Estructura de la Clave (49 Dígitos)**

| Posición | Campo | Longitud | Descripción / Fuente XML |
| :---- | :---- | :---- | :---- |
| **1-8** | Fecha Emisión | 8 | ddmmaaaa (Debe coincidir con \<fechaEmision\>). |
| **9-10** | Tipo Comprobante | 2 | Tabla 3: 01=Factura, 04=Nota Crédito, 05=Nota Débito, 06=Guía, 07=Retención, 03=Liquidación.1 |
| **11-23** | RUC Emisor | 13 | RUC del contribuyente. |
| **24** | Ambiente | 1 | 1=Pruebas, 2=Producción. |
| **25-30** | Serie (Estab) | 3 | Código del establecimiento (ej. 001). |
| **31-33** | Serie (PtoEmi) | 3 | Código del punto de emisión (ej. 002). |
| **34-42** | Secuencial | 9 | Número consecutivo del comprobante (rellenar con ceros). |
| **43-50** | Código Numérico | 8 | Número interno aleatorio/secuencial para unicidad. |
| **51** | Tipo Emisión | 1 | Siempre **1** (Emisión Normal Offline).1 |
| **52** | Dígito Verificador | 1 | Algoritmo **Módulo 11**. |

### **2.2 Validación Módulo 11**

Para calcular el dígito 52:

1. Invertir la cadena de 48 dígitos.  
2. Multiplicar por la serie ponderada (2, 3, 4, 5, 6, 7...).  
3. Sumar productos y obtener residuo mod 11\.  
4. Si el residuo es 0 \-\> dígito 0; si es 1 \-\> dígito 1 (según regla específica SRI, si resultado es 11=0, si es 10=1).1

## ---

**3\. Estructura Visual del RIDE (Layout)**

El diseño es libre pero debe contener secciones obligatorias mapeadas desde el XML.

### **3.1 Encabezado Izquierdo (Datos Emisor)**

* **Logotipo:** (Opcional).  
* **Razón Social y Nombre Comercial:** Tal como constan en el RUC.  
* **Dirección Matriz y Sucursal:** Etiquetas \<dirMatriz\> y \<dirEstablecimiento\>.1  
* **Contribuyente Especial:** "Contribuyente Especial Nro. 12345" (si aplica).  
* **Obligado a Llevar Contabilidad:** "SI" o "NO".  
* **Agente de Retención:** "Agente de Retención Resolución No. \[Nro\]" (Desde XSD V2.1.0).1  
* **Régimen RIMPE:** Texto obligatorio "CONTRIBUYENTE RÉGIMEN RIMPE" o "CONTRIBUYENTE NEGOCIO POPULAR \- RÉGIMEN RIMPE" si aplica.1

### **3.2 Encabezado Derecho (Datos Fiscales)**

* **RUC:** Del emisor.  
* **Tipo Documento:** (Ej. FACTURA).  
* **No. Comprobante:** Formato 001-001-000000001.  
* **Número de Autorización:** La Clave de Acceso de 49 dígitos.  
* **Fecha y Hora de Autorización:** Obtenida de la respuesta del Web Service (WS) del SRI.  
* **Ambiente:** PRUEBAS o PRODUCCIÓN.  
* **Emisión:** NORMAL.  
* **Clave de Acceso:** Numérica y en **Código de Barras** (Code 128 recomendado).1

### **3.3 Información del Cliente/Receptor**

* Razón Social, RUC/CI, Fecha Emisión.  
* **Guía de Remisión:** Si aplica, debe imprimirse el número.1

## ---

**4\. Detalles por Tipo de Documento**

### **4.1 Factura (01)**

* **Detalle Ítems:** Código, Cantidad, Descripción, Precio Unitario, Descuento, Precio Total.  
  * *Nota:* Los XSD V1.1.0 y superiores permiten hasta **6 decimales** en cantidad y precio unitario.1  
* **Subsidios:** Si es venta de combustible subsidiado, incluir columnas: "Precio sin Subsidio" y "Descuento/Subsidio".  
* **Formas de Pago:** Obligatorio detallar código (ej. 19 Tarjeta, 01 Sin sistema financiero), total y plazo.1

### **4.2 Liquidación de Compra (03)**

Utilizada para compras a proveedores sin RUC, extranjeros o reembolsos.

* **Datos Proveedor:** Identificación, nombre y dirección del vendedor precario.  
* **Reembolsos:** Si es una liquidación por reembolso, se debe detallar la factura original (intermediario).

### **4.3 Guía de Remisión (06)**

* **Transportista:** Razón social, RUC y **Placa** (Dato crítico para controles).  
* **Ruta:** Dirección partida y llegada, fechas de traslado.  
* **Destinatarios:** Lista de destinatarios con sus respectivas facturas de sustento y motivo de traslado.1

## ---

**5\. Impuestos y Tablas Referenciales**

El software debe mapear los códigos del XML a texto legible en el RIDE.

### **5.1 Códigos de Impuesto (Tabla 16\)**

* **2:** IVA  
* **3:** ICE  
* **5:** IRBPNR

### **5.2 Tarifas de IVA (Tabla 17\) \- ¡No "quemar" el 12%\!**

El RIDE debe leer la tarifa del XML, ya que puede variar (12%, 15%, 8%, etc.).

* **0:** 0%  
* **2:** 12%  
* **4:** 15% (Vigente 2024/2025 en ciertos periodos).  
* **5:** 5% (Materiales de construcción).  
* **8:** IVA Diferenciado (Sector turístico).  
* **10:** 13% (Casos especiales).1

## ---

**6\. Lógica de Negocio Actualizada (2025)**

### **6.1 Plazos de Anulación (Nuevas Reglas 2025\)**

Según la Resolución NAC-DGERCGC25-00000017 (Julio 2025):

* **Regla General:** Los comprobantes electrónicos pueden anularse hasta el **día 7 del mes siguiente** a su emisión.  
* **Consumidor Final:** Las facturas emitidas a "Consumidor Final" **NO se pueden anular** ni se puede emitir Nota de Crédito sobre ellas (salvo error de sistema comprobado o casos muy específicos).  
* **Validación:** Si tu sistema permite anular, debe validar estas fechas contra la fecha de emisión del XML.

### **6.2 Estados del Documento (WS)**

* **RECIBIDA:** XML pasó validación de esquema (XSD) y firma.  
* **AUTORIZADO:** XML tiene validez tributaria. Solo aquí se genera el RIDE definitivo.  
* **DEVUELTA:** Error en datos (ej. RUC incorrecto, duplicado). Corregir y re-enviar con *misma* clave de acceso si es posible, o nueva si cambió un dato clave.1

## ---

**7\. Checklist para Desarrolladores (CLI / Backend)**

Si vas a usar una herramienta CLI o script para procesar esto:

1. **Validar XSD:** Antes de enviar, valida tu XML contra factura\_V2.1.0.xsd (o la versión vigente).1  
2. **Firmar XAdES-BES:** Usa una librería que soporte firma electrónica archivo .p12.  
3. **Consumir WS Recepción:** Envía el XML en Base64.  
4. **Consumir WS Autorización:** Consulta por Clave de Acceso.  
5. **Generar PDF (RIDE):** Si el estado es AUTORIZADO, parsea el XML de respuesta (que contiene la fecha de autorización) y rellena tu plantilla de reporte.  
6. **Email:** Envía el XML y el PDF al correo del cliente extraído del campo \<correo\> en \<infoAdicional\>.1
