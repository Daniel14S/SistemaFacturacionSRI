// SistemaFacturacionSRI.Infrastructure/Services/FirmaElectronica/RsaSigner.cs
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Xml;

namespace SistemaFacturacionSRI.Infrastructure.Services.FirmaElectronica
{
    /// <summary>
    /// T-060: Firmador RSA para XADES-BES
    /// </summary>
    public class RsaSigner
    {
        /// <summary>
        /// T-060: Firma el nodo SignedInfo con RSA-SHA1
        /// </summary>
        /// <param name="signedInfo">Nodo SignedInfo a firmar</param>
        /// <param name="certificado">Certificado con clave privada</param>
        /// <returns>Firma en Base64</returns>
        public string FirmarSignedInfo(XmlElement signedInfo, X509Certificate2 certificado)
        {
            if (!certificado.HasPrivateKey)
            {
                throw new InvalidOperationException(
                    "El certificado no tiene clave privada para firmar");
            }

            // Obtener XML canonicalizado del SignedInfo
            var signedInfoXml = CanonicalizarSignedInfo(signedInfo);

            // Convertir a bytes
            var dataToSign = Encoding.UTF8.GetBytes(signedInfoXml);

            // Firmar con RSA-SHA1
            var firma = FirmarConRSA(dataToSign, certificado);

            // Retornar en Base64
            return Convert.ToBase64String(firma);
        }

        /// <summary>
        /// T-060: Realiza la firma RSA con SHA1
        /// </summary>
        private byte[] FirmarConRSA(byte[] data, X509Certificate2 certificado)
        {
            try
            {
                // Obtener la clave privada RSA
                using (var rsa = certificado.GetRSAPrivateKey())
                {
                    if (rsa == null)
                    {
                        throw new InvalidOperationException(
                            "No se pudo obtener la clave privada RSA del certificado");
                    }

                    // Firmar con RSA-SHA1 (requerido por SRI)
                    var firma = rsa.SignData(
                        data,
                        HashAlgorithmName.SHA1,
                        RSASignaturePadding.Pkcs1
                    );

                    return firma;
                }
            }
            catch (CryptographicException ex)
            {
                throw new InvalidOperationException(
                    "Error al firmar con RSA: " + ex.Message, ex);
            }
        }

        /// <summary>
        /// T-060: Canonicaliza el nodo SignedInfo
        /// </summary>
        private string CanonicalizarSignedInfo(XmlElement signedInfo)
        {
            // Obtener el XML del nodo
            var xml = signedInfo.OuterXml;

            // Aplicar canonicalización C14N simplificada
            // Eliminar espacios innecesarios
            xml = System.Text.RegularExpressions.Regex.Replace(xml, @"\s+", " ");
            xml = xml.Replace("> <", "><");

            return xml.Trim();
        }

        /// <summary>
        /// T-060: Verifica una firma RSA
        /// </summary>
        public bool VerificarFirma(
            string signedInfoXml,
            string firmaBase64,
            X509Certificate2 certificado)
        {
            try
            {
                // Decodificar firma desde Base64
                var firma = Convert.FromBase64String(firmaBase64);

                // Convertir SignedInfo a bytes
                var data = Encoding.UTF8.GetBytes(signedInfoXml);

                // Obtener clave pública RSA
                using (var rsa = certificado.GetRSAPublicKey())
                {
                    if (rsa == null)
                    {
                        return false;
                    }

                    // Verificar firma con RSA-SHA1
                    return rsa.VerifyData(
                        data,
                        firma,
                        HashAlgorithmName.SHA1,
                        RSASignaturePadding.Pkcs1
                    );
                }
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// T-060: Obtiene información de la clave RSA
        /// </summary>
        public (int KeySize, string Algorithm) ObtenerInformacionClave(X509Certificate2 certificado)
        {
            using (var rsa = certificado.GetRSAPrivateKey())
            {
                if (rsa == null)
                {
                    return (0, "No disponible");
                }

                return (rsa.KeySize, "RSA");
            }
        }
    }
}