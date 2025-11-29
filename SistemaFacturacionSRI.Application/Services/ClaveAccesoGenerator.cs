namespace SistemaFacturacionSRI.Application.Services;

/// <summary>
/// Generador de Clave de Acceso para comprobantes electrónicos del SRI
/// T-035: SPRINT 3 - DÍA 2
/// 
/// La clave de acceso tiene 48 dígitos y sigue este formato:
/// DDMMAAAATCTESSSSSSSSSSCNNNNNNNNN1
/// 
/// DD = Día (2 dígitos)
/// MM = Mes (2 dígitos)
/// AAAA = Año (4 dígitos)
/// TC = Tipo de Comprobante (2 dígitos: 01=Factura)
/// RUC = RUC del emisor (13 dígitos)
/// TE = Tipo de Emisión (1 dígito: 1=Normal, 2=Contingencia)
/// E = Establecimiento (3 dígitos: 001)
/// PE = Punto de Emisión (3 dígitos: 001)
/// S = Secuencial (9 dígitos: 000000001)
/// CN = Código Numérico (8 dígitos aleatorios)
/// DV = Dígito Verificador (1 dígito calculado con módulo 11)
/// 
/// Ejemplo: 2511202501123456789000110010010000000011234567801
/// </summary>
public class ClaveAccesoGenerator
{
    /// <summary>
    /// Genera una clave de acceso completa de 48 dígitos
    /// </summary>
    /// <param name="fechaEmision">Fecha de emisión del comprobante</param>
    /// <param name="tipoComprobante">Tipo de comprobante (01=Factura, 04=NotaCrédito, etc.)</param>
    /// <param name="ruc">RUC del emisor (13 dígitos)</param>
    /// <param name="ambiente">Ambiente SRI (1=Pruebas, 2=Producción)</param>
    /// <param name="establecimiento">Código de establecimiento (3 dígitos: 001)</param>
    /// <param name="puntoEmision">Punto de emisión (3 dígitos: 001)</param>
    /// <param name="secuencial">Número secuencial (9 dígitos: 000000001)</param>
    /// <param name="codigoNumerico">Código numérico aleatorio (8 dígitos). Si es null, se genera automáticamente</param>
    /// <returns>Clave de acceso de 48 dígitos</returns>
    public string GenerarClaveAcceso(
        DateTime fechaEmision,
        string tipoComprobante,
        string ruc,
        string ambiente,
        string establecimiento,
        string puntoEmision,
        string secuencial,
        string? codigoNumerico = null)
    {
        // Validaciones
        ValidarParametros(tipoComprobante, ruc, ambiente, establecimiento, puntoEmision, secuencial);
        
        // Generar código numérico si no se proporciona
        if (string.IsNullOrEmpty(codigoNumerico))
        {
            codigoNumerico = GenerarCodigoNumerico();
        }
        
        // Construir la clave base (47 dígitos)
        string claveBase = $"{fechaEmision:ddMMyyyy}" +  // 8 dígitos: DDMMAAAA
                          $"{tipoComprobante}" +          // 2 dígitos
                          $"{ruc}" +                      // 13 dígitos
                          $"{ambiente}" +                 // 1 dígito
                          $"{establecimiento}" +          // 3 dígitos
                          $"{puntoEmision}" +            // 3 dígitos
                          $"{secuencial}" +              // 9 dígitos
                          $"{codigoNumerico}";           // 8 dígitos
        
        // Validar longitud de clave base
        if (claveBase.Length != 47)
        {
            throw new InvalidOperationException(
                $"La clave base debe tener 47 dígitos. Longitud actual: {claveBase.Length}. " +
                $"Clave: {claveBase}");
        }
        
        // Calcular dígito verificador
        int digitoVerificador = CalcularModulo11(claveBase);
        
        // Retornar clave completa de 48 dígitos
        return claveBase + digitoVerificador;
    }
    
