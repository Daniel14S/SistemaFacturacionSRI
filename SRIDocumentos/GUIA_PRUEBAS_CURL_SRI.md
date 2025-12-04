# Guía de Pruebas con cURL - WebServices SRI

## 🧪 Pruebas de Conectividad con cURL

---

## ⚠️ IMPORTANTE: URLs Correctas

Las URLs del SRI **NO** llevan `?wsdl` al final cuando se consumen los servicios SOAP.

- ❌ **INCORRECTO**: `https://celements.sri.gob.ec/...RecepcionComprobantesOffline?wsdl`
- ✅ **CORRECTO**: `https://celements.sri.gob.ec/...RecepcionComprobantesOffline`

El `?wsdl` es solo para **ver la descripción** del servicio, NO para consumirlo.

---

## 1️⃣ Test de Conectividad Básica (PING)

### Windows (PowerShell)
```powershell
# Probar conectividad
Test-NetConnection celements.sri.gob.ec -Port 443

# Alternativa con curl
curl -I https://celements.sri.gob.ec/comprobantes-electronicos-ws/RecepcionComprobantesOffline
```

### Linux/Mac
```bash
# Probar conectividad
ping celements.sri.gob.ec

# Probar puerto HTTPS
nc -zv celements.sri.gob.ec 443

# Ver headers HTTP
curl -I https://celements.sri.gob.ec/comprobantes-electronicos-ws/RecepcionComprobantesOffline
```

**Respuesta esperada**: 
- Debe responder con código 200, 405 (Method Not Allowed) o similar
- Lo importante es que **NO** de timeout o error de DNS

---

## 2️⃣ Test de WSDL (Ver descripción del servicio)

### Ver WSDL de Recepción
```bash
curl "https://celements.sri.gob.ec/comprobantes-electronicos-ws/RecepcionComprobantesOffline?wsdl"
```

**Respuesta esperada**: XML con la definición WSDL del servicio

### Ver WSDL de Autorización
```bash
curl "https://celements.sri.gob.ec/comprobantes-electronicos-ws/AutorizacionComprobantesOffline?wsdl"
```

**Respuesta esperada**: XML con la definición WSDL del servicio

---

## 3️⃣ Test de Recepción (Request SOAP Vacío)

### Windows (PowerShell)
```powershell
# Guardar el XML SOAP en un archivo
@"
<?xml version="1.0" encoding="UTF-8"?>
<soapenv:Envelope xmlns:soapenv="http://schemas.xmlsoap.org/soap/envelope/" 
                  xmlns:ec="http://ec.gob.sri.ws.recepcion">
   <soapenv:Header/>
   <soapenv:Body>
      <ec:validarComprobante>
         <xml><![CDATA[TEST]]></xml>
      </ec:validarComprobante>
   </soapenv:Body>
</soapenv:Envelope>
"@ | Out-File -FilePath test-recepcion.xml -Encoding UTF8

# Enviar con curl
curl.exe -X POST "https://celements.sri.gob.ec/comprobantes-electronicos-ws/RecepcionComprobantesOffline" `
  -H "Content-Type: text/xml; charset=utf-8" `
  -H "SOAPAction: " `
  --data-binary "@test-recepcion.xml" `
  -v
```

### Linux/Mac
```bash
# Test de Recepción
curl -X POST "https://celements.sri.gob.ec/comprobantes-electronicos-ws/RecepcionComprobantesOffline" \
  -H "Content-Type: text/xml; charset=utf-8" \
  -H "SOAPAction: " \
  -d '<?xml version="1.0" encoding="UTF-8"?>
<soapenv:Envelope xmlns:soapenv="http://schemas.xmlsoap.org/soap/envelope/" 
                  xmlns:ec="http://ec.gob.sri.ws.recepcion">
   <soapenv:Header/>
   <soapenv:Body>
      <ec:validarComprobante>
         <xml><![CDATA[TEST]]></xml>
      </ec:validarComprobante>
   </soapenv:Body>
</soapenv:Envelope>' \
  -v
```

