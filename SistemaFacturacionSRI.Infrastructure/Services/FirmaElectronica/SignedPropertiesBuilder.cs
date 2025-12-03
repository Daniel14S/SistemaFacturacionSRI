// SistemaFacturacionSRI.Infrastructure/Services/FirmaElectronica/SignedPropertiesBuilder.cs

using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Xml;

namespace SistemaFacturacionSRI.Infrastructure.Services.FirmaElectronica
{
    /// <summary>
    /// T-062: Constructor de SignedProperties para XADES-BES
    /// Maneja la creación y cálculo de digest de las propiedades firmadas
    /// </summary>
    public class SignedPropertiesBuilder
    {
        private readonly XmlDocument _doc;
        private const string NS_DS = "http://www.w3.org/2000/09/xmldsig#";
        private const string NS_ETSI = "http://uri.etsi.org/01903/v1.3.2#";

        public SignedPropertiesBuilder(XmlDocument documento)
        {
            _doc = documento;
        }

        /// <summary>
        /// T-062: Crea el nodo Object completo con QualifyingProperties
        /// </summary>
        public XmlElement CrearObject(X509Certificate2 certificado, DateTime fechaFirma)
        {
            var objectNode = _doc.CreateElement("ds", "Object", NS_DS);
            objectNode.SetAttribute("Id", "Signature-Object");

            // QualifyingProperties
            var qualifyingProps = _doc.CreateElement("etsi", "QualifyingProperties", NS_ETSI);
            qualifyingProps.SetAttribute("Target", "#Signature");

            // SignedProperties
            var signedProps = CrearSignedProperties(certificado, fechaFirma);
            qualifyingProps.AppendChild(signedProps);

            objectNode.AppendChild(qualifyingProps);
            return objectNode;
        }

        /// <summary>
        /// T-062: Crea el nodo SignedProperties con toda su estructura
        /// </summary>
        public XmlElement CrearSignedProperties(X509Certificate2 certificado, DateTime fechaFirma)
        {
            var signedProps = _doc.CreateElement("etsi", "SignedProperties", NS_ETSI);
            signedProps.SetAttribute("Id", "Signature-SignedProperties");

            // SignedSignatureProperties (contiene SigningTime y SigningCertificate)
            var signedSigProps = CrearSignedSignatureProperties(certificado, fechaFirma);
            signedProps.AppendChild(signedSigProps);

            return signedProps;
        }

        /// <summary>
        /// T-062: Crea SignedSignatureProperties
        /// </summary>
        private XmlElement CrearSignedSignatureProperties(X509Certificate2 certificado, DateTime fechaFirma)
        {
            var signedSigProps = _doc.CreateElement("etsi", "SignedSignatureProperties", NS_ETSI);

            // 1. SigningTime (fecha y hora de la firma)
            var signingTime = CrearSigningTime(fechaFirma);
            signedSigProps.AppendChild(signingTime);

            // 2. SigningCertificate (información del certificado usado)
            var signingCert = CrearSigningCertificate(certificado);
            signedSigProps.AppendChild(signingCert);

            // 3. SignaturePolicyIdentifier (opcional, SRI no lo requiere siempre)
            // var policyId = CrearSignaturePolicyIdentifier();
            // signedSigProps.AppendChild(policyId);

            return signedSigProps;
        }

        /// <summary>
        /// T-062: Crea el nodo SigningTime
        /// </summary>
        private XmlElement CrearSigningTime(DateTime fechaFirma)
        {
            var signingTime = _doc.CreateElement("etsi", "SigningTime", NS_ETSI);
            
            // Formato ISO 8601 UTC (requerido por XADES)
            // Ejemplo: 2024-12-03T15:30:45Z
            signingTime.InnerText = fechaFirma.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ");
            
            return signingTime;
        }

        /// <summary>
        /// T-062: Crea el nodo SigningCertificate con información completa del certificado
        /// </summary>
        private XmlElement CrearSigningCertificate(X509Certificate2 certificado)
        {
            var signingCert = _doc.CreateElement("etsi", "SigningCertificate", NS_ETSI);

            // Cert (contiene CertDigest e IssuerSerial)
            var cert = _doc.CreateElement("etsi", "Cert", NS_ETSI);

            // 1. CertDigest (hash del certificado)
            var certDigest = CrearCertDigest(certificado);
            cert.AppendChild(certDigest);

            // 2. IssuerSerial (emisor y número de serie)
            var issuerSerial = CrearIssuerSerial(certificado);
            cert.AppendChild(issuerSerial);

            signingCert.AppendChild(cert);
            return signingCert;
        }

        /// <summary>
        /// T-062: Crea el digest (hash SHA1) del certificado
        /// </summary>
        private XmlElement CrearCertDigest(X509Certificate2 certificado)
        {
            var certDigest = _doc.CreateElement("etsi", "CertDigest", NS_ETSI);

            // DigestMethod (SHA1 requerido por SRI)
            var digestMethod = _doc.CreateElement("ds", "DigestMethod", NS_DS);
            digestMethod.SetAttribute("Algorithm", "http://www.w3.org/2000/09/xmldsig#sha1");
            certDigest.AppendChild(digestMethod);

            // DigestValue (hash del certificado en Base64)
            var digestValue = _doc.CreateElement("ds", "DigestValue", NS_DS);
            using (var sha1 = SHA1.Create())
            {
                var hash = sha1.ComputeHash(certificado.RawData);
                digestValue.InnerText = Convert.ToBase64String(hash);
            }
            certDigest.AppendChild(digestValue);

            return certDigest;
        }

