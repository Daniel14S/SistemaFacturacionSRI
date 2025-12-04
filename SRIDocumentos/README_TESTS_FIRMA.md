# Tests de Firma Electrónica XADES-BES (T-067)

## 📋 Requisitos Previos

1. **Certificado Digital .p12 o .pfx** con clave privada
2. **Contraseña del certificado**
3. Certificado válido (no expirado)
4. Certificado autorizado para firma digital

## 🔧 Configuración

### Paso 1: Colocar el Certificado

Coloca tu archivo de certificado en:
```
Infrastructure/Resources/Certificados/pruebas/certificado-sri-pruebas.p12
```

### Paso 2: Configurar appsettings.json

Edita `appsettings.json` y actualiza:

```json
{
  "CertificadoDigital": {
    "RutaCertificado": "Infrastructure/Resources/Certificados/pruebas/certificado-sri-pruebas.p12",
    "ClaveCertificado": "TU_CLAVE_AQUI",  // ⚠️ CAMBIAR
    "TipoCertificado": "PRUEBAS",
    "ValidarVigencia": true,
    "ValidarCadenaConfianza": false
  }
}
```

### Paso 3: Actualizar el Test

En `FirmaElectronicaIntegrationTests.cs`, actualiza:

```csharp
_claveCertificado = "tu_clave_aqui"; // CAMBIAR POR LA CLAVE REAL
```

### Paso 4: Remover Skip de los Tests

En cada test que quieras ejecutar, **elimina** el atributo:
```csharp
[Fact(Skip = "Requiere certificado real y clave configurada")]
```

Y déjalo como:
```csharp
[Fact]
```

## ▶️ Ejecutar los Tests

### Ejecutar Todos los Tests de Integración

```bash
cd Tests
dotnet test --filter "FullyQualifiedName~FirmaElectronicaIntegrationTests"
```

### Ejecutar un Test Específico

```bash
dotnet test --filter "FullyQualifiedName~Test04_FirmarFacturaSimple"
```

### Ejecutar con Logs Detallados

```bash
dotnet test --filter "FullyQualifiedName~FirmaElectronicaIntegrationTests" --logger "console;verbosity=detailed"
```

## 📝 Tests Disponibles

### ✅ Test 01: Cargar Certificado Real
Verifica que el certificado se cargue correctamente y tenga clave privada.

### ✅ Test 02: Validar Certificado Real
Valida que el certificado sea válido para firma SRI.

### ✅ Test 03: Imprimir Reporte Certificado
Muestra información completa del certificado en consola.

### ✅ Test 04: Firmar Factura Simple ⭐
**PRINCIPAL**: Firma un XML de factura y lo guarda en `factura_firmada_test.xml`.

### ✅ Test 05: Firmar y Validar
Firma un XML y valida que la firma sea correcta.

### ✅ Test 06: Validación Detallada
Obtiene información detallada de la validación de firma.

### ✅ Test 07: Obtener Información Certificado
Extrae información del certificado desde un XML firmado.

### ✅ Test 08: Extraer XML Original
Elimina la firma de un XML firmado.

### ✅ Test 09: Firmar Múltiples Documentos
Firma 3 facturas secuencialmente.

### ✅ Test 10: Validar Compatibilidad SRI
Verifica que el certificado cumpla los requisitos del SRI Ecuador.

## 📂 Archivos Generados

Después de ejecutar los tests, se generarán:

- `factura_firmada_test.xml` - Factura firmada de ejemplo
- Logs en consola con información detallada

## 🔍 Verificar el XML Firmado

El XML firmado debe contener:

```xml
<factura id="comprobante">
    <!-- Datos de la factura -->
    
    <ds:Signature xmlns:ds="http://www.w3.org/2000/09/xmldsig#">
        <ds:SignedInfo>
            <!-- Referencias y métodos -->
        </ds:SignedInfo>
        
        <ds:SignatureValue>
            <!-- Firma RSA en Base64 -->
        </ds:SignatureValue>
        
        <ds:KeyInfo>
            <!-- Certificado X509 -->
        </ds:KeyInfo>
        
        <ds:Object>
            <!-- Propiedades firmadas XADES -->
        </ds:Object>
    </ds:Signature>
</factura>
```

## 🎯 Test Recomendado para Empezar

Empieza con **Test04_FirmarFacturaSimple**:

1. Configura tu certificado y clave
2. Remueve el `Skip` solo de este test
3. Ejecuta:
   ```bash
   dotnet test --filter "Test04_FirmarFacturaSimple"
   ```
4. Si es exitoso, revisa `factura_firmada_test.xml`
5. Continúa con los demás tests

## ⚠️ Troubleshooting

### Error: "No se encontró el archivo del certificado"
- Verifica que la ruta en `appsettings.json` sea correcta
- Verifica que el archivo exista en `Infrastructure/Resources/Certificados/pruebas/`

### Error: "No se pudo cargar el certificado"
- Verifica que la contraseña sea correcta
- Verifica que el archivo no esté corrupto

### Error: "El certificado no tiene clave privada"
- El certificado debe ser exportado CON clave privada
- En Windows: Exportar como .pfx con clave privada marcada

### Error: "Certificado expirado"
- Renueva tu certificado digital
- O desactiva validación temporal (solo para tests): `ValidarVigencia: false`

### Error: "La firma no es válida"
- Verifica que el certificado sea para firma digital (Key Usage)
- Verifica que no esté expirado al momento de firmar

## 📊 Logs Esperados

Al ejecutar el Test04, deberías ver:

```
═══════════════════════════════════════
INICIANDO FIRMA ELECTRÓNICA XADES-BES
═══════════════════════════════════════
Certificado: CN=TU CERTIFICADO
1️⃣ Cargando y validando XML...
   ✅ Nodo a firmar: factura (id=comprobante)
2️⃣ Calculando digest (hash SHA1) del documento...
   ✅ Digest calculado: abcd1234...
3️⃣ Creando estructura XADES-BES...
4️⃣ Firmando SignedInfo con RSA-SHA1...
   ✅ Firma RSA generada: xyzt5678...
5️⃣ Insertando firma en SignatureValue...
6️⃣ Insertando nodo Signature en el XML...
   ✅ Firma insertada correctamente
═══════════════════════════════════════
✅ FIRMA COMPLETADA EXITOSAMENTE
═══════════════════════════════════════
⏱️  Tiempo: 123ms
📄 Tamaño XML: 5432 bytes
```

## 🎓 Próximos Pasos

Una vez que los tests pasen exitosamente:

1. ✅ PBI-11 Completo (Firma Electrónica)
2. ➡️ PBI-12: Integración con WebServices SOAP del SRI
3. ➡️ Envío de comprobantes al SRI
4. ➡️ Consulta de autorizaciones

## 📞 Soporte

Si encuentras problemas:
1. Revisa los logs detallados
2. Verifica la configuración del certificado
3. Asegúrate de que el certificado sea válido
4. Consulta la documentación del SRI Ecuador