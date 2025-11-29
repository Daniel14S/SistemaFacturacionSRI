using Xunit;
using SistemaFacturacionSRI.Application.Services;

namespace SistemaFacturacionSRI.Tests.Services;

/// <summary>
/// Pruebas unitarias completas para ClaveAccesoGenerator
/// T-036: SPRINT 3 - DÍA 3
/// </summary>
public class ClaveAccesoGeneratorTests
{
    private readonly ClaveAccesoGenerator _generator;

    public ClaveAccesoGeneratorTests()
    {
        _generator = new ClaveAccesoGenerator();
    }

    #region Pruebas de Generación Básica

    [Fact]
    public void GenerarClaveAcceso_DebeRetornar48Digitos()
    {
        // Arrange
        var fecha = new DateTime(2024, 11, 27);
        var tipoComprobante = "01";
        var ruc = "1234567890001";
        var ambiente = "1";
        var establecimiento = "001";
        var puntoEmision = "001";
        var secuencial = "000000001";

        // Act
        var claveAcceso = _generator.GenerarClaveAcceso(
            fecha, tipoComprobante, ruc, ambiente,
            establecimiento, puntoEmision, secuencial);

        // Assert
        Assert.Equal(48, claveAcceso.Length);
        Assert.True(claveAcceso.All(char.IsDigit), "La clave debe contener solo dígitos");
    }

    [Fact]
    public void GenerarClaveAcceso_ConMismosDatos_DebeGenerarClavesUnicas()
    {
        // Arrange
        var fecha = new DateTime(2024, 11, 27);
        var parametros = ("01", "1234567890001", "1", "001", "001", "000000001");

        // Act - Generar 10 claves con los mismos parámetros
        var claves = new HashSet<string>();
        for (int i = 0; i < 10; i++)
        {
            var clave = _generator.GenerarClaveAcceso(
                fecha, parametros.Item1, parametros.Item2, parametros.Item3,
                parametros.Item4, parametros.Item5, parametros.Item6);
            claves.Add(clave);
        }

        // Assert - Todas deben ser únicas (por el código numérico aleatorio)
        Assert.Equal(10, claves.Count);
    }

    [Fact]
    public void GenerarClaveAcceso_ConCodigoNumericoEspecifico_DebeUsarlo()
    {
        // Arrange
        var fecha = new DateTime(2024, 11, 27);
        var codigoNumerico = "12345678";

        // Act
        var clave = _generator.GenerarClaveAcceso(
            fecha, "01", "1234567890001", "1",
            "001", "001", "000000001", codigoNumerico);

        // Assert
        Assert.Contains(codigoNumerico, clave);
    }

    #endregion

    #region Pruebas de Validación

    [Fact]
    public void ValidarClaveAcceso_ConClaveValida_DebeRetornarTrue()
    {
        // Arrange
        var fecha = new DateTime(2024, 11, 27);
        var claveAcceso = _generator.GenerarClaveAcceso(
            fecha, "01", "1234567890001", "1",
            "001", "001", "000000001");

        // Act
        var esValida = _generator.ValidarClaveAcceso(claveAcceso);

        // Assert
        Assert.True(esValida);
    }

    [Fact]
    public void ValidarClaveAcceso_ConDigitoVerificadorIncorrecto_DebeRetornarFalse()
    {
        // Arrange - Generar clave válida y modificar el último dígito
        var fecha = new DateTime(2024, 11, 27);
        var claveValida = _generator.GenerarClaveAcceso(
            fecha, "01", "1234567890001", "1",
            "001", "001", "000000001");
        
        // Cambiar el último dígito
        var ultimoDigito = int.Parse(claveValida[47].ToString());
        var nuevoDigito = (ultimoDigito + 1) % 10;
        var claveInvalida = claveValida.Substring(0, 47) + nuevoDigito;

        // Act
        var esValida = _generator.ValidarClaveAcceso(claveInvalida);

        // Assert
        Assert.False(esValida);
    }

    [Theory]
    [InlineData("123456789012345678901234567890123456789012345678")] // 48 dígitos válidos
    [InlineData("2711202401123456789000110010010000000012345678")]   // Sin dígito verificador
    public void ValidarClaveAcceso_ConLongitudIncorrecta_DebeRetornarFalse(string claveInvalida)
    {
        // Act
        var esValida = _generator.ValidarClaveAcceso(claveInvalida);

        // Assert
        Assert.False(esValida);
    }

    [Fact]
    public void ValidarClaveAcceso_ConCaracteresNoNumericos_DebeRetornarFalse()
    {
        // Arrange
        var claveInvalida = "27112024011234567890001100100100000000123456ABC";

        // Act
        var esValida = _generator.ValidarClaveAcceso(claveInvalida);

        // Assert
        Assert.False(esValida);
    }

