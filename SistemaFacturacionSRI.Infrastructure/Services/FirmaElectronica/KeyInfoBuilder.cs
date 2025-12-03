// SistemaFacturacionSRI.Infrastructure/Services/FirmaElectronica/KeyInfoBuilder.cs
using System.Security.Cryptography.X509Certificates;
using System.Xml;

namespace SistemaFacturacionSRI.Infrastructure.Services.FirmaElectronica
{
    /// <summary>
    /// T-061: Constructor del nodo KeyInfo con certificado X509
    /// </summary>
    public class KeyInfoBuilder
    {
        private readonly XmlDocument _doc;
        private const string NS_DS = "http://www.w3.org/2000/09/xmldsig#";

        public KeyInfoBuilder(XmlDocument documento)
        {
            _doc = documento;
        }

        /// <summary>
        /// T-061: Crea el nodo KeyInfo completo con el certificado
        /// </summary>
        public XmlElement CrearKeyInfo(X509Certificate2 certificado)
        {
            // Exportar certificado a Base64
            var certBytes = certificado.Export(X509ContentType.Cert);
            var certBase64 = Convert.ToBase64String(certBytes);

            return CrearKeyInfo(certBase64, certificado);
        }

        /// <summary>
        /// T-061: Crea KeyInfo con certificado ya en Base64
        /// </summary>
        public XmlElement CrearKeyInfo(string certificadoBase64, X509Certificate2 certificado)
        {
            var keyInfo = _doc.CreateElement("ds", "KeyInfo", NS_DS);
            keyInfo.SetAttribute("Id", "Certificate");

            // X509Data
            var x509Data = CrearX509Data(certificadoBase64, certificado);
            keyInfo.AppendChild(x509Data);

            return keyInfo;
        }

        /// <summary>
        /// T-061: Crea el nodo X509Data con toda la información del certificado
        /// </summary>
        private XmlElement CrearX509Data(string certificadoBase64, X509Certificate2 certificado)
        {
            var x509Data = _doc.CreateElement("ds", "X509Data", NS_DS);

            // 1. X509Certificate (certificado completo en Base64)
            var x509Certificate = _doc.CreateElement("ds", "X509Certificate", NS_DS);
            x509Certificate.InnerText = certificadoBase64;
            x509Data.AppendChild(x509Certificate);

            // 2. X509IssuerSerial (opcional pero recomendado)
            var issuerSerial = CrearX509IssuerSerial(certificado);
            x509Data.AppendChild(issuerSerial);

            // 3. X509SubjectName (opcional)
            var subjectName = _doc.CreateElement("ds", "X509SubjectName", NS_DS);
            subjectName.InnerText = certificado.SubjectName.Name;
            x509Data.AppendChild(subjectName);

            return x509Data;
        }

        /// <summary>
        /// T-061: Crea el nodo X509IssuerSerial
        /// </summary>
        private XmlElement CrearX509IssuerSerial(X509Certificate2 certificado)
        {
            var issuerSerial = _doc.CreateElement("ds", "X509IssuerSerial", NS_DS);

            // X509IssuerName
            var issuerName = _doc.CreateElement("ds", "X509IssuerName", NS_DS);
            issuerName.InnerText = certificado.IssuerName.Name;
            issuerSerial.AppendChild(issuerName);

            // X509SerialNumber
            var serialNumber = _doc.CreateElement("ds", "X509SerialNumber", NS_DS);
            serialNumber.InnerText = certificado.SerialNumber;
            issuerSerial.AppendChild(serialNumber);

            return issuerSerial;
        }

        /// <summary>
        /// T-061: Extrae el certificado desde un nodo KeyInfo
        /// </summary>
        public X509Certificate2? ExtraerCertificado(XmlElement keyInfo)
        {
            try
            {
                var nsmgr = new XmlNamespaceManager(_doc.NameTable);
                nsmgr.AddNamespace("ds", NS_DS);

                // Buscar X509Certificate
                var certNode = keyInfo.SelectSingleNode("ds:X509Data/ds:X509Certificate", nsmgr);
                
                if (certNode == null || string.IsNullOrWhiteSpace(certNode.InnerText))
                {
                    return null;
                }

                // Decodificar Base64
                var certBytes = Convert.FromBase64String(certNode.InnerText);

                // Crear certificado
                return new X509Certificate2(certBytes);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// T-061: Valida que el KeyInfo tenga la estructura correcta
        /// </summary>
        public bool ValidarEstructura(XmlElement keyInfo)
        {
            try
            {
                var nsmgr = new XmlNamespaceManager(_doc.NameTable);
                nsmgr.AddNamespace("ds", NS_DS);

                // Verificar que existe X509Data
                var x509Data = keyInfo.SelectSingleNode("ds:X509Data", nsmgr);
                if (x509Data == null) return false;

                // Verificar que existe X509Certificate
                var x509Cert = x509Data.SelectSingleNode("ds:X509Certificate", nsmgr);
                if (x509Cert == null || string.IsNullOrWhiteSpace(x509Cert.InnerText))
                    return false;

                // Intentar cargar el certificado
                var cert = ExtraerCertificado(keyInfo);
                return cert != null;
            }
            catch
            {
                return false;
            }
        }
    }
}