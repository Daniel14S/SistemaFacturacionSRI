using System.Security.Cryptography.X509Certificates;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using SistemaFacturacionSRI.Domain.DTOs.Configuracion;
using SistemaFacturacionSRI.Domain.Interfaces.Services;
using SistemaFacturacionSRI.Domain.Entities;
using SistemaFacturacionSRI.Infrastructure.Data;

namespace SistemaFacturacionSRI.Infrastructure.Services
{
    /// <summary>
    /// Gestiona la configuración empresarial para la emisión electrónica.
    /// </summary>
    public class ConfiguracionService : IConfiguracionService
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly ICertificadoDigitalStorageService _certificadoStorage;
        private readonly ICertificadoDigitalService _certificadoService;

        public ConfiguracionService(
            ApplicationDbContext context,
            IConfiguration configuration,
            ICertificadoDigitalStorageService certificadoStorage,
            ICertificadoDigitalService certificadoService)
        {
            _context = context;
            _configuration = configuration;
            _certificadoStorage = certificadoStorage;
            _certificadoService = certificadoService;
        }

        #region Métodos de la Interfaz IConfiguracionService

        public async Task<ConfiguracionEmpresaDto> ObtenerConfiguracionAsync()
        {
            var configuracion = await _context.ConfiguracionEmpresa
                .AsNoTracking()
                .FirstOrDefaultAsync();

            if (configuracion == null)
            {
                // Crear configuración por defecto si no existe
                configuracion = await CrearConfiguracionPorDefectoAsync();
            }

            return await MapearADto(configuracion);
        }

        public async Task<ConfiguracionEmpresaDto> ActualizarConfiguracionAsync(ActualizarConfiguracionDto dto)
        {
            if (dto == null) throw new ArgumentNullException(nameof(dto));

            var configuracion = await _context.ConfiguracionEmpresa.FirstOrDefaultAsync();
            
            if (configuracion == null)
            {
                throw new InvalidOperationException("No existe configuración en el sistema");
            }

            // Actualizar solo los campos permitidos
            configuracion.RazonSocial = dto.RazonSocial.Trim();
            configuracion.NombreComercial = dto.NombreComercial.Trim();
            configuracion.DirMatriz = dto.DirMatriz.Trim();
            configuracion.DirEstablecimiento = dto.DirEstablecimiento.Trim();
            configuracion.ObligadoContabilidad = dto.ObligadoContabilidad;
            configuracion.AgenteRetencion = dto.AgenteRetencion?.Trim();
            configuracion.Telefono = dto.Telefono.Trim();
            configuracion.Email = dto.Email.Trim();
            configuracion.InfoAdicionalDefecto = dto.InfoAdicionalDefecto?.Trim();

            await _context.SaveChangesAsync();

            return await MapearADto(configuracion);
        }

        public async Task<bool> ActualizarCertificadoDigitalAsync(string rutaCertificado, string claveCertificado)
        {
            if (string.IsNullOrWhiteSpace(rutaCertificado))
                throw new ArgumentException("La ruta del certificado es obligatoria", nameof(rutaCertificado));

            if (string.IsNullOrWhiteSpace(claveCertificado))
                throw new ArgumentException("La clave del certificado es obligatoria", nameof(claveCertificado));

            // Cargar el archivo y almacenarlo de forma segura en la BD
            var bytes = await File.ReadAllBytesAsync(rutaCertificado);
            await GuardarCertificadoEnBdAsync(bytes, Path.GetFileName(rutaCertificado), claveCertificado, "PRUEBAS");

            return true;
        }

