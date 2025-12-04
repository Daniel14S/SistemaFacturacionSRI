# Guía: Obtener Acceso al Ambiente de Pruebas del SRI

## 📋 T-069: Credenciales y Acceso al Ambiente de Pruebas

---

## 🎯 Objetivo

Obtener acceso al ambiente de pruebas del SRI Ecuador para realizar tests de integración sin afectar la facturación real.

---

## 📝 Paso 1: Registro en el Portal SRI

### 1.1 Acceder al Portal
- **URL Producción**: https://www.sri.gob.ec
- **URL Pruebas**: https://celements.sri.gob.ec

### 1.2 Crear Cuenta (si no tienes)
1. Ir a "Registro de usuarios"
2. Proporcionar:
   - RUC de la empresa
   - Cédula del representante legal
   - Correo electrónico
   - Contraseña segura

---

## 🔐 Paso 2: Solicitar Certificado Digital de Pruebas

### 2.1 Opciones de Certificado

**Opción A: Certificado de Prueba del SRI** (Recomendado para desarrollo)
- El SRI proporciona certificados genéricos para pruebas
- No requiere trámite con entidad certificadora
- **Válido solo para ambiente de pruebas**

**Opción B: Certificado Real en Ambiente de Pruebas**
- Usar tu certificado digital real pero en el ambiente de pruebas
- Útil para validar que tu certificado funciona antes de producción

### 2.2 Descargar Certificado de Prueba

```bash
# El SRI puede proporcionar certificados genéricos como:
RUC de Pruebas: 1234567890001
Certificado: certificado-pruebas.p12
Clave: pruebas123
```

**Nota**: Contacta al soporte del SRI para obtener certificados de prueba oficiales.

---

## 🌐 Paso 3: URLs del Ambiente de Pruebas

### WebServices SOAP - Ambiente Pruebas

```
Recepción Comprobantes:
https://celements.sri.gob.ec/comprobantes-electronicos-ws/RecepcionComprobantesOffline?wsdl

Autorización Comprobantes:
https://celements.sri.gob.ec/comprobantes-electronicos-ws/AutorizacionComprobantesOffline?wsdl
```

### Portal Web - Ambiente Pruebas
```
https://celements.sri.gob.ec
```

---

## ✅ Paso 4: Validar Acceso

### 4.1 Probar Conexión con cURL

```bash
# Test de Recepción
curl -X POST https://celements.sri.gob.ec/comprobantes-electronicos-ws/RecepcionComprobantesOffline \
  -H "Content-Type: text/xml; charset=utf-8" \
  -H "SOAPAction: ''" \
  -d '<soapenv:Envelope xmlns:soapenv="http://schemas.xmlsoap.org/soap/envelope/" xmlns:ec="http://ec.gob.sri.ws.recepcion"><soapenv:Header/><soapenv:Body><ec:validarComprobante><xml><![CDATA[TEST]]></xml></ec:validarComprobante></soapenv:Body></soapenv:Envelope>'
```

**Respuesta Esperada**: Debe retornar un XML SOAP (aunque con error por XML inválido, pero confirma conectividad)

### 4.2 Verificar Certificado

```bash
# Verificar que el certificado .p12 se pueda cargar
openssl pkcs12 -in certificado-pruebas.p12 -noout
# Pedirá la contraseña y debe confirmar que es válido
```

---

## 🎓 Paso 5: Entender el Ambiente de Pruebas

### Características del Ambiente de Pruebas

✅ **Permitido**:
- Enviar comprobantes de prueba
- Probar firma digital
- Validar estructura XML
- Simular flujo completo

❌ **No Permitido**:
- Usar para facturación real
- RUCs de clientes reales
- Mezclar con producción

### Datos de Prueba Sugeridos

```yaml
RUC Emisor: 1234567890001
RUC Cliente: 9999999999999 (Consumidor Final)
Ambiente: 1 (PRUEBAS)
Establecimiento: 001
Punto Emisión: 001
Secuencial: 000000001 (y siguientes)
```

### Estructura Clave de Acceso (49 dígitos)

```
Formato: DDMMYYYYTTNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNC

DD: Día (01-31)
MM: Mes (01-12)
YYYY: Año (2024)
TT: Tipo comprobante (01=Factura, 04=Nota Crédito, etc.)
NNNNN...: RUC + Establecimiento + Punto Emisión + Secuencial + Código Numérico
C: Dígito verificador (módulo 11)
```

**Ejemplo**:
```
0312202401123456789000110010010000000011234567819
│││││││││││││││││││││││││││││││││││││││││││││││││
├─┴─┴─┴─┴─┴─┴─┴─┴─┴─┴─┴─┴─┴─┴─┴─┴─┴─┴─┴─┴─┴─┴─┴─┴─┴─┘
Fecha: 03/12/2024, Tipo: 01 (Factura), RUC: 1234567890001...
```

---

## 🛠️ Paso 6: Configurar el Proyecto

### 6.1 Actualizar appsettings.Development.json

