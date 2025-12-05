// SistemaFacturacionSRI.Infrastructure/Services/SRI/SriErrorHandler.cs
// T-085: Manejador especializado de errores del SRI

using Microsoft.Extensions.Logging;
using SistemaFacturacionSRI.Domain.DTOs.SRI;

namespace SistemaFacturacionSRI.Infrastructure.Services.SRI
{
    /// <summary>
    /// T-085: Manejador de errores del SRI con traducción y logging detallado
    /// </summary>
    public class SriErrorHandler
    {
        private readonly ILogger<SriErrorHandler> _logger;

        public SriErrorHandler(ILogger<SriErrorHandler> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        // ============================================================
        // T-085: ANÁLISIS Y TRADUCCIÓN DE ERRORES
        // ============================================================

        /// <summary>
        /// T-085: Analiza los mensajes del SRI y proporciona información detallada
        /// </summary>
        public ErrorSriDetallado AnalizarMensajesSri(List<MensajeSri> mensajes, string contexto = "")
        {
            var errorDetallado = new ErrorSriDetallado
            {
                Contexto = contexto,
                FechaAnalisis = DateTime.Now
            };

            foreach (var mensaje in mensajes)
            {
                _logger.LogDebug("Analizando mensaje SRI: [{Tipo}] [{Id}] {Mensaje}",
                    mensaje.Tipo, mensaje.Identificador, mensaje.Mensaje);

                var codigoNumerico = mensaje.IdentificadorNumerico ?? 0;

                // Clasificar mensaje
                if (mensaje.EsError)
                {
                    var errorInfo = ObtenerInformacionError(codigoNumerico, mensaje.Mensaje);
                    errorDetallado.Errores.Add(errorInfo);

                    // Log según severidad
                    if (errorInfo.EsCritico)
                    {
                        _logger.LogError("❌ Error crítico SRI [{Codigo}]: {Descripcion}",
                            codigoNumerico, errorInfo.DescripcionDetallada);
                    }
                    else
                    {
                        _logger.LogWarning("⚠️  Error SRI [{Codigo}]: {Descripcion}",
                            codigoNumerico, errorInfo.DescripcionDetallada);
                    }
                }
                else if (mensaje.EsAdvertencia)
                {
                    var advertencia = new AdvertenciaSri
                    {
                        Codigo = codigoNumerico,
                        Mensaje = mensaje.Mensaje,
                        Descripcion = TraducirMensajeSri(codigoNumerico, mensaje.Mensaje)
                    };
                    errorDetallado.Advertencias.Add(advertencia);

                    _logger.LogInformation("ℹ️  Advertencia SRI [{Codigo}]: {Mensaje}",
                        codigoNumerico, mensaje.Mensaje);
                }
                else if (mensaje.EsInformativo)
                {
                    errorDetallado.Informativos.Add(mensaje.Mensaje);
                    _logger.LogDebug("✅ Info SRI [{Codigo}]: {Mensaje}",
                        codigoNumerico, mensaje.Mensaje);
                }
            }

            // Determinar si hay errores críticos
            errorDetallado.TieneErroresCriticos = errorDetallado.Errores.Any(e => e.EsCritico);
            errorDetallado.TieneErroresRecuperables = errorDetallado.Errores.Any(e => !e.EsCritico);

            // Generar resumen
            errorDetallado.ResumenEjecutivo = GenerarResumenErrores(errorDetallado);

            return errorDetallado;
        }

        // ============================================================
        // T-085: CATÁLOGO DE ERRORES DEL SRI
        // ============================================================

        /// <summary>
        /// T-085: Obtiene información detallada de un código de error específico
        /// </summary>
        private InformacionErrorSri ObtenerInformacionError(int codigo, string mensajeOriginal)
        {
            var errorInfo = new InformacionErrorSri
            {
                Codigo = codigo,
                MensajeOriginal = mensajeOriginal
            };

            switch (codigo)
            {
                // Errores de Firma Digital
                case 69:
                    errorInfo.Categoria = "CERTIFICADO_DIGITAL";
                    errorInfo.DescripcionDetallada = "Certificado digital inválido o no autorizado por el SRI";
                    errorInfo.PosiblesCausas = new List<string>
                    {
                        "Certificado expirado",
                        "Certificado no emitido por entidad autorizada",
                        "Certificado revocado",
                        "Certificado no corresponde al RUC emisor"
                    };
                    errorInfo.SolucionesSugeridas = new List<string>
                    {
                        "Verificar fecha de vigencia del certificado",
                        "Renovar certificado digital",
                        "Verificar que el certificado sea de una CA autorizada por el SRI"
                    };
                    errorInfo.EsCritico = true;
                    errorInfo.EsRecuperable = true;
                    break;

                case 70:
                    errorInfo.Categoria = "FIRMA_ELECTRONICA";
                    errorInfo.DescripcionDetallada = "Firma electrónica XADES-BES no válida";
                    errorInfo.PosiblesCausas = new List<string>
                    {
                        "Estructura XADES-BES incorrecta",
                        "SignedProperties mal formado",
                        "Digest (hash) del documento no coincide",
                        "Firma RSA inválida"
                    };
                    errorInfo.SolucionesSugeridas = new List<string>
                    {
                        "Verificar estructura de firma XADES-BES",
                        "Re-firmar el documento",
                        "Verificar que el XML no se haya modificado después de firmar"
                    };
                    errorInfo.EsCritico = true;
                    errorInfo.EsRecuperable = true;
                    break;

                // Errores de Clave de Acceso
                case 65:
                    errorInfo.Categoria = "CLAVE_ACCESO";
                    errorInfo.DescripcionDetallada = "Clave de acceso incorrecta o mal formada";
                    errorInfo.PosiblesCausas = new List<string>
                    {
                        "Formato de clave incorrecto (debe ser 49 dígitos)",
                        "Dígito verificador incorrecto",
                        "Fecha en la clave no coincide con fecha de emisión"
                    };
                    errorInfo.SolucionesSugeridas = new List<string>
                    {
                        "Regenerar clave de acceso con algoritmo correcto",
                        "Verificar que tenga exactamente 49 dígitos",
                        "Calcular correctamente el dígito verificador (módulo 11)"
                    };
                    errorInfo.EsCritico = true;
                    errorInfo.EsRecuperable = true;
                    break;

                // Errores de Secuencial
                case 235:
                    errorInfo.Categoria = "SECUENCIAL";
                    errorInfo.DescripcionDetallada = "Número secuencial duplicado (ya fue usado)";
                    errorInfo.PosiblesCausas = new List<string>
                    {
                        "Ya existe un comprobante con ese secuencial",
                        "Intento de reenvío de comprobante ya autorizado"
                    };
                    errorInfo.SolucionesSugeridas = new List<string>
                    {
                        "Generar nuevo número secuencial",
                        "Verificar secuencial en base de datos antes de enviar",
                        "NO reutilizar secuenciales de comprobantes autorizados"
                    };
                    errorInfo.EsCritico = true;
                    errorInfo.EsRecuperable = false; // Requiere generar nuevo comprobante
                    break;

                // Errores de Estructura XML
                case 43:
                    errorInfo.Categoria = "INFORMATIVO";
                    errorInfo.DescripcionDetallada = "Clave de acceso registrada correctamente";
                    errorInfo.EsCritico = false;
                    errorInfo.EsRecuperable = true;
                    break;

                case 60:
                    errorInfo.Categoria = "INFORMATIVO";
                    errorInfo.DescripcionDetallada = "Comprobante autorizado correctamente";
                    errorInfo.EsCritico = false;
                    errorInfo.EsRecuperable = true;
                    break;

                // Errores del Sistema
                case 999:
                    errorInfo.Categoria = "SISTEMA_SRI";
                    errorInfo.DescripcionDetallada = "Error interno del sistema del SRI";
                    errorInfo.PosiblesCausas = new List<string>
                    {
                        "Servidor del SRI con problemas",
                        "Mantenimiento programado",
                        "Sobrecarga del sistema"
                    };
                    errorInfo.SolucionesSugeridas = new List<string>
                    {
                        "Reintentar después de unos minutos",
                        "Verificar estado de servicios del SRI",
                        "Contactar soporte del SRI si persiste"
                    };
                    errorInfo.EsCritico = false;
                    errorInfo.EsRecuperable = true;
                    errorInfo.DebeReintentar = true;
                    break;

                // Error desconocido
                default:
                    errorInfo.Categoria = "DESCONOCIDO";
                    errorInfo.DescripcionDetallada = $"Error no catalogado: {mensajeOriginal}";
                    errorInfo.SolucionesSugeridas = new List<string>
                    {
                        "Revisar documentación técnica del SRI",
                        "Contactar soporte técnico del SRI",
                        "Revisar logs detallados del sistema"
                    };
                    errorInfo.EsCritico = true;
                    errorInfo.EsRecuperable = false;
                    break;
            }

            return errorInfo;
        }

        // ============================================================
        // T-085: TRADUCCIÓN DE MENSAJES
        // ============================================================

        /// <summary>
        /// T-085: Traduce mensajes técnicos del SRI a lenguaje claro
        /// </summary>
        private string TraducirMensajeSri(int codigo, string mensajeOriginal)
        {
            return codigo switch
            {
                43 => "✅ El comprobante fue recibido correctamente por el SRI",
                60 => "🎉 El comprobante fue AUTORIZADO por el SRI",
                65 => "❌ La clave de acceso tiene errores. Debe regenerarse",
                68 => "⚠️ Su certificado digital está próximo a expirar. Renuévelo pronto",
                69 => "❌ El certificado digital no es válido. Verifique su vigencia",
                70 => "❌ La firma electrónica no es válida. Re-firme el documento",
                235 => "❌ Este número secuencial ya fue usado. Debe generar uno nuevo",
                999 => "⚠️ Error temporal del SRI. Reintente en unos minutos",
                _ => mensajeOriginal
            };
        }

        // ============================================================
        // T-085: GENERACIÓN DE REPORTES
        // ============================================================

        /// <summary>
        /// T-085: Genera un resumen ejecutivo de los errores
        /// </summary>
        private string GenerarResumenErrores(ErrorSriDetallado errorDetallado)
        {
            var sb = new System.Text.StringBuilder();

            if (errorDetallado.Errores.Any())
            {
                sb.AppendLine($"❌ Se encontraron {errorDetallado.Errores.Count} error(es):");
                foreach (var error in errorDetallado.Errores)
                {
                    sb.AppendLine($"  • [{error.Codigo}] {error.DescripcionDetallada}");
                }
            }

            if (errorDetallado.Advertencias.Any())
            {
                sb.AppendLine($"\n⚠️  Advertencias ({errorDetallado.Advertencias.Count}):");
                foreach (var adv in errorDetallado.Advertencias)
                {
                    sb.AppendLine($"  • {adv.Descripcion}");
                }
            }

            if (errorDetallado.Informativos.Any())
            {
                sb.AppendLine($"\nℹ️  Información:");
                foreach (var info in errorDetallado.Informativos)
                {
                    sb.AppendLine($"  • {info}");
                }
            }

            return sb.ToString();
        }

        /// <summary>
        /// T-085: Genera un reporte detallado en texto
        /// </summary>
        public string GenerarReporteDetallado(ErrorSriDetallado errorDetallado)
        {
            var sb = new System.Text.StringBuilder();

            sb.AppendLine("════════════════════════════════════════════════════════════");
            sb.AppendLine("   REPORTE DETALLADO DE ERRORES DEL SRI");
            sb.AppendLine("════════════════════════════════════════════════════════════");
            sb.AppendLine($"Contexto: {errorDetallado.Contexto}");
            sb.AppendLine($"Fecha: {errorDetallado.FechaAnalisis:dd/MM/yyyy HH:mm:ss}");
            sb.AppendLine();

            // Errores
            if (errorDetallado.Errores.Any())
            {
                sb.AppendLine("❌ ERRORES ENCONTRADOS:");
                sb.AppendLine("────────────────────────────────────────────────────────────");
                
                foreach (var error in errorDetallado.Errores)
                {
                    sb.AppendLine($"\n[Código {error.Codigo}] - {error.Categoria}");
                    sb.AppendLine($"Descripción: {error.DescripcionDetallada}");
                    sb.AppendLine($"Crítico: {(error.EsCritico ? "SÍ" : "NO")}");
                    sb.AppendLine($"Recuperable: {(error.EsRecuperable ? "SÍ" : "NO")}");

                    if (error.PosiblesCausas.Any())
                    {
                        sb.AppendLine("\nPosibles causas:");
                        foreach (var causa in error.PosiblesCausas)
                        {
                            sb.AppendLine($"  • {causa}");
                        }
                    }

                    if (error.SolucionesSugeridas.Any())
                    {
                        sb.AppendLine("\nSoluciones sugeridas:");
                        foreach (var solucion in error.SolucionesSugeridas)
                        {
                            sb.AppendLine($"  ✓ {solucion}");
                        }
                    }
                }
            }

            // Advertencias
            if (errorDetallado.Advertencias.Any())
            {
                sb.AppendLine("\n⚠️  ADVERTENCIAS:");
                sb.AppendLine("────────────────────────────────────────────────────────────");
                foreach (var adv in errorDetallado.Advertencias)
                {
                    sb.AppendLine($"[{adv.Codigo}] {adv.Descripcion}");
                }
            }

            // Informativos
            if (errorDetallado.Informativos.Any())
            {
                sb.AppendLine("\nℹ️  MENSAJES INFORMATIVOS:");
                sb.AppendLine("────────────────────────────────────────────────────────────");
                foreach (var info in errorDetallado.Informativos)
                {
                    sb.AppendLine($"  • {info}");
                }
            }

            sb.AppendLine("\n════════════════════════════════════════════════════════════");

            return sb.ToString();
        }

        /// <summary>
        /// T-085: Logging detallado de un error SRI
        /// </summary>
        public void LogErrorDetallado(ErrorSriDetallado errorDetallado)
        {
            if (!errorDetallado.Errores.Any() && !errorDetallado.Advertencias.Any())
            {
                return;
            }

            _logger.LogWarning("════════════════════════════════════════════════════════════");
            _logger.LogWarning("ERRORES DEL SRI - {Contexto}", errorDetallado.Contexto);
            _logger.LogWarning("════════════════════════════════════════════════════════════");

            foreach (var error in errorDetallado.Errores)
            {
                if (error.EsCritico)
                {
                    _logger.LogError("❌ [{Codigo}] {Categoria}: {Descripcion}",
                        error.Codigo, error.Categoria, error.DescripcionDetallada);
                }
                else
                {
                    _logger.LogWarning("⚠️  [{Codigo}] {Categoria}: {Descripcion}",
                        error.Codigo, error.Categoria, error.DescripcionDetallada);
                }

                if (error.SolucionesSugeridas.Any())
                {
                    _logger.LogInformation("  Soluciones: {Soluciones}",
                        string.Join("; ", error.SolucionesSugeridas));
                }
            }

            _logger.LogWarning("════════════════════════════════════════════════════════════");
        }
    }