        public async Task<CertificadoDigitalActivoDto> GuardarCertificadoEnBdAsync(byte[] archivo, string nombreArchivo, string clave, string tipo)
        {
            var resultado = await _certificadoStorage.GuardarCertificadoAsync(new GuardarCertificadoRequest
            {
                ArchivoBytes = archivo,
                NombreArchivo = nombreArchivo,
                Clave = clave,
                Tipo = tipo
            });

            // Limpiar valores antiguos en tabla ConfiguracionEmpresa para evitar rutas en disco
            var configuracion = await _context.ConfiguracionEmpresa.FirstOrDefaultAsync();
            if (configuracion != null)
            {
                configuracion.RutaCertificadoDigital = null;
                configuracion.ClaveCertificadoDigital = null;
                configuracion.FechaModificacion = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }

            // Limpiar caché para que se recargue el nuevo certificado sin reiniciar
            _certificadoService.RefrescarCertificado();

            return resultado;
        }

        public async Task<bool> EliminarCertificadoBdAsync()
        {
            await _certificadoStorage.EliminarCertificadoActivoAsync();

            var configuracion = await _context.ConfiguracionEmpresa.FirstOrDefaultAsync();
            if (configuracion != null)
            {
                configuracion.RutaCertificadoDigital = null;
                configuracion.ClaveCertificadoDigital = null;
                configuracion.FechaModificacion = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }

            // Limpiar caché para que se recargue el estado sin reiniciar
            _certificadoService.RefrescarCertificado();

            return true;
        }

