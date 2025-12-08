// SistemaFacturacionSRI.Infrastructure/Services/FirmaElectronica/SignatureStructureBuilder.cs
// FIX CRÍTICO: Asegurar que usamos el mismo XmlDocument

using System.Xml;
using System.Security.Cryptography.X509Certificates;

namespace SistemaFacturacionSRI.Infrastructure.Services.FirmaElectronica
{
    /// <summary>
    /// T-058: Construcción de la estructura de firma XADES-BES
    /// T-062: Actualizado para usar SignedPropertiesBuilder
    /// FIX: Garantizar uso del mismo XmlDocument
    /// </summary>
    public class SignatureStructureBuilder
    {
        // Namespaces requeridos por el estándar
        private const string NS_DS = "http://www.w3.org/2000/09/xmldsig#";
        private const string NS_ETSI = "http://uri.etsi.org/01903/v1.3.2#";
        private const string NS_XSI = "http://www.w3.org/2001/XMLSchema-instance";

        private readonly XmlDocument _doc;
        private readonly SignedPropertiesBuilder _signedPropsBuilder;

        public SignatureStructureBuilder(XmlDocument documento)
        {
            _doc = documento ?? throw new ArgumentNullException(nameof(documento));
            _signedPropsBuilder = new SignedPropertiesBuilder(documento);

            // DIAGNÓSTICO: Log del HashCode para verificar que es el mismo documento
            Console.WriteLine($"[DIAG] SignatureStructureBuilder creado con doc HashCode: {_doc.GetHashCode()}");
        }

        /// <summary>
        /// T-058: Crea el nodo raíz Signature con todos sus componentes
        /// T-062: Actualizado para calcular digest de SignedProperties
        /// </summary>
        public XmlElement CrearNodoSignature(
            string signatureValue,
            string digestValue,
            string certificadoBase64,
            X509Certificate2 certificado,
            DateTime fechaFirma,
            string idNodoFirmar = "comprobante")
        {
            // CRÍTICO: Usar _doc para crear TODOS los elementos
            Console.WriteLine($"[DIAG] CrearNodoSignature usando doc HashCode: {_doc.GetHashCode()}");

            // Crear nodo raíz Signature USANDO _doc
            var signature = _doc.CreateElement("ds", "Signature", NS_DS);
            signature.SetAttribute("Id", "Signature");
            signature.SetAttribute("xmlns:etsi", NS_ETSI);

            // 1. Crear Object con SignedProperties primero (lo necesitamos para el digest)
            var objectNode = _signedPropsBuilder.CrearObject(certificado, fechaFirma);

            // 2. Extraer SignedProperties para calcular su digest
            var nsmgr = new XmlNamespaceManager(_doc.NameTable);
            nsmgr.AddNamespace("etsi", NS_ETSI);
            var signedProps = objectNode.SelectSingleNode("//etsi:SignedProperties", nsmgr) as XmlElement;

            if (signedProps == null)
            {
                throw new InvalidOperationException("No se pudo crear SignedProperties");
            }

            // 3. Calcular digest de SignedProperties
            var digestSignedProps = _signedPropsBuilder.CalcularDigestSignedProperties(signedProps);

            // 4. Crear SignedInfo con el digest correcto de SignedProperties
            var signedInfo = CrearSignedInfo(digestValue, digestSignedProps, idNodoFirmar);
            signature.AppendChild(signedInfo);

            // 5. Agregar SignatureValue
            var signatureValueNode = CrearSignatureValue(signatureValue);
            signature.AppendChild(signatureValueNode);

            // 6. Agregar KeyInfo
            var keyInfo = CrearKeyInfo(certificadoBase64, certificado);
            signature.AppendChild(keyInfo);

            // 7. Agregar Object (con SignedProperties)
            signature.AppendChild(objectNode);

            // VERIFICACIÓN: El nodo pertenece al documento correcto
            Console.WriteLine($"[DIAG] Signature.OwnerDocument HashCode: {signature.OwnerDocument?.GetHashCode()}");
            Console.WriteLine($"[DIAG] ¿Mismo documento? {signature.OwnerDocument == _doc}");

            return signature;
        }

