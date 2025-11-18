using System;
using System.Linq;
using SistemaFacturacionSRI.Application.Validators;
using Xunit;

namespace SistemaFacturacionSRI.Tests.Validators
{
    public class CedulaEcuadorValidatorTests
    {
        [Theory]
        [InlineData("171003406")]
        [InlineData("092668785")]
        [InlineData("012345678")]
        public void EsValida_ReturnsTrue_ForValidCedulas(string primerosNueveDigitos)
        {
            var cedula = BuildCedula(primerosNueveDigitos);

            Assert.True(CedulaEcuadorValidator.EsValida(cedula));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void EsValida_ReturnsFalse_ForNullOrWhitespace(string? cedula)
        {
            Assert.False(CedulaEcuadorValidator.EsValida(cedula));
        }

        [Theory]
        [InlineData("123456789")] // Muy corta
        [InlineData("12345678901")] // Muy larga
        [InlineData("12345678A0")] // Caracter inválido
        public void EsValida_ReturnsFalse_ForInvalidLengthOrCharacters(string cedula)
        {
            Assert.False(CedulaEcuadorValidator.EsValida(cedula));
        }

        [Fact]
        public void EsValida_ReturnsFalse_ForInvalidProvince()
        {
            const string cedula = "2501234567"; // provincia 25 no existe

            Assert.False(CedulaEcuadorValidator.EsValida(cedula));
        }

        [Fact]
        public void EsValida_ReturnsFalse_ForInvalidCheckDigit()
        {
            var valida = BuildCedula("171234567");
            var digitoAlterado = valida[^1] == '9' ? '0' : '9';
            var invalida = valida[..9] + digitoAlterado;

            Assert.True(CedulaEcuadorValidator.EsValida(valida));
            Assert.False(CedulaEcuadorValidator.EsValida(invalida));
        }

        [Fact]
        public void TryValidar_ShouldReturnMessage_ForInvalidCedula()
        {
            var resultado = CedulaEcuadorValidator.TryValidar("123", out var mensaje);

            Assert.False(resultado);
            Assert.False(string.IsNullOrWhiteSpace(mensaje));
        }

        private static string BuildCedula(string firstNineDigits)
        {
            if (firstNineDigits.Length != 9)
                throw new ArgumentException("Se requieren nueve dígitos para construir la cédula.", nameof(firstNineDigits));

            if (!firstNineDigits.All(char.IsDigit))
                throw new ArgumentException("Todos los caracteres deben ser numéricos.", nameof(firstNineDigits));

            int[] coeficientes = { 2, 1, 2, 1, 2, 1, 2, 1, 2 };
            int suma = 0;

            for (int i = 0; i < firstNineDigits.Length; i++)
            {
                int valor = (firstNineDigits[i] - '0') * coeficientes[i];
                suma += valor > 9 ? valor - 9 : valor;
            }

            int residuo = suma % 10;
            int digitoVerificador = residuo == 0 ? 0 : 10 - residuo;

            return firstNineDigits + digitoVerificador.ToString();
        }
    }
}