        public async Task<bool> ActualizarLogoAsync(string rutaLogo)
        {
            if (string.IsNullOrWhiteSpace(rutaLogo))
                throw new ArgumentException("La ruta del logo es obligatoria", nameof(rutaLogo));

            var configuracion = await _context.ConfiguracionEmpresa.FirstOrDefaultAsync();
            
            if (configuracion == null)
            {
                throw new InvalidOperationException("No existe configuración en el sistema");
            }

            if (!File.Exists(rutaLogo))
            {
                throw new FileNotFoundException("El archivo de logo no existe", rutaLogo);
            }

            configuracion.LogoPath = rutaLogo;
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<bool> ValidarCertificadoDigitalAsync()
        {
            // Prioridad: certificado almacenado en BD
            var certificadoBd = await _certificadoStorage.ObtenerCertificadoActivoAsync();
            if (certificadoBd != null)
            {
                try
                {
                    using var cert = new X509Certificate2(certificadoBd.ArchivoBytes, certificadoBd.ClavePlano);
                    return DateTime.Now >= cert.NotBefore && DateTime.Now <= cert.NotAfter;
                }
                catch
                {
                    return false;
                }
            }

            // Fallback: configuración en appsettings / ruta física
            var configuracion = await _context.ConfiguracionEmpresa
                .AsNoTracking()
                .FirstOrDefaultAsync();

            var certPath = configuracion?.RutaCertificadoDigital ?? _configuration["FirmaElectronica:RutaCertificado"];
            var certPass = configuracion?.ClaveCertificadoDigital ?? _configuration["FirmaElectronica:ClaveCertificado"];

            if (string.IsNullOrWhiteSpace(certPath) || string.IsNullOrWhiteSpace(certPass))
            {
                return false;
            }

            try
            {
                if (!File.Exists(certPath))
                {
                    return false;
                }

                using var cert = new X509Certificate2(certPath, certPass);

                return DateTime.Now >= cert.NotBefore && DateTime.Now <= cert.NotAfter;
            }
            catch
            {
                return false;
            }
        }

        public async Task<InfoCertificadoDto?> ObtenerInfoCertificadoAsync()
        {
            var certificadoBd = await _certificadoStorage.ObtenerCertificadoActivoAsync();
            if (certificadoBd != null)
            {
                try
                {
                    using var cert = new X509Certificate2(certificadoBd.ArchivoBytes, certificadoBd.ClavePlano);
                    var fechaExpiracion = cert.NotAfter;
                    var diasParaExpirar = (fechaExpiracion - DateTime.Now).Days;

                    return new InfoCertificadoDto
                    {
                        Titular = cert.Subject,
                        RucTitular = ExtraerRucDeCertificado(cert),
                        FechaEmision = cert.NotBefore,
                        FechaExpiracion = fechaExpiracion,
                        EstaVigente = DateTime.Now >= cert.NotBefore && DateTime.Now <= fechaExpiracion,
                        DiasParaExpirar = diasParaExpirar > 0 ? diasParaExpirar : 0,
                        Emisor = cert.Issuer
                    };
                }
                catch
                {
                    return null;
                }
            }

            var configuracion = await _context.ConfiguracionEmpresa
                .AsNoTracking()
                .FirstOrDefaultAsync();

            var certPath = configuracion?.RutaCertificadoDigital ?? _configuration["FirmaElectronica:RutaCertificado"];
            var certPass = configuracion?.ClaveCertificadoDigital ?? _configuration["FirmaElectronica:ClaveCertificado"];

            if (string.IsNullOrWhiteSpace(certPath) || string.IsNullOrWhiteSpace(certPass))
            {
                return null;
            }

            try
            {
                if (!File.Exists(certPath))
                {
                    return null;
                }

                using var cert = new X509Certificate2(certPath, certPass);

                var fechaExpiracion = cert.NotAfter;
                var diasParaExpirar = (fechaExpiracion - DateTime.Now).Days;

                return new InfoCertificadoDto
                {
                    Titular = cert.Subject,
                    RucTitular = ExtraerRucDeCertificado(cert),
                    FechaEmision = cert.NotBefore,
                    FechaExpiracion = fechaExpiracion,
                    EstaVigente = DateTime.Now >= cert.NotBefore && DateTime.Now <= fechaExpiracion,
                    DiasParaExpirar = diasParaExpirar > 0 ? diasParaExpirar : 0,
                    Emisor = cert.Issuer
                };
            }
            catch
            {
                return null;
            }
        }

        public async Task<bool> CambiarAmbienteSRIAsync(string nuevoAmbiente)
        {
            if (string.IsNullOrWhiteSpace(nuevoAmbiente))
                throw new ArgumentException("El ambiente es obligatorio", nameof(nuevoAmbiente));

            if (nuevoAmbiente != "1" && nuevoAmbiente != "2")
            {
                throw new ArgumentException("El ambiente debe ser '1' (Pruebas) o '2' (Producción)", nameof(nuevoAmbiente));
            }

            var configuracion = await _context.ConfiguracionEmpresa.FirstOrDefaultAsync();
            
            if (configuracion == null)
            {
                throw new InvalidOperationException("No existe configuración en el sistema");
            }

            configuracion.AmbienteSRI = nuevoAmbiente;

            // Actualizar URLs según el ambiente
            if (nuevoAmbiente == "1") // Pruebas
            {
                configuracion.UrlRecepcionComprobantes = "https://celcer.sri.gob.ec/comprobantes-electronicos-ws/RecepcionComprobantesOffline?wsdl";
                configuracion.UrlAutorizacionComprobantes = "https://celcer.sri.gob.ec/comprobantes-electronicos-ws/AutorizacionComprobantesOffline?wsdl";
            }
            else // Producción
            {
                configuracion.UrlRecepcionComprobantes = "https://cel.sri.gob.ec/comprobantes-electronicos-ws/RecepcionComprobantesOffline?wsdl";
                configuracion.UrlAutorizacionComprobantes = "https://cel.sri.gob.ec/comprobantes-electronicos-ws/AutorizacionComprobantesOffline?wsdl";
            }

            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<(bool EstaCompleta, List<string> CamposFaltantes)> ValidarConfiguracionCompletaAsync()
        {
            var configuracion = await _context.ConfiguracionEmpresa
                .AsNoTracking()
                .FirstOrDefaultAsync();

            var camposFaltantes = new List<string>();

            if (configuracion == null)
            {
                camposFaltantes.Add("No existe configuración en el sistema");
                return (false, camposFaltantes);
            }

            // Validar campos obligatorios
            if (string.IsNullOrWhiteSpace(configuracion.RUC))
                camposFaltantes.Add("RUC");

            if (string.IsNullOrWhiteSpace(configuracion.RazonSocial))
                camposFaltantes.Add("Razón Social");

            if (string.IsNullOrWhiteSpace(configuracion.DirMatriz))
                camposFaltantes.Add("Dirección Matriz");

            if (string.IsNullOrWhiteSpace(configuracion.CodigoEstablecimiento))
                camposFaltantes.Add("Código Establecimiento");

            if (string.IsNullOrWhiteSpace(configuracion.PuntoEmision))
                camposFaltantes.Add("Punto de Emisión");

            var certificadoBd = await _certificadoStorage.ObtenerCertificadoActivoAsync();
            if (certificadoBd != null)
            {
                var certificadoValido = await ValidarCertificadoDigitalAsync();
                if (!certificadoValido)
                {
                    camposFaltantes.Add("Certificado Digital (BD) no válido o expirado");
                }
            }
            else
            {
                // Verificar si existe configuración en appsettings si no está en BD
                var certPath = configuracion.RutaCertificadoDigital;
                var certPass = configuracion.ClaveCertificadoDigital;

                if (string.IsNullOrWhiteSpace(certPath))
                    certPath = _configuration["FirmaElectronica:RutaCertificado"];
                
                if (string.IsNullOrWhiteSpace(certPass))
                    certPass = _configuration["FirmaElectronica:ClaveCertificado"];

                if (string.IsNullOrWhiteSpace(certPath))
                    camposFaltantes.Add("Certificado Digital");

                if (string.IsNullOrWhiteSpace(certPass))
                    camposFaltantes.Add("Clave del Certificado");

                // Validar que el certificado sea válido
                if (!string.IsNullOrWhiteSpace(certPath))
                {
                    var certificadoValido = await ValidarCertificadoDigitalAsync();
                    if (!certificadoValido)
                    {
                        camposFaltantes.Add("Certificado Digital no válido o expirado");
                    }
                }
            }

            return (camposFaltantes.Count == 0, camposFaltantes);
        }

        #endregion

        #region Métodos CRUD Originales (compatibilidad)

        public async Task<IEnumerable<ConfiguracionEmpresa>> ObtenerTodasAsync(CancellationToken cancellationToken = default)
        {
            return await _context.ConfiguracionEmpresa
                .AsNoTracking()
                .OrderBy(c => c.CodigoEstablecimiento)
                .ThenBy(c => c.PuntoEmision)
                .ToListAsync(cancellationToken);
        }

        public async Task<ConfiguracionEmpresa?> ObtenerPorIdAsync(int id, CancellationToken cancellationToken = default)
        {
            return await _context.ConfiguracionEmpresa
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
        }

        public async Task<ConfiguracionEmpresa?> ObtenerPorEstablecimientoAsync(string establecimiento, string puntoEmision, CancellationToken cancellationToken = default)
        {
            establecimiento = NormalizarCodigo(establecimiento);
            puntoEmision = NormalizarCodigo(puntoEmision);

            return await _context.ConfiguracionEmpresa
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.CodigoEstablecimiento == establecimiento && c.PuntoEmision == puntoEmision, cancellationToken);
        }

        public async Task<ConfiguracionEmpresa> CrearAsync(ConfiguracionEmpresa configuracion, CancellationToken cancellationToken = default)
        {
            if (configuracion == null) throw new ArgumentNullException(nameof(configuracion));

            Normalizar(configuracion);
            await ValidarUnicidadAsync(configuracion, cancellationToken);

            await _context.ConfiguracionEmpresa.AddAsync(configuracion, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);

            return configuracion;
        }

        public async Task<ConfiguracionEmpresa> ActualizarAsync(ConfiguracionEmpresa configuracion, CancellationToken cancellationToken = default)
        {
            if (configuracion == null) throw new ArgumentNullException(nameof(configuracion));

            var existente = await _context.ConfiguracionEmpresa.FirstOrDefaultAsync(c => c.Id == configuracion.Id, cancellationToken);
            if (existente is null)
            {
                throw new KeyNotFoundException($"No existe una configuración con Id {configuracion.Id}");
            }

            Normalizar(configuracion);
            await ValidarUnicidadAsync(configuracion, cancellationToken, configuracion.Id);

            existente.RUC = configuracion.RUC;
            existente.RazonSocial = configuracion.RazonSocial;
            existente.NombreComercial = configuracion.NombreComercial;
            existente.DirMatriz = configuracion.DirMatriz;
            existente.DirEstablecimiento = configuracion.DirEstablecimiento;
            existente.AgenteRetencion = configuracion.AgenteRetencion;
            existente.ObligadoContabilidad = configuracion.ObligadoContabilidad;
            existente.CodigoEstablecimiento = configuracion.CodigoEstablecimiento;
            existente.PuntoEmision = configuracion.PuntoEmision;
            existente.AmbienteSRI = configuracion.AmbienteSRI;
            existente.TipoEmision = configuracion.TipoEmision;
            existente.RutaCertificadoDigital = configuracion.RutaCertificadoDigital;
            existente.ClaveCertificadoDigital = configuracion.ClaveCertificadoDigital;
            existente.LogoPath = configuracion.LogoPath;

            await _context.SaveChangesAsync(cancellationToken);
            return existente;
        }

        public async Task EliminarAsync(int id, CancellationToken cancellationToken = default)
        {
            var configuracion = await _context.ConfiguracionEmpresa.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
            if (configuracion == null)
            {
                return;
            }

            _context.ConfiguracionEmpresa.Remove(configuracion);
            await _context.SaveChangesAsync(cancellationToken);
        }

        #endregion

        #region Métodos Privados

        private async Task<ConfiguracionEmpresa> CrearConfiguracionPorDefectoAsync()
        {
            var configuracion = new ConfiguracionEmpresa
            {
                RUC = "",
                RazonSocial = "CONFIGURAR",
                NombreComercial = "CONFIGURAR",
                DirMatriz = "CONFIGURAR",
                DirEstablecimiento = "CONFIGURAR",
                CodigoEstablecimiento = "001",
                PuntoEmision = "001",
                ObligadoContabilidad = false,
                Telefono = "",
                Email = "",
                AmbienteSRI = "1", // Pruebas por defecto
                TipoEmision = "1",
                UrlRecepcionComprobantes = "https://celcer.sri.gob.ec/comprobantes-electronicos-ws/RecepcionComprobantesOffline?wsdl",
                UrlAutorizacionComprobantes = "https://celcer.sri.gob.ec/comprobantes-electronicos-ws/AutorizacionComprobantesOffline?wsdl"
            };

            await _context.ConfiguracionEmpresa.AddAsync(configuracion);
            await _context.SaveChangesAsync();

            return configuracion;
        }

        private async Task<ConfiguracionEmpresaDto> MapearADto(ConfiguracionEmpresa configuracion)
        {
            var infoCertificado = await ObtenerInfoCertificadoAsync();
            var (estaCompleta, camposFaltantes) = await ValidarConfiguracionCompletaAsync();
            var certificadoBd = await _certificadoStorage.ObtenerCertificadoActivoAsync();
            var tieneCertificado = certificadoBd != null || !string.IsNullOrWhiteSpace(configuracion.RutaCertificadoDigital);
            var origenCertificado = certificadoBd != null ? "ALMACEN_BD" : configuracion.RutaCertificadoDigital;

            return new ConfiguracionEmpresaDto
            {
                Id = configuracion.Id,
                RUC = configuracion.RUC,
                RazonSocial = configuracion.RazonSocial,
                NombreComercial = configuracion.NombreComercial,
                DirMatriz = configuracion.DirMatriz,
                DirEstablecimiento = configuracion.DirEstablecimiento,
                CodigoEstablecimiento = configuracion.CodigoEstablecimiento,
                PuntoEmision = configuracion.PuntoEmision,
                ObligadoContabilidad = configuracion.ObligadoContabilidad,
                AgenteRetencion = configuracion.AgenteRetencion,
                Telefono = configuracion.Telefono,
                Email = configuracion.Email,
                TieneCertificado = tieneCertificado,
                RutaCertificadoDigital = origenCertificado,
                InfoCertificado = infoCertificado,
                AmbienteSRI = configuracion.AmbienteSRI,
                AmbienteSRIDescripcion = configuracion.AmbienteSRI == "1" ? "Pruebas" : "Producción",
                TipoEmision = configuracion.TipoEmision,
                UrlRecepcionComprobantes = configuracion.UrlRecepcionComprobantes,
                UrlAutorizacionComprobantes = configuracion.UrlAutorizacionComprobantes,
                LogoPath = configuracion.LogoPath,
                InfoAdicionalDefecto = configuracion.InfoAdicionalDefecto,
                ConfiguracionCompleta = estaCompleta,
                CamposFaltantes = camposFaltantes
            };
        }

        private static string ExtraerRucDeCertificado(X509Certificate2 certificado)
        {
            // Intentar extraer el RUC del Subject del certificado
            var subject = certificado.Subject;
            var parts = subject.Split(',');
            
            foreach (var part in parts)
            {
                var trimmed = part.Trim();
                if (trimmed.StartsWith("SERIALNUMBER=", StringComparison.OrdinalIgnoreCase))
                {
                    return trimmed.Substring("SERIALNUMBER=".Length).Trim();
                }
            }

            return "N/A";
        }

        private async Task ValidarUnicidadAsync(ConfiguracionEmpresa configuracion, CancellationToken cancellationToken, int? excluirId = null)
        {
            var rucDuplicado = await _context.ConfiguracionEmpresa
                .AnyAsync(c => c.RUC == configuracion.RUC && (!excluirId.HasValue || c.Id != excluirId.Value), cancellationToken);

            if (rucDuplicado)
            {
                throw new InvalidOperationException($"Ya existe una configuración registrada con el RUC {configuracion.RUC}");
            }

            var combinacionDuplicada = await _context.ConfiguracionEmpresa
                .AnyAsync(c => c.CodigoEstablecimiento == configuracion.CodigoEstablecimiento && 
                             c.PuntoEmision == configuracion.PuntoEmision && 
                             (!excluirId.HasValue || c.Id != excluirId.Value), cancellationToken);

            if (combinacionDuplicada)
            {
                throw new InvalidOperationException($"Ya existe una configuración para el establecimiento {configuracion.CodigoEstablecimiento}-{configuracion.PuntoEmision}");
            }
        }

        private static void Normalizar(ConfiguracionEmpresa configuracion)
        {
            configuracion.RUC = configuracion.RUC.Trim();
            configuracion.RazonSocial = configuracion.RazonSocial.Trim();
            configuracion.NombreComercial = configuracion.NombreComercial?.Trim() ?? string.Empty;
            configuracion.DirMatriz = configuracion.DirMatriz.Trim();
            configuracion.DirEstablecimiento = configuracion.DirEstablecimiento.Trim();
            configuracion.AgenteRetencion = configuracion.AgenteRetencion?.Trim();
            configuracion.CodigoEstablecimiento = NormalizarCodigo(configuracion.CodigoEstablecimiento);
            configuracion.PuntoEmision = NormalizarCodigo(configuracion.PuntoEmision);
            
            if (!string.IsNullOrWhiteSpace(configuracion.RutaCertificadoDigital))
            {
                configuracion.RutaCertificadoDigital = configuracion.RutaCertificadoDigital.Trim();
            }
            
            if (!string.IsNullOrWhiteSpace(configuracion.ClaveCertificadoDigital))
            {
                configuracion.ClaveCertificadoDigital = configuracion.ClaveCertificadoDigital.Trim();
            }
        }

        private static string NormalizarCodigo(string? codigo)
        {
            if (string.IsNullOrWhiteSpace(codigo))
            {
                throw new ArgumentException("El código es obligatorio", nameof(codigo));
            }

            return codigo.Trim().PadLeft(3, '0');
        }

        #endregion
    }
}