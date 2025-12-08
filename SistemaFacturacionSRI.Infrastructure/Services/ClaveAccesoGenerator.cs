// ============================================================
// ClaveAccesoGenerator.cs - VERSIÓN CORREGIDA
// ✅ Genera claves de acceso de 49 dígitos según estándar SRI
// ============================================================

using System.Globalization;
using Microsoft.Extensions.Logging;

namespace SistemaFacturacionSRI.Infrastructure.Services;

/// <summary>
/// Generador de Clave de Acceso para comprobantes electrónicos del SRI
/// 
/// La clave de acceso tiene 49 dígitos y sigue este formato:
/// DDMMAAAATCRRRRRRRRRRRRRATEEEpppssssssssscccccccccdv
/// 
/// Posiciones:
/// 0-7   (8):  DD/MM/AAAA - Fecha de emisión
/// 8-9   (2):  TC - Tipo de Comprobante (01=Factura)
/// 10-22 (13): RUC - RUC del emisor
/// 23    (1):  A - Ambiente (1=Pruebas, 2=Producción)
/// 24    (1):  TE - Tipo de Emisión (1=Normal, 2=Contingencia)
/// 25-27 (3):  EEE - Establecimiento (001)
/// 28-30 (3):  PPP - Punto de Emisión (001)
/// 31-39 (9):  SSSSSSSSS - Secuencial (000000001)
/// 40-47 (8):  CCCCCCCC - Código Numérico aleatorio
/// 48    (1):  DV - Dígito Verificador (módulo 11)
/// 
/// Ejemplo: 0812202501180418379400111001001000000037123456785
/// </summary>
public class ClaveAccesoGenerator
{
    private readonly ILogger<ClaveAccesoGenerator>? _logger;

    public ClaveAccesoGenerator(ILogger<ClaveAccesoGenerator>? logger = null)
    {
        _logger = logger;
    }

    /// <summary>
    /// ✅ MÉTODO PRINCIPAL: Genera una clave de acceso completa de 49 dígitos
    /// </summary>
    public string GenerarClaveAcceso(
        DateTime fechaEmision,
        string tipoComprobante,
        string ruc,
        string ambiente,
        string tipoEmision,
        string establecimiento,
        string puntoEmision,
        string secuencial,
        string? codigoNumerico = null)
    {
        _logger?.LogInformation("\n╔════════════════════════════════════════════════════════════╗");
        _logger?.LogInformation("║  🔑 GENERANDO CLAVE DE ACCESO                             ║");
        _logger?.LogInformation("╚════════════════════════════════════════════════════════════╝");

        // ✅ NORMALIZAR PARÁMETROS (Asegurar formato correcto)
        establecimiento = establecimiento.Trim().PadLeft(3, '0');
        puntoEmision = puntoEmision.Trim().PadLeft(3, '0');
        secuencial = secuencial.Trim().PadLeft(9, '0');
        tipoComprobante = tipoComprobante.Trim().PadLeft(2, '0');
        ruc = ruc.Trim().PadLeft(13, '0');
        ambiente = ambiente.Trim();
        tipoEmision = tipoEmision.Trim();

        // ✅ VALIDAR PARÁMETROS
        ValidarParametros(tipoComprobante, ruc, ambiente, tipoEmision, establecimiento, puntoEmision, secuencial);

        // ✅ GENERAR CÓDIGO NUMÉRICO SI NO SE PROPORCIONA
        if (string.IsNullOrEmpty(codigoNumerico))
        {
            codigoNumerico = GenerarCodigoNumerico();
        }
        else
        {
            codigoNumerico = codigoNumerico.Trim().PadLeft(8, '0');
        }

        // ✅ CONSTRUIR LA CLAVE BASE (48 dígitos) - ORDEN CORRECTO
        string claveBase =
            $"{fechaEmision:ddMMyyyy}" +      // [0-7]   (8 dígitos): DDMMAAAA
            $"{tipoComprobante}" +             // [8-9]   (2 dígitos): TC
            $"{ruc}" +                         // [10-22] (13 dígitos): RUC
            $"{ambiente}" +                    // [23]    (1 dígito): A
            $"{tipoEmision}" +                 // [24]    (1 dígito): TE
            $"{establecimiento}" +             // [25-27] (3 dígitos): EEE
            $"{puntoEmision}" +                // [28-30] (3 dígitos): PPP
            $"{secuencial}" +                  // [31-39] (9 dígitos): SSSSSSSSS
            $"{codigoNumerico}";               // [40-47] (8 dígitos): CCCCCCCC

        // ✅ VALIDAR LONGITUD
        if (claveBase.Length != 48)
        {
            var error = $"❌ ERROR: Clave base debe tener 48 dígitos, tiene {claveBase.Length}";
            _logger?.LogError(error);
            throw new InvalidOperationException(error);
        }

        // ✅ CALCULAR DÍGITO VERIFICADOR
        int digitoVerificador = CalcularModulo11(claveBase);

        // ✅ CLAVE COMPLETA
        string claveCompleta = claveBase + digitoVerificador;

        // ✅ LOGGING DETALLADO
        _logger?.LogInformation("  📋 COMPONENTES DE LA CLAVE:");
        _logger?.LogInformation("  [1] Fecha:          {Fecha} (posiciones 0-7)", fechaEmision.ToString("ddMMyyyy"));
        _logger?.LogInformation("  [2] Tipo Comp:      {Tipo} (posiciones 8-9)", tipoComprobante);
        _logger?.LogInformation("  [3] RUC:            {Ruc} (posiciones 10-22)", ruc);
        _logger?.LogInformation("  [4] Ambiente:       {Ambiente} (posición 23)", ambiente);
        _logger?.LogInformation("  [5] Tipo Emisión:   {TipoEmision} (posición 24)", tipoEmision);
        _logger?.LogInformation("  [6] Establecim:     {Estab} ✅ (posiciones 25-27)", establecimiento);
        _logger?.LogInformation("  [7] Punto Emis:     {Punto} ✅ (posiciones 28-30)", puntoEmision);
        _logger?.LogInformation("  [8] Secuencial:     {Sec} ✅ (posiciones 31-39)", secuencial);
        _logger?.LogInformation("  [9] Cód Numérico:   {Cod} (posiciones 40-47)", codigoNumerico);
        _logger?.LogInformation("  [10] Dígito Verif:  {Dv} (posición 48)", digitoVerificador);
        _logger?.LogInformation("");
        _logger?.LogInformation("  ✅ CLAVE COMPLETA: {Clave}", claveCompleta);
        _logger?.LogInformation("  ✅ LONGITUD: {Length} dígitos", claveCompleta.Length);
        _logger?.LogInformation("╚════════════════════════════════════════════════════════════╝\n");

        return claveCompleta;
    }