    /// <summary>
    /// Calcula el dígito verificador usando el algoritmo de módulo 11
    /// </summary>
    /// <param name="claveBase">Clave de 47 dígitos sin el dígito verificador</param>
    /// <returns>Dígito verificador (0-9 o 1 si da 10)</returns>
    private int CalcularModulo11(string claveBase)
    {
        // Multiplicadores que se repiten cíclicamente: 2, 3, 4, 5, 6, 7
        int[] multiplicadores = { 2, 3, 4, 5, 6, 7 };
        int suma = 0;
        int multiplicadorIndex = 0;
        
        // Recorrer la clave de derecha a izquierda
        for (int i = claveBase.Length - 1; i >= 0; i--)
        {
            // Obtener el dígito actual
            if (!char.IsDigit(claveBase[i]))
            {
                throw new InvalidOperationException(
                    $"La clave contiene caracteres no numéricos: {claveBase}");
            }
            
            int digito = int.Parse(claveBase[i].ToString());
            
            // Multiplicar por el factor correspondiente
            suma += digito * multiplicadores[multiplicadorIndex];
            
            // Avanzar al siguiente multiplicador (ciclicamente)
            multiplicadorIndex = (multiplicadorIndex + 1) % multiplicadores.Length;
        }
        
        // Calcular módulo 11
        int residuo = suma % 11;
        int resultado = 11 - residuo;
        
        // Reglas especiales del SRI
        if (resultado == 11) return 0;  // Si da 11, el dígito es 0
        if (resultado == 10) return 1;  // Si da 10, el dígito es 1
        
        return resultado;  // En otros casos, retornar el resultado
    }
    
    /// <summary>
    /// Genera un código numérico aleatorio de 8 dígitos
    /// </summary>
    /// <returns>Código numérico de 8 dígitos</returns>
    public string GenerarCodigoNumerico()
    {
        Random random = new Random();
        
        // Generar número entre 10000000 y 99999999
        int numero = random.Next(10000000, 99999999);
        
        return numero.ToString();
    }
    
    /// <summary>
    /// Valida que todos los parámetros tengan el formato correcto
    /// </summary>
    private void ValidarParametros(
        string tipoComprobante,
        string ruc,
        string ambiente,
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
    /// Verifica si una cadena contiene solo números
    /// </summary>
    private bool EsSoloNumeros(string texto)
    {
        return texto.All(char.IsDigit);
    }
    
    /// <summary>
    /// Valida si una clave de acceso es válida
    /// </summary>
    /// <param name="claveAcceso">Clave de acceso a validar</param>
    /// <returns>True si es válida, False si no</returns>
    public bool ValidarClaveAcceso(string claveAcceso)
    {
        // Validar longitud
        if (string.IsNullOrWhiteSpace(claveAcceso) || claveAcceso.Length != 48)
        {
            return false;
        }
        
        // Validar que solo contenga números
        if (!EsSoloNumeros(claveAcceso))
        {
            return false;
        }
        
        // Extraer la clave base (primeros 47 dígitos)
        string claveBase = claveAcceso.Substring(0, 47);
        
        // Extraer el dígito verificador (último dígito)
        int digitoVerificadorRecibido = int.Parse(claveAcceso[47].ToString());
        
        // Calcular el dígito verificador esperado
        int digitoVerificadorCalculado = CalcularModulo11(claveBase);
        
        // Comparar
        return digitoVerificadorRecibido == digitoVerificadorCalculado;
    }
    
    /// <summary>
    /// Extrae información de una clave de acceso
    /// </summary>
    /// <param name="claveAcceso">Clave de acceso de 48 dígitos</param>
    /// <returns>Objeto con la información extraída</returns>
    public ClaveAccesoInfo ExtraerInformacion(string claveAcceso)
    {
        if (string.IsNullOrWhiteSpace(claveAcceso) || claveAcceso.Length != 48)
        {
            throw new ArgumentException("La clave de acceso debe tener 48 dígitos");
        }
        
        return new ClaveAccesoInfo
        {
            Dia = claveAcceso.Substring(0, 2),
            Mes = claveAcceso.Substring(2, 2),
            Anio = claveAcceso.Substring(4, 4),
            TipoComprobante = claveAcceso.Substring(8, 2),
            Ruc = claveAcceso.Substring(10, 13),
            Ambiente = claveAcceso.Substring(23, 1),
            Establecimiento = claveAcceso.Substring(24, 3),
            PuntoEmision = claveAcceso.Substring(27, 3),
            Secuencial = claveAcceso.Substring(30, 9),
            CodigoNumerico = claveAcceso.Substring(39, 8),
            DigitoVerificador = claveAcceso.Substring(47, 1)
        };
    }
}

/// <summary>
/// Clase que contiene la información extraída de una clave de acceso
/// </summary>
public class ClaveAccesoInfo
{
    public string Dia { get; set; } = string.Empty;
    public string Mes { get; set; } = string.Empty;
    public string Anio { get; set; } = string.Empty;
    public string TipoComprobante { get; set; } = string.Empty;
    public string Ruc { get; set; } = string.Empty;
    public string Ambiente { get; set; } = string.Empty;
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