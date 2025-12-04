// SistemaFacturacionSRI.Infrastructure/Services/SRI/RetryPolicy.cs
// T-080: Política de reintentos para operaciones del SRI

using Microsoft.Extensions.Logging;

namespace SistemaFacturacionSRI.Infrastructure.Services.SRI
{
    /// <summary>
    /// T-080: Política de reintentos con backoff exponencial
    /// </summary>
    public class RetryPolicy
    {
        private readonly ILogger _logger;
        private readonly int _maxReintentos;
        private readonly int _delayInicialMs;
        private readonly bool _backoffExponencial;
        private readonly double _factorBackoff;
        private readonly int _delayMaximoMs;

        public RetryPolicy(
            ILogger logger,
            int maxReintentos = 3,
            int delayInicialMs = 2000,
            bool backoffExponencial = true,
            double factorBackoff = 2.0,
            int delayMaximoMs = 30000)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _maxReintentos = maxReintentos;
            _delayInicialMs = delayInicialMs;
            _backoffExponencial = backoffExponencial;
            _factorBackoff = factorBackoff;
            _delayMaximoMs = delayMaximoMs;
        }

        /// <summary>
        /// T-080: Ejecuta una acción con reintentos automáticos
        /// </summary>
        public async Task<TResult> ExecuteAsync<TResult>(
            Func<int, Task<TResult>> action,
            Func<TResult, bool> esExitoso,
            Func<TResult, bool>? debeReintentar = null,
            CancellationToken cancellationToken = default)
        {
            Exception? ultimaExcepcion = null;
            TResult? ultimoResultado = default;

            for (int intento = 1; intento <= _maxReintentos; intento++)
            {
                try
                {
                    _logger.LogDebug("Intento {Intento}/{Max}", intento, _maxReintentos);

                    // Ejecutar acción
                    var resultado = await action(intento);
                    ultimoResultado = resultado;

                    // Verificar si fue exitoso
                    if (esExitoso(resultado))
                    {
                        _logger.LogDebug("✅ Operación exitosa en intento {Intento}", intento);
                        return resultado;
                    }

                    // Verificar si debe reintentar
                    if (debeReintentar != null && !debeReintentar(resultado))
                    {
                        _logger.LogDebug("❌ No reintentar según política");
                        return resultado;
                    }

                    // Si no es el último intento, esperar
                    if (intento < _maxReintentos)
                    {
                        await EsperarEntreReintentos(intento, cancellationToken);
                    }
                }
                catch (OperationCanceledException)
                {
                    _logger.LogWarning("⚠️  Operación cancelada");
                    throw;
                }
                catch (Exception ex)
                {
                    ultimaExcepcion = ex;
                    _logger.LogWarning(ex, "⚠️  Error en intento {Intento}/{Max}",
                        intento, _maxReintentos);

                    // Si es el último intento, lanzar excepción
                    if (intento >= _maxReintentos)
                    {
                        _logger.LogError("❌ Todos los reintentos fallaron");
                        throw new InvalidOperationException(
                            $"Operación falló después de {_maxReintentos} intentos", ex);
                    }

                    // Esperar antes del siguiente intento
                    await EsperarEntreReintentos(intento, cancellationToken);
                }
            }

            // Si llegamos aquí y hay una excepción, lanzarla
            if (ultimaExcepcion != null)
            {
                throw ultimaExcepcion;
            }

            // Retornar último resultado
            return ultimoResultado!;
        }

        /// <summary>
        /// T-080: Calcula el delay para un intento específico
        /// </summary>
        public int CalcularDelay(int numeroIntento)
        {
            if (!_backoffExponencial)
            {
                return _delayInicialMs;
            }

            // Backoff exponencial: delay * factor^(intento-1)
            var delay = _delayInicialMs * Math.Pow(_factorBackoff, numeroIntento - 1);
            return (int)Math.Min(delay, _delayMaximoMs);
        }

        /// <summary>
        /// Espera entre reintentos con delay calculado
        /// </summary>
        private async Task EsperarEntreReintentos(int numeroIntento, CancellationToken cancellationToken)
        {
            var delay = CalcularDelay(numeroIntento);
            _logger.LogInformation("⏳ Esperando {Delay}ms antes del siguiente intento...", delay);

            try
            {
                await Task.Delay(delay, cancellationToken);
            }
            catch (TaskCanceledException)
            {
                _logger.LogWarning("⚠️  Espera cancelada");
                throw;
            }
        }

        /// <summary>
        /// T-080: Determina si una excepción es retriable
        /// </summary>
        public static bool EsExcepcionRetriable(Exception ex)
        {
            return ex is TimeoutException ||
                   ex is HttpRequestException ||
                   ex is TaskCanceledException ||
                   (ex is InvalidOperationException && ex.Message.Contains("timeout", StringComparison.OrdinalIgnoreCase));
        }
    }

    /// <summary>
    /// T-080: Resultado de operación con reintentos
    /// </summary>
    public class RetryResult<T>
    {
        public bool Exitoso { get; set; }
        public T? Resultado { get; set; }
        public int NumeroIntentos { get; set; }
        public TimeSpan TiempoTotal { get; set; }
        public List<Exception> Excepciones { get; set; } = new List<Exception>();
        public string? MensajeError { get; set; }

        public static RetryResult<T> Success(T resultado, int intentos, TimeSpan tiempo)
        {
            return new RetryResult<T>
            {
                Exitoso = true,
                Resultado = resultado,
                NumeroIntentos = intentos,
                TiempoTotal = tiempo
            };
        }

        public static RetryResult<T> Failure(string mensaje, List<Exception> excepciones, int intentos, TimeSpan tiempo)
        {
            return new RetryResult<T>
            {
                Exitoso = false,
                MensajeError = mensaje,
                Excepciones = excepciones,
                NumeroIntentos = intentos,
                TiempoTotal = tiempo
            };
        }
    }
}