**Respuesta esperada**:
```xml
<?xml version="1.0" encoding="UTF-8"?>
<soap:Envelope xmlns:soap="http://schemas.xmlsoap.org/soap/envelope/">
   <soap:Body>
      <ns2:validarComprobanteResponse xmlns:ns2="http://ec.gob.sri.ws.recepcion">
         <RespuestaRecepcionComprobante>
            <estado>DEVUELTA</estado>
            <comprobantes>
               <comprobante>
                  <mensajes>
                     <mensaje>
                        <identificador>XX</identificador>
                        <mensaje>ERROR: XML mal formado o inválido</mensaje>
                        <tipo>ERROR</tipo>
                     </mensaje>
                  </mensajes>
               </comprobante>
            </comprobantes>
         </RespuestaRecepcionComprobante>
      </ns2:validarComprobanteResponse>
   </soap:Body>
</soap:Envelope>
```

✅ **Interpretación**: 
- Si recibes un XML con `<estado>DEVUELTA</estado>` y mensajes de error → **CONEXIÓN EXITOSA**
- El error es esperado porque enviamos un XML de prueba inválido
- Lo importante es que el SRI **respondió**

---

## 4️⃣ Test de Autorización (Request SOAP con Clave de Acceso)

### Windows (PowerShell)
```powershell
@"
<?xml version="1.0" encoding="UTF-8"?>
<soapenv:Envelope xmlns:soapenv="http://schemas.xmlsoap.org/soap/envelope/" 
                  xmlns:ec="http://ec.gob.sri.ws.autorizacion">
   <soapenv:Header/>
   <soapenv:Body>
      <ec:autorizacionComprobante>
         <claveAccesoComprobante>0000000000000000000000000000000000000000000000000</claveAccesoComprobante>
      </ec:autorizacionComprobante>
   </soapenv:Body>
</soapenv:Envelope>
"@ | Out-File -FilePath test-autorizacion.xml -Encoding UTF8

curl.exe -X POST "https://celements.sri.gob.ec/comprobantes-electronicos-ws/AutorizacionComprobantesOffline" `
  -H "Content-Type: text/xml; charset=utf-8" `
  -H "SOAPAction: " `
  --data-binary "@test-autorizacion.xml" `
  -v
```

### Linux/Mac
```bash
curl -X POST "https://celements.sri.gob.ec/comprobantes-electronicos-ws/AutorizacionComprobantesOffline" \
  -H "Content-Type: text/xml; charset=utf-8" \
  -H "SOAPAction: " \
  -d '<?xml version="1.0" encoding="UTF-8"?>
<soapenv:Envelope xmlns:soapenv="http://schemas.xmlsoap.org/soap/envelope/" 
                  xmlns:ec="http://ec.gob.sri.ws.autorizacion">
   <soapenv:Header/>
   <soapenv:Body>
      <ec:autorizacionComprobante>
         <claveAccesoComprobante>0000000000000000000000000000000000000000000000000</claveAccesoComprobante>
      </ec:autorizacionComprobante>
   </soapenv:Body>
</soapenv:Envelope>' \
  -v
```

**Respuesta esperada**:
```xml
<?xml version="1.0" encoding="UTF-8"?>
<soap:Envelope xmlns:soap="http://schemas.xmlsoap.org/soap/envelope/">
   <soap:Body>
      <ns2:autorizacionComprobanteResponse xmlns:ns2="http://ec.gob.sri.ws.autorizacion">
         <RespuestaAutorizacionComprobante>
            <claveAccesoConsultada>0000000000000000000000000000000000000000000000000</claveAccesoConsultada>
            <numeroComprobantes>0</numeroComprobantes>
            <autorizaciones/>
         </RespuestaAutorizacionComprobante>
      </ns2:autorizacionComprobanteResponse>
   </soap:Body>
</soap:Envelope>
```

✅ **Interpretación**: 
- Si recibes un XML con `<numeroComprobantes>0</numeroComprobantes>` → **CONEXIÓN EXITOSA**
- No encuentra comprobantes porque la clave de acceso es ficticia
- Lo importante es que el SRI **respondió**

---

## 5️⃣ Test Completo con Timeout

