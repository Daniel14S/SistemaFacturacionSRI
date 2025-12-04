# Documentación WebServices SOAP del SRI Ecuador

## 📡 T-068: Investigación Completa de WebServices del SRI

---

## 🌐 URLs de los WebServices

### Ambiente de PRUEBAS
```
Recepción de Comprobantes:
https://celements.sri.gob.ec/comprobantes-electronicos-ws/RecepcionComprobantesOffline?wsdl

Autorización de Comprobantes:
https://celements.sri.gob.ec/comprobantes-electronicos-ws/AutorizacionComprobantesOffline?wsdl
```

### Ambiente de PRODUCCIÓN
```
Recepción de Comprobantes:
https://cel.sri.gob.ec/comprobantes-electronicos-ws/RecepcionComprobantesOffline?wsdl

Autorización de Comprobantes:
https://cel.sri.gob.ec/comprobantes-electronicos-ws/AutorizacionComprobantesOffline?wsdl
```

---

## 🔧 Métodos SOAP Disponibles

### 1. **RecepcionComprobantesOffline** - Método: `validarComprobante`

**Descripción**: Recibe y valida un comprobante electrónico firmado digitalmente.

**Request SOAP**:
```xml
<soapenv:Envelope xmlns:soapenv="http://schemas.xmlsoap.org/soap/envelope/" 
                  xmlns:ec="http://ec.gob.sri.ws.recepcion">
   <soapenv:Header/>
   <soapenv:Body>
      <ec:validarComprobante>
         <xml><![CDATA[<?xml version="1.0" encoding="UTF-8"?>
            <!-- XML del comprobante firmado en Base64 o CDATA -->
         ]]></xml>
      </ec:validarComprobante>
   </soapenv:Body>
</soapenv:Envelope>
```

**Response SOAP**:
```xml
<soap:Envelope xmlns:soap="http://schemas.xmlsoap.org/soap/envelope/">
   <soap:Body>
      <ns2:validarComprobanteResponse xmlns:ns2="http://ec.gob.sri.ws.recepcion">
         <RespuestaRecepcionComprobante>
            <estado>RECIBIDA</estado> <!-- o DEVUELTA -->
            <comprobantes>
               <comprobante>
                  <claveAcceso>4912202401123456789000110010010000000011234567819</claveAcceso>
                  <mensajes>
                     <mensaje>
                        <identificador>43</identificador>
                        <mensaje>CLAVE ACCESO REGISTRADA</mensaje>
                        <informacionAdicional></informacionAdicional>
                        <tipo>INFORMATIVO</tipo>
                     </mensaje>
                  </mensajes>
               </comprobante>
            </comprobantes>
         </RespuestaRecepcionComprobante>
      </ns2:validarComprobanteResponse>
   </soap:Body>
</soap:Envelope>
```

**Estados Posibles**:
- `RECIBIDA`: Comprobante recibido correctamente, pasar a consultar autorización
- `DEVUELTA`: Comprobante rechazado, revisar mensajes de error

---

### 2. **AutorizacionComprobantesOffline** - Método: `autorizacionComprobante`

**Descripción**: Consulta el estado de autorización de un comprobante usando su clave de acceso.

**Request SOAP**:
```xml
<soapenv:Envelope xmlns:soapenv="http://schemas.xmlsoap.org/soap/envelope/" 
                  xmlns:ec="http://ec.gob.sri.ws.autorizacion">
   <soapenv:Header/>
   <soapenv:Body>
      <ec:autorizacionComprobante>
         <claveAccesoComprobante>4912202401123456789000110010010000000011234567819</claveAccesoComprobante>
      </ec:autorizacionComprobante>
   </soapenv:Body>
</soapenv:Envelope>
```

**Response SOAP**:
```xml
<soap:Envelope xmlns:soap="http://schemas.xmlsoap.org/soap/envelope/">
   <soap:Body>
      <ns2:autorizacionComprobanteResponse xmlns:ns2="http://ec.gob.sri.ws.autorizacion">
         <RespuestaAutorizacionComprobante>
            <claveAccesoConsultada>4912202401123456789000110010010000000011234567819</claveAccesoConsultada>
            <numeroComprobantes>1</numeroComprobantes>
            <autorizaciones>
               <autorizacion>
                  <estado>AUTORIZADO</estado> <!-- o NO AUTORIZADO, EN PROCESAMIENTO -->
                  <numeroAutorizacion>1234567890</numeroAutorizacion>
                  <fechaAutorizacion>03/12/2024 15:30:45</fechaAutorizacion>
                  <ambiente>PRUEBAS</ambiente>
                  <comprobante><![CDATA[<?xml version="1.0"?>
                     <!-- XML del comprobante autorizado -->
                  ]]></comprobante>
                  <mensajes>
                     <mensaje>
                        <identificador>60</identificador>
                        <mensaje>COMPROBANTE AUTORIZADO</mensaje>
                        <informacionAdicional></informacionAdicional>
                        <tipo>INFORMATIVO</tipo>
                     </mensaje>
                  </mensajes>
               </autorizacion>
            </autorizaciones>
         </RespuestaAutorizacionComprobante>
      </ns2:autorizacionComprobanteResponse>
   </soap:Body>
</soap:Envelope>
```

**Estados Posibles**:
- `AUTORIZADO`: Comprobante autorizado, guardar número y fecha de autorización
- `NO AUTORIZADO`: Comprobante rechazado, revisar mensajes
- `EN PROCESAMIENTO`: Todavía procesándose, reintentar después
- `DEVUELTA`: Error en recepción

---

## 📋 Estructura de Mensajes de Error/Info