    /// <summary>
    /// ✅ Calcula el dígito verificador usando el algoritmo de módulo 11
    /// </summary>
    private int CalcularModulo11(string claveBase)
    {
        // Multiplicadores que se repiten cíclicamente: 2, 3, 4, 5, 6, 7
        int[] multiplicadores = { 2, 3, 4, 5, 6, 7 };
        int suma = 0;
        int multiplicadorIndex = 0;

        // ✅ Recorrer la clave de DERECHA a IZQUIERDA
        for (int i = claveBase.Length - 1; i >= 0; i--)
        {
            if (!char.IsDigit(claveBase[i]))
            {
                throw new InvalidOperationException(
                    $"La clave contiene caracteres no numéricos en posición {i}: '{claveBase[i]}'");
            }

            int digito = int.Parse(claveBase[i].ToString());

            // Multiplicar por el factor correspondiente
            suma += digito * multiplicadores[multiplicadorIndex];

            // Avanzar al siguiente multiplicador (cíclicamente)
            multiplicadorIndex = (multiplicadorIndex + 1) % multiplicadores.Length;
        }

        // Calcular módulo 11
        int residuo = suma % 11;
        int resultado = 11 - residuo;

        // ✅ Reglas especiales del SRI
        if (resultado == 11) return 0;  // Si da 11, el dígito es 0
        if (resultado == 10) return 1;  // Si da 10, el dígito es 1

        return resultado;
    }

    /// <summary>
    /// ✅ Genera un código numérico aleatorio de 8 dígitos
    /// </summary>
    public string GenerarCodigoNumerico()
    {
        Random random = new Random();
        int numero = random.Next(10000000, 99999999);
        return numero.ToString();
    }

