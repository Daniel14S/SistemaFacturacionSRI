using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Logging;
using SistemaFacturacionSRI.Domain.DTOs.Configuracion;
using SistemaFacturacionSRI.Domain.Entities;
using SistemaFacturacionSRI.Domain.Interfaces.Repositories;
using SistemaFacturacionSRI.Domain.Interfaces.Services;

namespace SistemaFacturacionSRI.Infrastructure.Services
{
    /// <summary>
    /// Almacena de forma segura el certificado digital y su clave dentro de la BD.
    /// </summary>
    public class CertificadoDigitalStorageService : ICertificadoDigitalStorageService
    {
        private readonly ICertificadoDigitalRepository _repository;
        private readonly IDataProtector _protector;
        private readonly ILogger<CertificadoDigitalStorageService> _logger;

        public CertificadoDigitalStorageService(
            ICertificadoDigitalRepository repository,
            IDataProtectionProvider dataProtectionProvider,
            ILogger<CertificadoDigitalStorageService> logger)
        {
            _repository = repository;
            _protector = dataProtectionProvider.CreateProtector("CertificadoDigitalStorage");
            _logger = logger;
        }

        public async Task<CertificadoDigitalActivoDto?> ObtenerCertificadoActivoAsync(CancellationToken cancellationToken = default)
        {
            var entidad = await _repository.ObtenerActivoAsync(cancellationToken);
            if (entidad == null)
            {
                return null;
            }

            var archivoBytes = _protector.Unprotect(entidad.ArchivoEncriptado);
            var claveBytes = _protector.Unprotect(entidad.ClaveEncriptada);
            var clavePlano = Encoding.UTF8.GetString(claveBytes);

            return new CertificadoDigitalActivoDto
            {
                Id = entidad.Id,
                NombreArchivo = entidad.NombreArchivo,
                Tipo = entidad.Tipo,
                TamanoBytes = entidad.TamanoBytes,
                FechaExpiracion = entidad.FechaExpiracion,
                FechaCreacion = entidad.FechaCreacion,
                EsActivo = entidad.EsActivo,
                ArchivoBytes = archivoBytes,
                ClavePlano = clavePlano
            };
        }

        public async Task<CertificadoDigitalActivoDto> GuardarCertificadoAsync(GuardarCertificadoRequest request, CancellationToken cancellationToken = default)
        {
            if (request.ArchivoBytes == null || request.ArchivoBytes.Length == 0)
            {
                throw new ArgumentException("El archivo del certificado está vacío", nameof(request.ArchivoBytes));
            }

            if (string.IsNullOrWhiteSpace(request.Clave))
            {
                throw new ArgumentException("La clave del certificado es obligatoria", nameof(request.Clave));
            }

            // Validar que el certificado se puede cargar con la clave
            DateTime? fechaExpiracion = null;
            try
            {
                using var cert = new X509Certificate2(request.ArchivoBytes, request.Clave);
                fechaExpiracion = cert.NotAfter;
            }
            catch (CryptographicException ex)
            {
                _logger.LogError(ex, "Clave incorrecta o archivo de certificado inválido");
                throw new InvalidOperationException("No se pudo leer el certificado con la clave proporcionada");
            }

            // Desactivar certificados previos
            await _repository.DesactivarTodosAsync(cancellationToken);

            var hash = CalcularSha256(request.ArchivoBytes);
            var entidad = new CertificadoDigital
            {
                NombreArchivo = request.NombreArchivo,
                ArchivoEncriptado = _protector.Protect(request.ArchivoBytes),
                ClaveEncriptada = _protector.Protect(Encoding.UTF8.GetBytes(request.Clave)),
                HashSha256 = hash,
                TamanoBytes = request.ArchivoBytes.LongLength,
                Tipo = string.IsNullOrWhiteSpace(request.Tipo) ? "PRUEBAS" : request.Tipo,
                FechaExpiracion = fechaExpiracion,
                Notas = request.Notas,
                EsActivo = true,
                Activo = true,
                FechaCreacion = DateTime.UtcNow
            };

            await _repository.CrearAsync(entidad, cancellationToken);

            _logger.LogInformation("Certificado digital almacenado en BD. Nombre: {Nombre}, Expira: {Expira}",
                entidad.NombreArchivo, entidad.FechaExpiracion);

            return new CertificadoDigitalActivoDto
            {
                Id = entidad.Id,
                NombreArchivo = entidad.NombreArchivo,
                Tipo = entidad.Tipo,
                TamanoBytes = entidad.TamanoBytes,
                FechaExpiracion = entidad.FechaExpiracion,
                FechaCreacion = entidad.FechaCreacion,
                EsActivo = entidad.EsActivo,
                ArchivoBytes = request.ArchivoBytes,
                ClavePlano = request.Clave
            };
        }

        public async Task<bool> EliminarCertificadoActivoAsync(CancellationToken cancellationToken = default)
        {
            await _repository.EliminarTodosAsync(cancellationToken);
            _logger.LogInformation("Certificados digitales eliminados de la base de datos");
            return true;
        }

        private static string CalcularSha256(byte[] data)
        {
            using var sha = SHA256.Create();
            var hash = sha.ComputeHash(data);
            return Convert.ToHexString(hash);
        }
    }
}