### Tipos de Mensaje
- `INFORMATIVO`: Información general
- `ADVERTENCIA`: Advertencias no críticas
- `ERROR`: Errores que impiden la autorización

### Códigos de Mensaje Comunes

| Código | Tipo | Mensaje | Acción |
|--------|------|---------|--------|
| 43 | INFORMATIVO | CLAVE ACCESO REGISTRADA | Continuar con autorización |
| 60 | INFORMATIVO | COMPROBANTE AUTORIZADO | Comprobante exitoso |
| 65 | ERROR | CLAVE DE ACCESO INCORRECTA | Regenerar clave de acceso |
| 69 | ERROR | CERTIFICADO INVALIDO | Renovar certificado |
| 70 | ERROR | FIRMA ELECTRONICA NO VALIDA | Revisar firma XADES-BES |
| 235 | ERROR | SECUENCIAL YA EXISTE | Cambiar número secuencial |
| 999 | ERROR | ERROR INTERNO DEL SISTEMA | Reintentar más tarde |

---

## 🔐 Headers HTTP Requeridos

```http
Content-Type: text/xml; charset=utf-8
SOAPAction: ""
Accept: text/xml, application/xml
User-Agent: Sistema-Facturacion/1.0
```

---

## ⏱️ Timeouts Recomendados

| Operación | Timeout | Razón |
|-----------|---------|-------|
| Recepción | 30s | Validación y recepción rápida |
| Autorización | 60s | Proceso puede tardar más |
| Conexión | 10s | Detectar problemas de red rápido |

---

## 🔄 Flujo Completo de Envío

```
1. GENERAR XML de Factura
   ↓
2. FIRMAR XML con certificado digital (XADES-BES)
   ↓
3. ENVIAR a RecepcionComprobantesOffline
   ↓
4. ¿Estado = RECIBIDA?
   │
   ├─ SÍ → Esperar 5-10 segundos
   │        ↓
   │        CONSULTAR en AutorizacionComprobantesOffline
   │        ↓
   │        ¿Estado = AUTORIZADO?
   │        │
   │        ├─ SÍ → ✅ Guardar número autorización
   │        │        ✅ Actualizar estado en BD
   │        │        ✅ Generar PDF con sello
   │        │
   │        ├─ EN PROCESAMIENTO → Reintentar cada 5s (máx 10 intentos)
   │        │
   │        └─ NO AUTORIZADO → ❌ Revisar mensajes de error
   │                            ❌ Corregir y reenviar
   │
   └─ NO (DEVUELTA) → ❌ Revisar mensajes de error
                       ❌ Corregir XML
                       ❌ Re-firmar y reenviar
```

---

## 📝 Consideraciones Importantes

### ✅ Validaciones Previas (antes de enviar)
1. ✅ XML bien formado y válido según XSD del SRI
2. ✅ Clave de acceso correcta (49 dígitos con dígito verificador)
3. ✅ Fecha de emisión no futura
4. ✅ RUC válido y activo
5. ✅ Secuencial único y consecutivo
6. ✅ Firma digital XADES-BES válida
7. ✅ Certificado vigente y autorizado

### ⚠️ Errores Comunes
- **Error 70 (Firma inválida)**: 
  - Verificar estructura XADES-BES
  - Verificar certificado vigente
  - Verificar que SignedProperties esté correcto

- **Error 235 (Secuencial duplicado)**:
  - El secuencial ya fue usado
  - Incrementar secuencial y regenerar

- **Error 69 (Certificado inválido)**:
  - Certificado expirado
  - Certificado no autorizado por el SRI
  - Renovar certificado

### 🔄 Política de Reintentos
1. **Recepción**: 3 reintentos con delay 2s, 5s, 10s
2. **Autorización**: 
   - Si `EN PROCESAMIENTO`: 10 reintentos cada 5s
   - Si error de red: 3 reintentos con delay exponencial

### 🔒 Seguridad
- ✅ Usar HTTPS siempre
- ✅ Validar certificado SSL del SRI
- ✅ No exponer credenciales en logs
- ✅ Timeout para evitar bloqueos

---

## 📚 Referencias Oficiales

- **Portal SRI**: https://www.sri.gob.ec
- **Documentación Técnica**: https://www.sri.gob.ec/facturacion-electronica
- **Esquemas XSD**: https://www.sri.gob.ec/esquemas-xsd
- **Pruebas**: https://celements.sri.gob.ec (ambiente de pruebas)

---

## 🧪 Ambiente de Pruebas

### Obtener Acceso
1. Registrarse en el portal del SRI
2. Solicitar acceso al ambiente de pruebas
3. Obtener certificado digital de pruebas
4. Configurar RUC de pruebas

### RUC de Pruebas Común
```
RUC: 1234567890001 (ejemplo genérico)
Ambiente: 1 (PRUEBAS)
```

### Validación en Pruebas
- ✅ No requiere contribuyente real
- ✅ Certificados de prueba aceptados
- ✅ Sin impacto en producción
- ✅ Mismo flujo que producción

---

## 🎯 Próximos Pasos (T-069 a T-083)

1. ✅ T-068: Documentación completa ← **ESTAMOS AQUÍ**
2. ➡️ T-069: Obtener credenciales ambiente pruebas
3. ➡️ T-070: Configurar URLs en appsettings
4. ➡️ T-071: Instalar paquetes SOAP
5. ➡️ T-072-073: Crear DTOs Request/Response
6. ➡️ T-074: Parsers XML SOAP
7. ➡️ T-075-079: Implementar cliente SOAP
8. ➡️ T-080: Lógica de reintentos
9. ➡️ T-081-083: Orquestador completo

---

**Documentación generada**: Diciembre 2024  
**Basado en**: Especificaciones SRI Ecuador v2.23