    /// <summary>
    /// ✅ Valida que todos los parámetros tengan el formato correcto
    /// </summary>
    private void ValidarParametros(
        string tipoComprobante,
        string ruc,
        string ambiente,
        string tipoEmision,
        string establecimiento,
        string puntoEmision,
        string secuencial)
    {
        // Validar tipo de comprobante (2 dígitos)
        if (string.IsNullOrWhiteSpace(tipoComprobante) || tipoComprobante.Length != 2 || !EsSoloNumeros(tipoComprobante))
        {
            throw new ArgumentException(
                $"El tipo de comprobante debe tener exactamente 2 dígitos. Valor recibido: '{tipoComprobante}'");
        }

        // Validar RUC (13 dígitos)
        if (string.IsNullOrWhiteSpace(ruc) || ruc.Length != 13 || !EsSoloNumeros(ruc))
        {
            throw new ArgumentException(
                $"El RUC debe tener exactamente 13 dígitos. Valor recibido: '{ruc}'");
        }

        // Validar ambiente (1 dígito: 1 o 2)
        if (string.IsNullOrWhiteSpace(ambiente) || ambiente.Length != 1 || !EsSoloNumeros(ambiente))
        {
            throw new ArgumentException(
                $"El ambiente debe ser 1 (Pruebas) o 2 (Producción). Valor recibido: '{ambiente}'");
        }

        if (ambiente != "1" && ambiente != "2")
        {
            throw new ArgumentException(
                $"El ambiente debe ser 1 (Pruebas) o 2 (Producción). Valor recibido: '{ambiente}'");
        }

        // Validar tipo de emisión (1 dígito: 1 o 2)
        if (string.IsNullOrWhiteSpace(tipoEmision) || tipoEmision.Length != 1 || !EsSoloNumeros(tipoEmision))
        {
            throw new ArgumentException(
                $"El tipo de emisión debe ser 1 (Normal) o 2 (Contingencia). Valor recibido: '{tipoEmision}'");
        }

        if (tipoEmision != "1" && tipoEmision != "2")
        {
            throw new ArgumentException(
                $"El tipo de emisión debe ser 1 (Normal) o 2 (Contingencia). Valor recibido: '{tipoEmision}'");
        }

        // Validar establecimiento (3 dígitos)
        if (string.IsNullOrWhiteSpace(establecimiento) || establecimiento.Length != 3 || !EsSoloNumeros(establecimiento))
        {
            throw new ArgumentException(
                $"El establecimiento debe tener exactamente 3 dígitos. Valor recibido: '{establecimiento}'");
        }

        // Validar punto de emisión (3 dígitos)
        if (string.IsNullOrWhiteSpace(puntoEmision) || puntoEmision.Length != 3 || !EsSoloNumeros(puntoEmision))
        {
            throw new ArgumentException(
                $"El punto de emisión debe tener exactamente 3 dígitos. Valor recibido: '{puntoEmision}'");
        }

        // Validar secuencial (9 dígitos)
        if (string.IsNullOrWhiteSpace(secuencial) || secuencial.Length != 9 || !EsSoloNumeros(secuencial))
        {
            throw new ArgumentException(
                $"El secuencial debe tener exactamente 9 dígitos. Valor recibido: '{secuencial}'");
        }
    }

    /// <summary>
    /// ✅ Verifica si una cadena contiene solo números
    /// </summary>
    private bool EsSoloNumeros(string texto)
    {
        return texto.All(char.IsDigit);
    }

    /// <summary>
    /// ✅ Valida si una clave de acceso es válida
    /// </summary>
    public bool ValidarClaveAcceso(string claveAcceso)
    {
        if (string.IsNullOrWhiteSpace(claveAcceso) || claveAcceso.Length != 49)
        {
            return false;
        }

        if (!EsSoloNumeros(claveAcceso))
        {
            return false;
        }

        // Validar que los primeros 8 dígitos representen una fecha válida (DDMMAAAA)
        var fechaSegmento = claveAcceso.Substring(0, 8);
        if (!DateTime.TryParseExact(
                fechaSegmento,
                "ddMMyyyy",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out _))
        {
            return false;
        }

        // Extraer la clave base (primeros 48 dígitos)
        string claveBase = claveAcceso.Substring(0, 48);

        // Extraer el dígito verificador (último dígito)
        int digitoVerificadorRecibido = int.Parse(claveAcceso[48].ToString());

        // Calcular el dígito verificador esperado
        int digitoVerificadorCalculado = CalcularModulo11(claveBase);

        // Comparar
        return digitoVerificadorRecibido == digitoVerificadorCalculado;
    }