        /// <summary>
        /// T-062: Crea IssuerSerial con información del emisor y serial
        /// </summary>
        private XmlElement CrearIssuerSerial(X509Certificate2 certificado)
        {
            var issuerSerial = _doc.CreateElement("etsi", "IssuerSerial", NS_ETSI);

            // X509IssuerName
            var x509IssuerName = _doc.CreateElement("ds", "X509IssuerName", NS_DS);
            x509IssuerName.InnerText = certificado.IssuerName.Name;
            issuerSerial.AppendChild(x509IssuerName);

            // X509SerialNumber
            var x509SerialNumber = _doc.CreateElement("ds", "X509SerialNumber", NS_DS);
            x509SerialNumber.InnerText = certificado.SerialNumber;
            issuerSerial.AppendChild(x509SerialNumber);

            return issuerSerial;
        }

        /// <summary>
        /// T-062: Calcula el digest de SignedProperties
        /// Este digest debe incluirse en el Reference de SignedInfo
        /// </summary>
        public string CalcularDigestSignedProperties(XmlElement signedProperties)
        {
            // Canonicalizar el XML de SignedProperties
            var xmlString = CanonicalizarNodo(signedProperties);

            // Calcular SHA1
            using (var sha1 = SHA1.Create())
            {
                var bytes = Encoding.UTF8.GetBytes(xmlString);
                var hash = sha1.ComputeHash(bytes);
                return Convert.ToBase64String(hash);
            }
        }

        /// <summary>
        /// T-062: Canonicalización simplificada para SRI
        /// </summary>
        private string CanonicalizarNodo(XmlNode nodo)
        {
            var xml = nodo.OuterXml;

            // Normalizar espacios en blanco
            xml = System.Text.RegularExpressions.Regex.Replace(xml, @"\s+", " ");
            xml = xml.Replace("> <", "><");

            return xml.Trim();
        }

        /// <summary>
        /// T-062: Actualiza el Reference a SignedProperties con el digest correcto
        /// </summary>
        public void ActualizarReferenceSignedProperties(XmlElement signedInfo, string digestValue)
        {
            var nsmgr = new XmlNamespaceManager(_doc.NameTable);
            nsmgr.AddNamespace("ds", NS_DS);

            // Buscar el Reference a SignedProperties
            var reference = signedInfo.SelectSingleNode(
                "ds:Reference[@Type='http://uri.etsi.org/01903#SignedProperties']", 
                nsmgr) as XmlElement;

            if (reference != null)
            {
                // Actualizar DigestValue
                var digestValueNode = reference.SelectSingleNode("ds:DigestValue", nsmgr) as XmlElement;
                if (digestValueNode != null)
                {
                    digestValueNode.InnerText = digestValue;
                }
            }
        }

        /// <summary>
        /// T-062: Crea SignaturePolicyIdentifier (opcional)
        /// Algunos certificados del SRI pueden requerirlo
        /// </summary>
        private XmlElement CrearSignaturePolicyIdentifier()
        {
            var policyId = _doc.CreateElement("etsi", "SignaturePolicyIdentifier", NS_ETSI);
            
            var policyImplied = _doc.CreateElement("etsi", "SignaturePolicyImplied", NS_ETSI);
            policyId.AppendChild(policyImplied);

            return policyId;
        }

        /// <summary>
        /// T-062: Valida la estructura de SignedProperties
        /// </summary>
        public bool ValidarEstructura(XmlElement signedProperties)
        {
            try
            {
                var nsmgr = new XmlNamespaceManager(_doc.NameTable);
                nsmgr.AddNamespace("etsi", NS_ETSI);
                nsmgr.AddNamespace("ds", NS_DS);

                // Verificar que existe SignedSignatureProperties
                var signedSigProps = signedProperties.SelectSingleNode(
                    "etsi:SignedSignatureProperties", nsmgr);
                if (signedSigProps == null) return false;

                // Verificar SigningTime
                var signingTime = signedSigProps.SelectSingleNode("etsi:SigningTime", nsmgr);
                if (signingTime == null || string.IsNullOrWhiteSpace(signingTime.InnerText)) 
                    return false;

                // Verificar SigningCertificate
                var signingCert = signedSigProps.SelectSingleNode("etsi:SigningCertificate", nsmgr);
                if (signingCert == null) return false;

                // Verificar CertDigest
                var certDigest = signingCert.SelectSingleNode(".//etsi:CertDigest", nsmgr);
                if (certDigest == null) return false;

                // Verificar IssuerSerial
                var issuerSerial = signingCert.SelectSingleNode(".//etsi:IssuerSerial", nsmgr);
                if (issuerSerial == null) return false;

                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}