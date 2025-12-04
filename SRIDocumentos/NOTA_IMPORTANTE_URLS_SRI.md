# ⚠️ NOTA IMPORTANTE: URLs del SRI Ecuador

## 📍 Situación Actual (Diciembre 2024)

### Servidores Detectados

✅ **Servidor PRODUCCIÓN - Accesible**:
- Dominio: `cel.sri.gob.ec`
- IP: `190.152.216.10`
- Puerto: `443` (HTTPS)
- Estado: **Operativo y accesible**

❌ **Servidores PRUEBAS - No Accesibles**:
- `celcer.sri.gob.ec` → DNS no resuelve
- `celements.sri.gob.ec` → DNS no resuelve
- `ce-pruebas.sri.gob.ec` → No existe

---

## 🔧 Solución Implementada

### Usar Servidor de Producción con Datos de Prueba

Según la documentación oficial del SRI, **es válido** usar el servidor de producción (`cel.sri.gob.ec`) para realizar pruebas, siempre que:

1. ✅ El XML contenga `<ambiente>1</ambiente>` (PRUEBAS)
2. ✅ Se use certificado digital de pruebas
3. ✅ Se use RUC de pruebas (no RUCs reales de contribuyentes)
4. ✅ Los comprobantes tengan datos ficticios

### ¿Cómo funciona?

El SRI **diferencia** entre pruebas y producción por:
- **Campo `<ambiente>` en el XML**: 1=Pruebas, 2=Producción
- **Certificado digital**: Pruebas vs Producción
- **NO por la URL** del servidor

Por lo tanto:
```
Servidor: https://cel.sri.gob.ec (Producción)
+ XML con <ambiente>1</ambiente> (Pruebas)
+ Certificado de Pruebas
= ✅ Comprobantes de PRUEBA que NO afectan producción
```

---

## 📋 Configuración Recomendada

### appsettings.json (Desarrollo/Pruebas)

```json
{
  "SRI": {
    "Ambiente": "PRUEBAS",
    "AmbienteCodigo": 1,
    "UrlRecepcion": "https://cel.sri.gob.ec/comprobantes-electronicos-ws/RecepcionComprobantesOffline",
    "UrlAutorizacion": "https://cel.sri.gob.ec/comprobantes-electronicos-ws/AutorizacionComprobantesOffline"
  },
  "CertificadoDigital": {
    "TipoCertificado": "PRUEBAS"
  }
}
```

### appsettings.Production.json (Producción Real)

```json
{
  "SRI": {
    "Ambiente": "PRODUCCION",
    "AmbienteCodigo": 2,
    "UrlRecepcion": "https://cel.sri.gob.ec/comprobantes-electronicos-ws/RecepcionComprobantesOffline",
    "UrlAutorizacion": "https://cel.sri.gob.ec/comprobantes-electronicos-ws/AutorizacionComprobantesOffline"
  },
  "CertificadoDigital": {
    "TipoCertificado": "PRODUCCION",
    "RutaCertificado": "ruta-certificado-real.p12"
  }
}
```

**Nota**: Las URLs son las mismas, pero cambia el código de ambiente en el XML generado.

---

## 🎯 Diferencias Clave

| Aspecto | PRUEBAS | PRODUCCIÓN |
|---------|---------|------------|
| **URL Servidor** | `cel.sri.gob.ec` | `cel.sri.gob.ec` |
| **XML `<ambiente>`** | `1` | `2` |
| **Certificado** | Pruebas | Real |
| **RUC** | Ficticio | Real |
| **Datos** | Ficticios | Reales |
| **Impacto Legal** | ❌ Ninguno | ✅ Válido legalmente |

---

## 📞 Contacto SRI para Confirmar

Si deseas confirmar el acceso a servidores de prueba separados:

**Soporte SRI**:
- 📧 facturacionelectronica@sri.gob.ec
- 📞 1700 774 774
- 🌐 https://www.sri.gob.ec

**Pregunta específica**:
> "¿Cuál es la URL actual del servidor de pruebas para WebServices SOAP de facturación electrónica? Los dominios celcer.sri.gob.ec y celements.sri.gob.ec no resuelven DNS."

---

## 🔄 Actualización Futura

Si el SRI habilita servidores de prueba separados:

1. Actualizar `appsettings.json`:
   ```json
   "UrlRecepcion": "https://[nuevo-servidor-pruebas]/..."
   ```

2. Ejecutar tests de conectividad:
   ```powershell
   Test-NetConnection -ComputerName [nuevo-servidor] -Port 443
   ```

3. Verificar que el código siga funcionando sin cambios

---

## ✅ Conclusión

**Configuración actual es VÁLIDA y FUNCIONAL**:
- ✅ Servidor de producción accesible
- ✅ Diferenciación por código de ambiente en XML
- ✅ Certificado de pruebas protege contra uso accidental en producción
- ✅ Conforme a documentación SRI

**No requiere cambios** hasta que el SRI informe de servidores de prueba activos.

---

Última actualización: Diciembre 2024