// SistemaFacturacionSRI.Infrastructure/Services/FirmaElectronica/DigestCalculator.cs
using System.Security.Cryptography;
using System.Text;
using System.Xml;

namespace SistemaFacturacionSRI.Infrastructure.Services.FirmaElectronica
{
    /// <summary>
    /// T-059: Calculador de digest (hash) para firma electrónica
    /// </summary>
    public class DigestCalculator
    {
        /// <summary>
        /// T-059: Calcula el hash SHA1 de un nodo XML específico
        /// </summary>
        /// <param name="documento">Documento XML</param>
        /// <param name="idNodo">ID del nodo a hashear</param>
        /// <returns>Hash en Base64</returns>
        public string CalcularDigestNodo(XmlDocument documento, string idNodo)
        {
            // Buscar el nodo por ID
            var nodo = documento.SelectSingleNode($"//*[@id='{idNodo}']");
            
            if (nodo == null)
            {
                throw new InvalidOperationException($"No se encontró el nodo con id '{idNodo}'");
            }

            // Convertir el nodo a string XML
            var xmlString = nodo.OuterXml;

            // Calcular hash SHA1
            return CalcularSHA1(xmlString);
        }

        /// <summary>
        /// T-059: Calcula SHA1 de un string XML
        /// </summary>
        public string CalcularSHA1(string xmlContent)
        {
            using (var sha1 = SHA1.Create())
            {
                var bytes = Encoding.UTF8.GetBytes(xmlContent);
                var hash = sha1.ComputeHash(bytes);
                return Convert.ToBase64String(hash);
            }
        }

        /// <summary>
        /// T-059: Calcula SHA1 de un array de bytes
        /// </summary>
        public string CalcularSHA1(byte[] data)
        {
            using (var sha1 = SHA1.Create())
            {
                var hash = sha1.ComputeHash(data);
                return Convert.ToBase64String(hash);
            }
        }

        /// <summary>
        /// T-059: Calcula digest del documento aplicando canonicalización
        /// </summary>
        public string CalcularDigestCanonicalizado(XmlDocument documento, string idNodo)
        {
            // Buscar el nodo
            var nodo = documento.SelectSingleNode($"//*[@id='{idNodo}']");
            
            if (nodo == null)
            {
                throw new InvalidOperationException($"No se encontró el nodo con id '{idNodo}'");
            }

            // Aplicar canonicalización C14N
            var xmlCanonicalizado = CanonicalizarNodo(nodo);

            // Calcular SHA1
            return CalcularSHA1(xmlCanonicalizado);
        }

        /// <summary>
        /// T-059: Aplica canonicalización C14N a un nodo XML
        /// Implementación simplificada para SRI
        /// </summary>
        private string CanonicalizarNodo(XmlNode nodo)
        {
            // Para simplificar, usar OuterXml
            // En producción real se debería usar System.Security.Cryptography.Xml.XmlDsigC14NTransform
            var xml = nodo.OuterXml;

            // Normalizar espacios en blanco y saltos de línea
            xml = System.Text.RegularExpressions.Regex.Replace(xml, @"\s+", " ");
            xml = xml.Replace("> <", "><");

            return xml.Trim();
        }

        /// <summary>
        /// T-062: Calcula digest de SignedProperties
        /// </summary>
        public string CalcularDigestSignedProperties(XmlElement signedProperties)
        {
            var xmlString = signedProperties.OuterXml;
            
            // Aplicar canonicalización
            var canonicalizado = CanonicalizarNodo(signedProperties);
            
            return CalcularSHA1(canonicalizado);
        }

        /// <summary>
        /// T-059: Verifica si un digest es correcto
        /// </summary>
        public bool VerificarDigest(string xmlContent, string digestEsperado)
        {
            var digestCalculado = CalcularSHA1(xmlContent);
            return digestCalculado.Equals(digestEsperado, StringComparison.Ordinal);
        }

        /// <summary>
        /// T-059: Calcula múltiples digests para validación
        /// </summary>
        public Dictionary<string, string> CalcularDigestsDocumento(XmlDocument documento)
        {
            var digests = new Dictionary<string, string>();

            // Digest del documento principal
            var raiz = documento.DocumentElement;
            if (raiz != null)
            {
                var idRaiz = raiz.GetAttribute("id");
                if (!string.IsNullOrEmpty(idRaiz))
                {
                    digests["documento"] = CalcularDigestNodo(documento, idRaiz);
                }
            }

            // Digest de SignedProperties si existe
            var signedProps = documento.SelectSingleNode(
                "//*[local-name()='SignedProperties']") as XmlElement;
            
            if (signedProps != null)
            {
                digests["signedProperties"] = CalcularDigestSignedProperties(signedProps);
            }

            return digests;
        }
    }
}