    /// <summary>
    /// 🔍 Decodifica una clave de acceso y muestra sus componentes
    /// </summary>
    public ComponentesClave DecodificarClaveAcceso(string claveAcceso)
    {
        _logger?.LogInformation("\n╔════════════════════════════════════════════════════════════╗");
        _logger?.LogInformation("║  🔍 DECODIFICANDO CLAVE DE ACCESO                         ║");
        _logger?.LogInformation("╚════════════════════════════════════════════════════════════╝");

        if (string.IsNullOrWhiteSpace(claveAcceso) || claveAcceso.Length != 49)
        {
            throw new ArgumentException($"La clave debe tener 49 dígitos. Tiene: {claveAcceso?.Length ?? 0}");
        }

        var componentes = new ComponentesClave
        {
            Fecha = claveAcceso.Substring(0, 8),
            TipoComprobante = claveAcceso.Substring(8, 2),
            Ruc = claveAcceso.Substring(10, 13),
            Ambiente = claveAcceso.Substring(23, 1),
            TipoEmision = claveAcceso.Substring(24, 1),
            Establecimiento = claveAcceso.Substring(25, 3),
            PuntoEmision = claveAcceso.Substring(28, 3),
            Secuencial = claveAcceso.Substring(31, 9),
            CodigoNumerico = claveAcceso.Substring(40, 8),
            DigitoVerificador = claveAcceso.Substring(48, 1)
        };

        var esValida = ValidarClaveAcceso(claveAcceso);

        _logger?.LogInformation("  📊 COMPONENTES:");
        _logger?.LogInformation("  [0-7]   Fecha:          {Fecha}", componentes.Fecha);
        _logger?.LogInformation("  [8-9]   Tipo Comp:      {Tipo}", componentes.TipoComprobante);
        _logger?.LogInformation("  [10-22] RUC:            {Ruc}", componentes.Ruc);
        _logger?.LogInformation("  [23]    Ambiente:       {Ambiente}", componentes.Ambiente);
        _logger?.LogInformation("  [24]    Tipo Emisión:   {TipoEmision}", componentes.TipoEmision);
        _logger?.LogInformation("  [25-27] Establecim:     {Estab}", componentes.Establecimiento);
        _logger?.LogInformation("  [28-30] Punto Emis:     {Punto}", componentes.PuntoEmision);
        _logger?.LogInformation("  [31-39] Secuencial:     {Sec}", componentes.Secuencial);
        _logger?.LogInformation("  [40-47] Cód Numérico:   {Cod}", componentes.CodigoNumerico);
        _logger?.LogInformation("  [48]    Dígito Verif:   {Dv}", componentes.DigitoVerificador);
        _logger?.LogInformation("");
        _logger?.LogInformation("  {Status} Clave {Validez}",
            esValida ? "✅" : "❌",
            esValida ? "VÁLIDA" : "INVÁLIDA");
        _logger?.LogInformation("╚════════════════════════════════════════════════════════════╝\n");

        return componentes;
    }

    /// <summary>
    /// 📋 Extrae información de una clave de acceso (sin logs)
    /// </summary>
    public ClaveAccesoInfo ExtraerInformacion(string claveAcceso)
    {
        if (string.IsNullOrWhiteSpace(claveAcceso) || claveAcceso.Length != 49)
        {
            throw new ArgumentException("La clave de acceso debe tener 49 dígitos");
        }

        return new ClaveAccesoInfo
        {
            Dia = claveAcceso.Substring(0, 2),
            Mes = claveAcceso.Substring(2, 2),
            Anio = claveAcceso.Substring(4, 4),
            TipoComprobante = claveAcceso.Substring(8, 2),
            Ruc = claveAcceso.Substring(10, 13),
            Ambiente = claveAcceso.Substring(23, 1),
            TipoEmision = claveAcceso.Substring(24, 1),
            Establecimiento = claveAcceso.Substring(25, 3),
            PuntoEmision = claveAcceso.Substring(28, 3),
            Secuencial = claveAcceso.Substring(31, 9),
            CodigoNumerico = claveAcceso.Substring(40, 8),
            DigitoVerificador = claveAcceso.Substring(48, 1)
        };
    }
}

/// <summary>
/// 📋 Clase para componentes decodificados
/// </summary>
public class ComponentesClave
{
    public string Fecha { get; set; } = string.Empty;
    public string TipoComprobante { get; set; } = string.Empty;
    public string Ruc { get; set; } = string.Empty;
    public string Ambiente { get; set; } = string.Empty;
    public string TipoEmision { get; set; } = string.Empty;
    public string Establecimiento { get; set; } = string.Empty;
    public string PuntoEmision { get; set; } = string.Empty;
    public string Secuencial { get; set; } = string.Empty;
    public string CodigoNumerico { get; set; } = string.Empty;
    public string DigitoVerificador { get; set; } = string.Empty;
}

/// <summary>
/// 📋 Clase con información extraída de una clave
/// </summary>
public class ClaveAccesoInfo
{
    public string Dia { get; set; } = string.Empty;
    public string Mes { get; set; } = string.Empty;
    public string Anio { get; set; } = string.Empty;
    public string TipoComprobante { get; set; } = string.Empty;
    public string Ruc { get; set; } = string.Empty;
    public string Ambiente { get; set; } = string.Empty;
    public string TipoEmision { get; set; } = string.Empty;
    public string Establecimiento { get; set; } = string.Empty;
    public string PuntoEmision { get; set; } = string.Empty;
    public string Secuencial { get; set; } = string.Empty;
    public string CodigoNumerico { get; set; } = string.Empty;
    public string DigitoVerificador { get; set; } = string.Empty;

    public DateTime FechaEmision => new DateTime(
        int.Parse(Anio),
        int.Parse(Mes),
        int.Parse(Dia));

    public string NumeroComprobante => $"{Establecimiento}-{PuntoEmision}-{Secuencial}";
}