```json
{
  "SRI": {
    "Ambiente": "PRUEBAS",
    "AmbienteCodigo": 1,
    "UrlRecepcion": "https://celements.sri.gob.ec/comprobantes-electronicos-ws/RecepcionComprobantesOffline",
    "UrlAutorizacion": "https://celements.sri.gob.ec/comprobantes-electronicos-ws/AutorizacionComprobantesOffline",
    "TimeoutSegundos": 30,
    "ReintentoMaximo": 3,
    "DelayEntreReintentosMs": 2000
  },
  "CertificadoDigital": {
    "RutaCertificado": "Infrastructure/Resources/Certificados/pruebas/certificado-sri-pruebas.p12",
    "ClaveCertificado": "pruebas123",
    "TipoCertificado": "PRUEBAS"
  }
}
```

### 6.2 Para Producción (appsettings.Production.json)

```json
{
  "SRI": {
    "Ambiente": "PRODUCCION",
    "AmbienteCodigo": 2,
    "UrlRecepcion": "https://cel.sri.gob.ec/comprobantes-electronicos-ws/RecepcionComprobantesOffline",
    "UrlAutorizacion": "https://cel.sri.gob.ec/comprobantes-electronicos-ws/AutorizacionComprobantesOffline",
    "TimeoutSegundos": 30,
    "ReintentoMaximo": 3,
    "DelayEntreReintentosMs": 2000
  }
}
```

---

## 📞 Paso 7: Contacto y Soporte SRI

### Canales de Soporte

**📧 Email**: 
- soporte@sri.gob.ec
- facturacionelectronica@sri.gob.ec

**📞 Teléfono**:
- 1700 774 774 (Ecuador)
- Opción: Facturación Electrónica

**🌐 Portal**:
- https://www.sri.gob.ec
- Chat en línea disponible

**📍 Oficinas**:
- Consultar oficinas SRI más cercanas en el portal

### Información a Solicitar

Al contactar al SRI, ten lista esta información:
1. ✅ RUC de la empresa
2. ✅ Nombre del representante legal
3. ✅ Correo electrónico de contacto
4. ✅ Tipo de comprobantes a emitir (Facturas, Notas de Crédito, etc.)
5. ✅ Solicitud específica: "Acceso a ambiente de pruebas para facturación electrónica"

---

## 🧪 Paso 8: Validar el Setup

### Checklist de Validación

```
✅ Tengo el certificado digital .p12
✅ Conozco la contraseña del certificado
✅ El certificado no está expirado
✅ Puedo hacer ping/curl a celements.sri.gob.ec
✅ He actualizado appsettings.json con las URLs de pruebas
✅ Tengo RUC válido para pruebas
✅ Conozco los códigos de ambiente y tipo de emisión
```

### Script de Validación (Opcional)

```bash
#!/bin/bash
echo "==================================="
echo "VALIDACIÓN SETUP SRI PRUEBAS"
echo "==================================="

echo "1. Verificando conectividad al SRI..."
curl -s -o /dev/null -w "Status: %{http_code}\n" https://celements.sri.gob.ec

echo "2. Verificando certificado..."
if [ -f "certificado-sri-pruebas.p12" ]; then
    echo "✅ Certificado encontrado"
else
    echo "❌ Certificado no encontrado"
fi

echo "3. URLs configuradas:"
echo "   Recepción: https://celements.sri.gob.ec/.../RecepcionComprobantesOffline"
echo "   Autorización: https://celements.sri.gob.ec/.../AutorizacionComprobantesOffline"

echo "==================================="
echo "Setup completo. Listo para T-070"
echo "==================================="
```

---

## 📚 Recursos Adicionales

### Documentación Oficial
- [Guía Facturación Electrónica SRI](https://www.sri.gob.ec/facturacion-electronica)
- [Especificaciones Técnicas v2.23](https://www.sri.gob.ec/especificaciones-tecnicas)
- [Preguntas Frecuentes](https://www.sri.gob.ec/preguntas-frecuentes)

### Herramientas Útiles
- [Generador de Clave de Acceso](https://www.sri.gob.ec/calculadora-clave-acceso)
- [Validador XML](https://www.sri.gob.ec/validador-xml)
- [Esquemas XSD](https://www.sri.gob.ec/esquemas-xsd)

---

## ⚠️ Notas Importantes

1. **No mezclar ambientes**: Nunca usar certificados de producción en pruebas ni viceversa
2. **Datos ficticios**: Usar RUCs de prueba, no datos de clientes reales
3. **Limpiar datos**: Los comprobantes de prueba no afectan producción pero mantener orden
4. **Certificado vigente**: Aunque sea de pruebas, debe estar vigente
5. **Firewall**: Asegurar que puertos 80/443 estén abiertos para celements.sri.gob.ec

---

## ✅ Resultado Esperado

Al completar T-069, deberías tener:

- ✅ Acceso confirmado al ambiente de pruebas del SRI
- ✅ Certificado digital de pruebas (.p12) descargado
- ✅ Contraseña del certificado conocida
- ✅ URLs de pruebas validadas y funcionales
- ✅ RUC de pruebas asignado
- ✅ Conectividad verificada con celements.sri.gob.ec

---

**Siguiente Paso**: T-070 - Configurar URLs de WebServices en appsettings.json