    // ============================================================
    // T-085: CLASES DE SOPORTE
    // ============================================================

    /// <summary>
    /// Error del SRI analizado y enriquecido con información
    /// </summary>
    public class ErrorSriDetallado
    {
        public string Contexto { get; set; } = string.Empty;
        public DateTime FechaAnalisis { get; set; }
        public List<InformacionErrorSri> Errores { get; set; } = new List<InformacionErrorSri>();
        public List<AdvertenciaSri> Advertencias { get; set; } = new List<AdvertenciaSri>();
        public List<string> Informativos { get; set; } = new List<string>();
        public bool TieneErroresCriticos { get; set; }
        public bool TieneErroresRecuperables { get; set; }
        public string ResumenEjecutivo { get; set; } = string.Empty;
    }

    /// <summary>
    /// Información detallada de un error específico
    /// </summary>
    public class InformacionErrorSri
    {
        public int Codigo { get; set; }
        public string MensajeOriginal { get; set; } = string.Empty;
        public string Categoria { get; set; } = string.Empty;
        public string DescripcionDetallada { get; set; } = string.Empty;
        public List<string> PosiblesCausas { get; set; } = new List<string>();
        public List<string> SolucionesSugeridas { get; set; } = new List<string>();
        public bool EsCritico { get; set; }
        public bool EsRecuperable { get; set; }
        public bool DebeReintentar { get; set; }
    }

    /// <summary>
    /// Advertencia del SRI
    /// </summary>
    public class AdvertenciaSri
    {
        public int Codigo { get; set; }
        public string Mensaje { get; set; } = string.Empty;
        public string Descripcion { get; set; } = string.Empty;
    }
}