    [Fact]
    public void ValidarClaveAcceso_ConNull_DebeRetornarFalse()
    {
        // Act
        var esValida = _generator.ValidarClaveAcceso(null!); // ⬅️ CORREGIDO: Agregado ! para suprimir warning

        // Assert
        Assert.False(esValida);
    }

    #endregion

    #region Pruebas de Validación de Parámetros

    [Theory]
    [InlineData("1")]      // Tipo comprobante debe tener 2 dígitos
    [InlineData("123")]    // Tipo comprobante debe tener 2 dígitos
    [InlineData("AB")]     // Tipo comprobante debe ser numérico
    public void GenerarClaveAcceso_ConTipoComprobanteInvalido_DebeLanzarExcepcion(
        string tipoComprobante) // ⬅️ CORREGIDO: Eliminado parámetro 'razon'
    {
        // Arrange
        var fecha = new DateTime(2024, 11, 27);

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            _generator.GenerarClaveAcceso(
                fecha, tipoComprobante, "1234567890001", "1",
                "001", "001", "000000001"));

        Assert.Contains("tipo de comprobante", exception.Message.ToLower());
    }

    [Theory]
    [InlineData("123456789000")]    // RUC muy corto
    [InlineData("12345678900012")]  // RUC muy largo
    [InlineData("12345678900AB")]   // RUC con letras
    public void GenerarClaveAcceso_ConRucInvalido_DebeLanzarExcepcion(
        string ruc) // ⬅️ CORREGIDO: Eliminado parámetro 'razon'
    {
        // Arrange
        var fecha = new DateTime(2024, 11, 27);

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            _generator.GenerarClaveAcceso(
                fecha, "01", ruc, "1",
                "001", "001", "000000001"));

        Assert.Contains("ruc", exception.Message.ToLower());
    }

    [Theory]
    [InlineData("0")]   // Ambiente 0 no válido
    [InlineData("3")]   // Ambiente 3 no válido
    [InlineData("12")]  // Ambiente con más de 1 dígito
    public void GenerarClaveAcceso_ConAmbienteInvalido_DebeLanzarExcepcion(
        string ambiente) // ⬅️ CORREGIDO: Eliminado parámetro 'razon'
    {
        // Arrange
        var fecha = new DateTime(2024, 11, 27);

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            _generator.GenerarClaveAcceso(
                fecha, "01", "1234567890001", ambiente,
                "001", "001", "000000001"));

        Assert.Contains("ambiente", exception.Message.ToLower());
    }

    [Theory]
    [InlineData("01")]   // Establecimiento muy corto
    [InlineData("0001")] // Establecimiento muy largo
    [InlineData("0A1")]  // Establecimiento con letras
    public void GenerarClaveAcceso_ConEstablecimientoInvalido_DebeLanzarExcepcion(
        string establecimiento) // ⬅️ CORREGIDO: Eliminado parámetro 'razon'
    {
        // Arrange
        var fecha = new DateTime(2024, 11, 27);

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            _generator.GenerarClaveAcceso(
                fecha, "01", "1234567890001", "1",
                establecimiento, "001", "000000001"));

        Assert.Contains("establecimiento", exception.Message.ToLower());
    }

    [Theory]
    [InlineData("00000001")]   // Secuencial muy corto
    [InlineData("0000000012")] // Secuencial muy largo
    public void GenerarClaveAcceso_ConSecuencialInvalido_DebeLanzarExcepcion(
        string secuencial) // ⬅️ CORREGIDO: Eliminado parámetro 'razon'
    {
        // Arrange
        var fecha = new DateTime(2024, 11, 27);

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            _generator.GenerarClaveAcceso(
                fecha, "01", "1234567890001", "1",
                "001", "001", secuencial));

        Assert.Contains("secuencial", exception.Message.ToLower());
    }

    #endregion

    #region Pruebas de Código Numérico

    [Fact]
    public void GenerarCodigoNumerico_DebeRetornar8Digitos()
    {
        // Act
        var codigo = _generator.GenerarCodigoNumerico();

        // Assert
        Assert.Equal(8, codigo.Length);
        Assert.True(codigo.All(char.IsDigit));
    }

    [Fact]
    public void GenerarCodigoNumerico_DebeGenerarValoresUnicos()
    {
        // Arrange & Act
        var codigos = new HashSet<string>();
        for (int i = 0; i < 100; i++)
        {
            codigos.Add(_generator.GenerarCodigoNumerico());
        }

        // Assert - Al menos el 95% deben ser únicos
        Assert.True(codigos.Count >= 95, $"Solo {codigos.Count} códigos únicos de 100");
    }

    #endregion

    #region Pruebas de Extracción de Información

    [Fact]
    public void ExtraerInformacion_DebeExtraerTodosLosCamposCorrectamente()
    {
        // Arrange
        var fecha = new DateTime(2024, 11, 27);
        var tipoComprobante = "01";
        var ruc = "1234567890001";
        var ambiente = "1";
        var establecimiento = "001";
        var puntoEmision = "002";
        var secuencial = "000000123";
        var codigoNumerico = "87654321";

        var claveAcceso = _generator.GenerarClaveAcceso(
            fecha, tipoComprobante, ruc, ambiente,
            establecimiento, puntoEmision, secuencial, codigoNumerico);

        // Act
        var info = _generator.ExtraerInformacion(claveAcceso);

        // Assert
        Assert.Equal("27", info.Dia);
        Assert.Equal("11", info.Mes);
        Assert.Equal("2024", info.Anio);
        Assert.Equal("01", info.TipoComprobante);
        Assert.Equal("1234567890001", info.Ruc);
        Assert.Equal("1", info.Ambiente);
        Assert.Equal("001", info.Establecimiento);
        Assert.Equal("002", info.PuntoEmision);
        Assert.Equal("000000123", info.Secuencial);
        Assert.Equal("87654321", info.CodigoNumerico);
    }

    [Fact]
    public void ExtraerInformacion_FechaEmision_DebeSerCorrecta()
    {
        // Arrange
        var fechaEsperada = new DateTime(2024, 11, 27);
        var claveAcceso = _generator.GenerarClaveAcceso(
            fechaEsperada, "01", "1234567890001", "1",
            "001", "001", "000000001");

        // Act
        var info = _generator.ExtraerInformacion(claveAcceso);

        // Assert
        Assert.Equal(fechaEsperada.Date, info.FechaEmision.Date);
    }

    [Fact]
    public void ExtraerInformacion_NumeroComprobante_DebeEstarBienFormateado()
    {
        // Arrange
        var claveAcceso = _generator.GenerarClaveAcceso(
            DateTime.Now, "01", "1234567890001", "1",
            "001", "002", "000000123");

        // Act
        var info = _generator.ExtraerInformacion(claveAcceso);

        // Assert
        Assert.Equal("001-002-000000123", info.NumeroComprobante);
    }

    [Theory]
    [InlineData("123")]
    [InlineData("12345678901234567890123456789012345678901234567")]
    public void ExtraerInformacion_ConClaveInvalida_DebeLanzarExcepcion(string claveInvalida)
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            _generator.ExtraerInformacion(claveInvalida));
    }
    
    [Fact]
    public void ExtraerInformacion_ConNull_DebeLanzarExcepcion()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            _generator.ExtraerInformacion(null!)); // ⬅️ CORREGIDO: Agregado ! para suprimir warning
    }

    #endregion

    #region Pruebas de Casos Especiales

    [Fact]
    public void GenerarClaveAcceso_ConFechaLimite_DebeGenerarCorrectamente()
    {
        // Arrange - Fecha límite de año
        var fecha = new DateTime(2024, 12, 31);

        // Act
        var clave = _generator.GenerarClaveAcceso(
            fecha, "01", "1234567890001", "1",
            "001", "001", "000000001");

        // Assert
        Assert.StartsWith("31122024", clave);
        Assert.Equal(48, clave.Length);
        Assert.True(_generator.ValidarClaveAcceso(clave));
    }

    [Fact]
    public void GenerarClaveAcceso_ConSecuencialMaximo_DebeGenerarCorrectamente()
    {
        // Arrange - Secuencial máximo
        var fecha = DateTime.Now;
        var secuencialMaximo = "999999999";

        // Act
        var clave = _generator.GenerarClaveAcceso(
            fecha, "01", "1234567890001", "1",
            "001", "001", secuencialMaximo);

        // Assert
        Assert.Contains(secuencialMaximo, clave);
        Assert.True(_generator.ValidarClaveAcceso(clave));
    }

    [Fact]
    public void GenerarClaveAcceso_AmbienteProduccion_DebeGenerarCorrectamente()
    {
        // Arrange
        var fecha = DateTime.Now;

        // Act
        var clave = _generator.GenerarClaveAcceso(
            fecha, "01", "1234567890001", "2", // Ambiente Producción
            "001", "001", "000000001");

        // Assert
        var info = _generator.ExtraerInformacion(clave);
        Assert.Equal("2", info.Ambiente);
        Assert.True(_generator.ValidarClaveAcceso(clave));
    }

    #endregion

    #region Pruebas de Rendimiento

    [Fact]
    public void GenerarClaveAcceso_1000Veces_DebeCompletarEnMenosDe1Segundo()
    {
        // Arrange
        var fecha = DateTime.Now;
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        // Act
        for (int i = 0; i < 1000; i++)
        {
            _generator.GenerarClaveAcceso(
                fecha, "01", "1234567890001", "1",
                "001", "001", $"{i:D9}");
        }

        stopwatch.Stop();

        // Assert
        Assert.True(stopwatch.ElapsedMilliseconds < 1000,
            $"Tomó {stopwatch.ElapsedMilliseconds}ms para generar 1000 claves");
    }

    #endregion
}