```bash
# Test con timeout de 10 segundos
curl -X POST "https://celements.sri.gob.ec/comprobantes-electronicos-ws/RecepcionComprobantesOffline" \
  -H "Content-Type: text/xml; charset=utf-8" \
  -H "SOAPAction: " \
  -H "User-Agent: SistemaFacturacionSRI/1.0" \
  -d '<soapenv:Envelope xmlns:soapenv="http://schemas.xmlsoap.org/soap/envelope/" xmlns:ec="http://ec.gob.sri.ws.recepcion"><soapenv:Header/><soapenv:Body><ec:validarComprobante><xml><![CDATA[TEST]]></xml></ec:validarComprobante></soapenv:Body></soapenv:Envelope>' \
  --max-time 10 \
  --connect-timeout 5 \
  -v
```

---

## 6️⃣ Verificar Certificado SSL del SRI

```bash
# Ver información del certificado SSL
openssl s_client -connect celements.sri.gob.ec:443 -servername celements.sri.gob.ec < /dev/null

# Verificar validez del certificado
curl -v https://celements.sri.gob.ec 2>&1 | grep -i "ssl"
```

---

## 📊 Checklist de Validación

Marca cada ítem conforme lo completes:

- [ ] ✅ `ping celements.sri.gob.ec` responde
- [ ] ✅ `curl -I https://celements.sri.gob.ec/...` responde (no timeout)
- [ ] ✅ WSDL de Recepción se descarga correctamente
- [ ] ✅ WSDL de Autorización se descarga correctamente
- [ ] ✅ Request SOAP a Recepción responde (aunque sea con error)
- [ ] ✅ Request SOAP a Autorización responde (aunque sea con error)
- [ ] ✅ Certificado SSL del SRI es válido
- [ ] ✅ No hay errores de firewall/proxy

---

## 🐛 Troubleshooting

### Error: "Could not resolve host"
```
❌ Problema: No se puede resolver el DNS de celements.sri.gob.ec
✅ Solución: 
   - Verificar conexión a internet
   - Verificar DNS (usar 8.8.8.8 de Google)
   - Verificar firewall
```

### Error: "Connection timeout"
```
❌ Problema: El servidor no responde en el tiempo esperado
✅ Solución:
   - Aumentar timeout: --max-time 30
   - Verificar que no haya proxy/firewall bloqueando
   - Probar desde otra red
```

### Error: "SSL certificate problem"
```
❌ Problema: Certificado SSL inválido o expirado
✅ Solución:
   - Actualizar certificados del sistema
   - Usar: curl -k (solo para testing, NO en producción)
   - Verificar fecha/hora del sistema
```

### Error: "Empty reply from server"
```
❌ Problema: El servidor cerró la conexión sin responder
✅ Solución:
   - Verificar que la URL NO tenga ?wsdl al final
   - Verificar headers (Content-Type, SOAPAction)
   - Revisar el XML SOAP (debe estar bien formado)
```

---

## 🎯 Resultado Esperado

Después de ejecutar estos tests, deberías poder confirmar:

1. ✅ Conectividad con `celements.sri.gob.ec` (ambiente pruebas)
2. ✅ Los servicios SOAP responden (aunque rechacen el XML de prueba)
3. ✅ No hay problemas de firewall/proxy
4. ✅ El certificado SSL del SRI es válido
5. ✅ Tu aplicación podrá conectarse cuando envíe XMLs reales firmados

---

## 📝 Guardar Logs para Debugging

```bash
# Guardar respuesta completa en archivo
curl -X POST "https://celements.sri.gob.ec/comprobantes-electronicos-ws/RecepcionComprobantesOffline" \
  -H "Content-Type: text/xml; charset=utf-8" \
  -H "SOAPAction: " \
  -d '<soapenv:Envelope xmlns:soapenv="http://schemas.xmlsoap.org/soap/envelope/" xmlns:ec="http://ec.gob.sri.ws.recepcion"><soapenv:Header/><soapenv:Body><ec:validarComprobante><xml><![CDATA[TEST]]></xml></ec:validarComprobante></soapenv:Body></soapenv:Envelope>' \
  -v \
  > response.xml 2> debug.log

# Ver respuesta
cat response.xml

# Ver logs de debug
cat debug.log
```

---

**Próximo paso**: Si todos los tests pasan, estás listo para implementar el cliente SOAP en C# (T-075 a T-079).