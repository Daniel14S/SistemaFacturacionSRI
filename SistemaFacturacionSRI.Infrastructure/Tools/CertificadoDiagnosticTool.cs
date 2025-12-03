using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace SistemaFacturacionSRI.Infrastructure.Tools
{
    /// <summary>
    /// Herramienta para diagnosticar certificados digitales
    /// Útil para debugging y verificación
    /// </summary>
    public static class CertificadoDiagnosticTool
    {
        /// <summary>
        /// Genera un reporte completo del certificado
        /// </summary>
        public static string GenerarReporteCertificado(X509Certificate2 certificado)
        {
            var sb = new StringBuilder();
            sb.AppendLine("═══════════════════════════════════════════════════════");
            sb.AppendLine("           REPORTE DE CERTIFICADO DIGITAL");
            sb.AppendLine("═══════════════════════════════════════════════════════");
            sb.AppendLine();

            // Información básica
            sb.AppendLine("📋 INFORMACIÓN BÁSICA:");
            sb.AppendLine($"   Subject: {certificado.Subject}");
            sb.AppendLine($"   Issuer: {certificado.Issuer}");
            sb.AppendLine($"   Serial Number: {certificado.SerialNumber}");
            sb.AppendLine($"   Thumbprint: {certificado.Thumbprint}");
            sb.AppendLine();

            // Vigencia
            sb.AppendLine("📅 VIGENCIA:");
            sb.AppendLine($"   Válido desde: {certificado.NotBefore:dd/MM/yyyy HH:mm:ss}");
            sb.AppendLine($"   Válido hasta: {certificado.NotAfter:dd/MM/yyyy HH:mm:ss}");
            
            var diasRestantes = (certificado.NotAfter - DateTime.Now).Days;
            var estado = diasRestantes > 0 ? "✅ VIGENTE" : "❌ EXPIRADO";
            sb.AppendLine($"   Estado: {estado}");
            sb.AppendLine($"   Días restantes: {diasRestantes}");
            sb.AppendLine();

            // Algoritmo
            sb.AppendLine("🔐 ALGORITMO:");
            sb.AppendLine($"   Signature Algorithm: {certificado.SignatureAlgorithm.FriendlyName}");
            sb.AppendLine($"   Version: {certificado.Version}");
            sb.AppendLine();

            // Clave privada (CORREGIDO - usar GetRSAPrivateKey)
            sb.AppendLine("🔑 CLAVE PRIVADA:");
            sb.AppendLine($"   Tiene clave privada: {(certificado.HasPrivateKey ? "✅ SÍ" : "❌ NO")}");
            if (certificado.HasPrivateKey)
            {
                try
                {
                    // Usar GetRSAPrivateKey() en lugar de PrivateKey
                    using var rsa = certificado.GetRSAPrivateKey();
                    if (rsa != null)
                    {
                        sb.AppendLine($"   Algoritmo: RSA");
                        sb.AppendLine($"   Key Size: {rsa.KeySize} bits");
                    }
                }
                catch
                {
                    sb.AppendLine($"   Algoritmo: No disponible o no es RSA");
                }
            }
            sb.AppendLine();

            // Key Usages
            sb.AppendLine("🎯 USOS DE CLAVE (Key Usage):");
            foreach (var extension in certificado.Extensions)
            {
                if (extension is X509KeyUsageExtension keyUsageExt)
                {
                    var usages = new List<string>();
                    if (keyUsageExt.KeyUsages.HasFlag(X509KeyUsageFlags.DigitalSignature))
                        usages.Add("Digital Signature ✅");
                    if (keyUsageExt.KeyUsages.HasFlag(X509KeyUsageFlags.NonRepudiation))
                        usages.Add("Non Repudiation ✅");
                    if (keyUsageExt.KeyUsages.HasFlag(X509KeyUsageFlags.KeyEncipherment))
                        usages.Add("Key Encipherment");
                    if (keyUsageExt.KeyUsages.HasFlag(X509KeyUsageFlags.DataEncipherment))
                        usages.Add("Data Encipherment");
                    if (keyUsageExt.KeyUsages.HasFlag(X509KeyUsageFlags.KeyAgreement))
                        usages.Add("Key Agreement");
                    if (keyUsageExt.KeyUsages.HasFlag(X509KeyUsageFlags.KeyCertSign))
                        usages.Add("Key Cert Sign");
                    if (keyUsageExt.KeyUsages.HasFlag(X509KeyUsageFlags.CrlSign))
                        usages.Add("CRL Sign");

                    foreach (var usage in usages)
                    {
                        sb.AppendLine($"   • {usage}");
                    }
                }
            }
            sb.AppendLine();

            // Extended Key Usage
            sb.AppendLine("🎯 USOS EXTENDIDOS (Extended Key Usage):");
            foreach (var extension in certificado.Extensions)
            {
                if (extension is X509EnhancedKeyUsageExtension ekuExt)
                {
                    foreach (var oid in ekuExt.EnhancedKeyUsages)
                    {
                        sb.AppendLine($"   • {oid.FriendlyName ?? oid.Value}");
                    }
                }
            }
            sb.AppendLine();

            // Subject Alternative Names (CORREGIDO - usar AsnEncodedData correctamente)
            sb.AppendLine("📧 NOMBRES ALTERNATIVOS (SAN):");
            foreach (var extension in certificado.Extensions)
            {
                if (extension.Oid?.Value == "2.5.29.17") // Subject Alternative Name
                {
                    var asnData = new System.Security.Cryptography.AsnEncodedData(
                        extension.Oid, 
                        extension.RawData
                    );
                    sb.AppendLine($"   {asnData.Format(false)}");
                }
            }
            sb.AppendLine();

            // Todas las extensiones
            sb.AppendLine("🔧 EXTENSIONES:");
            foreach (var extension in certificado.Extensions)
            {
                sb.AppendLine($"   • {extension.Oid?.FriendlyName ?? extension.Oid?.Value ?? "Unknown"}");
                sb.AppendLine($"     Critical: {extension.Critical}");
            }

            sb.AppendLine();
            sb.AppendLine("═══════════════════════════════════════════════════════");

            return sb.ToString();
        }

        /// <summary>
        /// Valida si el certificado es apto para firma SRI
        /// </summary>
        public static (bool EsValido, List<string> Errores, List<string> Advertencias) 
            ValidarCertificadoParaSRI(X509Certificate2 certificado)
        {
            var errores = new List<string>();
            var advertencias = new List<string>();

            // 1. Verificar clave privada
            if (!certificado.HasPrivateKey)
            {
                errores.Add("❌ El certificado NO tiene clave privada. Requerido para firmar.");
            }

            // 2. Verificar vigencia
            if (DateTime.Now < certificado.NotBefore)
            {
                errores.Add($"❌ El certificado aún no es válido. Válido desde: {certificado.NotBefore:dd/MM/yyyy}");
            }

            if (DateTime.Now > certificado.NotAfter)
            {
                errores.Add($"❌ El certificado está EXPIRADO. Expiró el: {certificado.NotAfter:dd/MM/yyyy}");
            }

            var diasRestantes = (certificado.NotAfter - DateTime.Now).Days;
            if (diasRestantes <= 30 && diasRestantes > 0)
            {
                advertencias.Add($"⚠️ El certificado expira en {diasRestantes} días. Considere renovarlo.");
            }

            // 3. Verificar algoritmo de firma (debe ser RSA de al menos 2048 bits)
            try
            {
                // CORREGIDO: Usar GetRSAPrivateKey en lugar de PrivateKey
                using var rsa = certificado.GetRSAPrivateKey();
                if (rsa != null)
                {
                    if (rsa.KeySize < 2048)
                    {
                        advertencias.Add($"⚠️ El tamaño de clave es {rsa.KeySize} bits. Se recomienda 2048 bits o más.");
                    }
                }
                else
                {
                    advertencias.Add("⚠️ No se pudo determinar el tamaño de la clave.");
                }
            }
            catch
            {
                advertencias.Add("⚠️ No se pudo verificar el algoritmo de clave.");
            }

            // 4. Verificar Key Usage para firma digital
            bool tieneDigitalSignature = false;
            foreach (var extension in certificado.Extensions)
            {
                if (extension is X509KeyUsageExtension keyUsageExt)
                {
                    if (keyUsageExt.KeyUsages.HasFlag(X509KeyUsageFlags.DigitalSignature))
                    {
                        tieneDigitalSignature = true;
                        break;
                    }
                }
            }

            if (!tieneDigitalSignature)
            {
                advertencias.Add("⚠️ El certificado no tiene explícitamente el uso 'Digital Signature'. Puede funcionar, pero no es lo ideal.");
            }

            var esValido = errores.Count == 0;
            return (esValido, errores, advertencias);
        }

        /// <summary>
        /// Imprime el reporte en consola
        /// </summary>
        public static void ImprimirReporte(X509Certificate2 certificado)
        {
            Console.WriteLine(GenerarReporteCertificado(certificado));
        }
    }
}