        /// <summary>
        /// T-058: Crea el nodo SignedInfo con la información que será firmada
        /// T-062: Actualizado para incluir digest real de SignedProperties
        /// </summary>
        private XmlElement CrearSignedInfo(
            string digestValue,
            string digestSignedProps,
            string idNodoFirmar)
        {
            var signedInfo = _doc.CreateElement("ds", "SignedInfo", NS_DS);
            signedInfo.SetAttribute("Id", "Signature-SignedInfo");

            // CanonicalizationMethod
            var canonicalization = _doc.CreateElement("ds", "CanonicalizationMethod", NS_DS);
            canonicalization.SetAttribute("Algorithm", "http://www.w3.org/TR/2001/REC-xml-c14n-20010315");
            signedInfo.AppendChild(canonicalization);

            // SignatureMethod (RSA-SHA1 requerido por SRI)
            var signatureMethod = _doc.CreateElement("ds", "SignatureMethod", NS_DS);
            signatureMethod.SetAttribute("Algorithm", "http://www.w3.org/2000/09/xmldsig#rsa-sha1");
            signedInfo.AppendChild(signatureMethod);

            // Reference al documento
            var reference = CrearReference(digestValue, idNodoFirmar);
            signedInfo.AppendChild(reference);

            // Reference a SignedProperties (CON EL DIGEST CORRECTO)
            var referenceProps = CrearReferenceSignedProperties(digestSignedProps);
            signedInfo.AppendChild(referenceProps);

            return signedInfo;
        }

        /// <summary>
        /// T-058: Crea Reference al documento principal
        /// </summary>
        private XmlElement CrearReference(string digestValue, string idNodoFirmar)
        {
            var reference = _doc.CreateElement("ds", "Reference", NS_DS);
            reference.SetAttribute("Id", "SignedPropertiesID");
            reference.SetAttribute("URI", $"#{idNodoFirmar}");

            // Transforms
            var transforms = _doc.CreateElement("ds", "Transforms", NS_DS);
            var transform = _doc.CreateElement("ds", "Transform", NS_DS);
            transform.SetAttribute("Algorithm", "http://www.w3.org/2000/09/xmldsig#enveloped-signature");
            transforms.AppendChild(transform);
            reference.AppendChild(transforms);

            // DigestMethod (SHA1 requerido por SRI)
            var digestMethod = _doc.CreateElement("ds", "DigestMethod", NS_DS);
            digestMethod.SetAttribute("Algorithm", "http://www.w3.org/2000/09/xmldsig#sha1");
            reference.AppendChild(digestMethod);

            // DigestValue
            var digestValueNode = _doc.CreateElement("ds", "DigestValue", NS_DS);
            digestValueNode.InnerText = digestValue;
            reference.AppendChild(digestValueNode);

            return reference;
        }

        /// <summary>
        /// T-058: Crea Reference a SignedProperties
        /// T-062: Actualizado para recibir el digest correcto
        /// </summary>
        private XmlElement CrearReferenceSignedProperties(string digestValue)
        {
            var reference = _doc.CreateElement("ds", "Reference", NS_DS);
            reference.SetAttribute("Type", "http://uri.etsi.org/01903#SignedProperties");
            reference.SetAttribute("URI", "#Signature-SignedProperties");

            // DigestMethod
            var digestMethod = _doc.CreateElement("ds", "DigestMethod", NS_DS);
            digestMethod.SetAttribute("Algorithm", "http://www.w3.org/2000/09/xmldsig#sha1");
            reference.AppendChild(digestMethod);

            // DigestValue (AHORA CON VALOR REAL)
            var digestValueNode = _doc.CreateElement("ds", "DigestValue", NS_DS);
            digestValueNode.InnerText = digestValue;
            reference.AppendChild(digestValueNode);

            return reference;
        }

        /// <summary>
        /// T-058: Crea el nodo SignatureValue
        /// </summary>
        private XmlElement CrearSignatureValue(string signatureValue)
        {
            var signatureValueNode = _doc.CreateElement("ds", "SignatureValue", NS_DS);
            signatureValueNode.SetAttribute("Id", "SignatureValue");
            signatureValueNode.InnerText = signatureValue;
            return signatureValueNode;
        }

        /// <summary>
        /// T-061: Crea el nodo KeyInfo con el certificado
        /// </summary>
        private XmlElement CrearKeyInfo(string certificadoBase64, X509Certificate2 certificado)
        {
            var keyInfo = _doc.CreateElement("ds", "KeyInfo", NS_DS);
            keyInfo.SetAttribute("Id", "Certificate");

            // X509Data
            var x509Data = _doc.CreateElement("ds", "X509Data", NS_DS);

            // X509Certificate (certificado completo en Base64)
            var x509Certificate = _doc.CreateElement("ds", "X509Certificate", NS_DS);
            x509Certificate.InnerText = certificadoBase64;
            x509Data.AppendChild(x509Certificate);

            keyInfo.AppendChild(x509Data);
            return keyInfo;
        }
    }
}