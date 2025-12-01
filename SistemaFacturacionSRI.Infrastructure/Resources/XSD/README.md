# Esquemas XSD del SRI

Esta carpeta contiene los esquemas XSD oficiales del Servicio de Rentas Internas (SRI) de Ecuador para la validación de comprobantes electrónicos.

## Archivos requeridos

### factura_v1.1.0.xsd
Esquema oficial para validación de facturas electrónicas versión 1.1.0

**Descarga desde:**
- Portal SRI: https://www.sri.gob.ec/esquemas-xsd
- URL directa: https://celcer.sri.gob.ec/comprobantes-electronicos-ws/schemas/factura_v1.1.0.xsd

## Configuración en Visual Studio

1. Agregar el archivo XSD a esta carpeta
2. Click derecho en el archivo → Propiedades
3. Configurar:
   - **Build Action**: Content
   - **Copy to Output Directory**: Copy if newer

## Verificación

El archivo debe tener esta estructura inicial:

```xml
<?xml version="1.0" encoding="UTF-8"?>
<xs:schema xmlns:xs="http://www.w3.org/2001/XMLSchema" 
           elementFormDefault="qualified" 
           attributeFormDefault="unqualified">
    <xs:element name="factura">
        <!-- Definiciones del esquema -->
    </xs:element>
</xs:schema>
```

## Versión actual
- Versión del esquema: 1.1.0
- Última actualización: 2024
- Compatible con: Resolución NAC-DGERCGC12-00105

## Notas importantes

- NO modificar los archivos XSD descargados
- Mantener la versión oficial del SRI
- Actualizar cuando el SRI publique nuevas versiones
- Los esquemas se copian automáticamente al directorio de salida al compilar

## Troubleshooting

### Error: "No se encontró el esquema XSD"
**Solución**: Verificar que el archivo esté en `Infrastructure/Resources/XSD/` y tenga "Copy to Output Directory" = "Copy if newer"

### Error al validar
**Solución**: Descargar nuevamente el esquema desde el portal oficial del SRI