# 🔐 Firma Electrónica XADES-BES para SRI Ecuador

## T-050: SPRINT 3 - DÍA 6

## ¿Qué es XADES-BES?

**XADES** (XML Advanced Electronic Signatures) es un estándar europeo para firmas electrónicas en documentos XML.

**BES** (Basic Electronic Signature) es el nivel básico que incluye:
- La firma digital del documento
- Información del certificado usado
- Timestamp de cuándo se firmó

## Estructura de la Firma XADES-BES
```xml
<ds:Signature xmlns:ds="http://www.w3.org/2000/09/xmldsig#" Id="Signature123">
  
  <!-- 1. INFORMACIÓN FIRMADA -->
  <ds:SignedInfo>
    <ds:CanonicalizationMethod Algorithm="http://www.w3.org/TR/2001/REC-xml-c14n-20010315"/>
    <ds:SignatureMethod Algorithm="http://www.w3.org/2000/09/xmldsig#rsa-sha1"/>
    <ds:Reference URI="#comprobante">
      <ds:Transforms>
        <ds:Transform Algorithm="http://www.w3.org/2000/09/xmldsig#enveloped-signature"/>
      </ds:Transforms>
      <ds:DigestMethod Algorithm="http://www.w3.org/2000/09/xmldsig#sha1"/>
      <ds:DigestValue>BASE64_HASH_DEL_DOCUMENTO</ds:DigestValue>
    </ds:Reference>
  </ds:SignedInfo>
  
  <!-- 2. VALOR DE LA FIRMA -->
  <ds:SignatureValue>BASE64_FIRMA_DIGITAL</ds:SignatureValue>
  
  <!-- 3. INFORMACIÓN DEL CERTIFICADO -->
  <ds:KeyInfo>
    <ds:X509Data>
      <ds:X509Certificate>BASE64_CERTIFICADO_COMPLETO</ds:X509Certificate>
    </ds:X509Data>
  </ds:KeyInfo>
  
  <!-- 4. PROPIEDADES XADES -->
  <ds:Object Id="Signature123-Object">
    <xades:QualifyingProperties Target="#Signature123">
      <xades:SignedProperties Id="Signature123-SignedProperties">
        <xades:SignedSignatureProperties>
          <xades:SigningTime>2024-11-30T20:00:00-05:00</xades:SigningTime>
          <xades:SigningCertificate>
            <xades:Cert>
              <xades:CertDigest>
                <ds:DigestMethod Algorithm="http://www.w3.org/2000/09/xmldsig#sha1"/>
                <ds:DigestValue>HASH_DEL_CERTIFICADO</ds:DigestValue>
              </xades:CertDigest>
              <xades:IssuerSerial>
                <ds:X509IssuerName>CN=ENTIDAD CERTIFICADORA</ds:X509IssuerName>
                <ds:X509SerialNumber>12345678</ds:X509SerialNumber>
              </xades:IssuerSerial>
            </xades:Cert>
          </xades:SigningCertificate>
        </xades:SignedSignatureProperties>
      </xades:SignedProperties>
    </xades:QualifyingProperties>
  </ds:Object>
  
</ds:Signature>
```

## Proceso de Firma Paso a Paso

### 1. Cargar el Certificado Digital (.p12)
```csharp
var certificado = new X509Certificate2("ruta/certificado.p12", "contraseña");
```

### 2. Calcular el Digest (Hash SHA1) del XML
```csharp
// Canonicalizar el XML
// Calcular SHA1 del XML canonicalizado
// Codificar en Base64
```

### 3. Crear el nodo SignedInfo
```xml
<ds:SignedInfo>
  <!-- Método de canonicalización -->
  <!-- Método de firma (RSA-SHA1) -->
  <!-- Referencia al documento con su digest -->
</ds:SignedInfo>
```

### 4. Firmar el SignedInfo con RSA
```csharp
// Usar la clave privada del certificado
// Firmar con algoritmo RSA-SHA1
// Codificar en Base64
```

### 5. Insertar la Firma en el XML
- El nodo `<ds:Signature>` debe ir **antes** del cierre de `</factura>`
- Debe incluir todos los elementos requeridos

## Librerías .NET Requeridas
```bash
# Criptografía básica de .NET
System.Security.Cryptography.Pkcs
System.Security.Cryptography.Xml

# Para manejar certificados avanzados
BouncyCastle.Cryptography
```

## Validaciones del Certificado

✅ **Debe cumplir:**
1. Formato .p12 o .pfx
2. No estar expirado
3. Ser de una entidad certificadora válida en Ecuador:
   - BCE (Banco Central del Ecuador)
   - Security Data
   - ANF AC
4. Ser un certificado de **firma** (no de encriptación)
5. Tener la cadena de certificación completa

## Errores Comunes

| Error | Causa | Solución |
|-------|-------|----------|
| "Certificado expirado" | Fecha vencida | Renovar certificado |
| "Firma inválida" | Digest incorrecto | Verificar canonicalización |
| "No se puede leer el certificado" | Contraseña incorrecta | Verificar password |
| "Certificado no válido" | Cadena de confianza rota | Instalar certificados raíz |

## Namespaces XML Requeridos
```csharp
xmlns:ds="http://www.w3.org/2000/09/xmldsig#"
xmlns:xades="http://uri.etsi.org/01903/v1.3.2#"
```

## Referencias Oficiales

- **Especificación XADES**: ETSI TS 101 903
- **XML Digital Signature**: W3C Recommendation
- **Ficha Técnica SRI**: v2.21 (Sección Firma Electrónica)

## Flujo Completo de Firma
```
1. Generar XML sin firmar
   ↓
2. Validar XML contra XSD
   ↓
3. Cargar certificado digital
   ↓
4. Validar certificado (vigencia, cadena)
   ↓
5. Calcular digest SHA1 del XML
   ↓
6. Crear nodo SignedInfo
   ↓
7. Firmar SignedInfo con RSA
   ↓
8. Crear nodo Signature completo
   ↓
9. Insertar firma en el XML
   ↓
10. Guardar XML firmado
```

## Siguiente Paso (T-051)

Instalar los paquetes NuGet necesarios para implementar la firma.