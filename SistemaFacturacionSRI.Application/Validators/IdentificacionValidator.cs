namespace SistemaFacturacionSRI.Application.Validators
{
    /// <summary>
    /// Validador de identificaciones según códigos del SRI Ecuador.
    /// </summary>
    public static class IdentificacionValidator
    {
        /// <summary>
        /// Valida el formato de identificación según el código SRI
        /// </summary>
        /// <param name="codigoSRI">Código SRI del tipo de identificación</param>
        /// <param name="identificacion">Número de identificación a validar</param>
        /// <returns>True si el formato es válido</returns>
        public static bool ValidarFormato(string codigoSRI, string identificacion)
        {
            if (string.IsNullOrWhiteSpace(identificacion))
                return false;

            identificacion = identificacion.Trim();

            return codigoSRI switch
            {
                "04" => ValidarCedula(identificacion),      // RUC Persona Natural
                "05" => ValidarCedula(identificacion),      // Cédula
                "06" => ValidarPasaporte(identificacion),   // Pasaporte
                "07" => ValidarRUC(identificacion),         // RUC Privada
                "08" => ValidarRUC(identificacion),         // RUC Pública
                _ => identificacion.Length >= 6 && identificacion.Length <= 20
            };
        }

        /// <summary>
        /// Valida formato de cédula ecuatoriana (10 dígitos)
        /// </summary>
        private static bool ValidarCedula(string cedula)
        {
            if (string.IsNullOrWhiteSpace(cedula))
                return false;

            cedula = cedula.Trim();

            // Debe tener exactamente 10 dígitos
            if (cedula.Length != 10 || !cedula.All(char.IsDigit))
                return false;

            // Los dos primeros dígitos deben corresponder a una provincia válida (01-24)
            if (!int.TryParse(cedula.Substring(0, 2), out int provincia))
                return false;

            if (provincia < 1 || provincia > 24)
                return false;

            // Validación del dígito verificador (algoritmo módulo 10)
            return ValidarDigitoVerificadorCedula(cedula);
        }

        /// <summary>
        /// Valida el dígito verificador de la cédula ecuatoriana
        /// </summary>
        private static bool ValidarDigitoVerificadorCedula(string cedula)
        {
            try
            {
                int[] coeficientes = { 2, 1, 2, 1, 2, 1, 2, 1, 2 };
                int suma = 0;

                for (int i = 0; i < 9; i++)
                {
                    int valor = int.Parse(cedula[i].ToString()) * coeficientes[i];
                    suma += (valor > 9) ? valor - 9 : valor;
                }

                int residuo = suma % 10;
                int digitoVerificador = (residuo == 0) ? 0 : 10 - residuo;

                return digitoVerificador == int.Parse(cedula[9].ToString());
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Valida formato de RUC ecuatoriano (13 dígitos)
        /// </summary>
        private static bool ValidarRUC(string ruc)
        {
            if (string.IsNullOrWhiteSpace(ruc))
                return false;

            ruc = ruc.Trim();

            // Debe tener exactamente 13 dígitos
            if (ruc.Length != 13 || !ruc.All(char.IsDigit))
                return false;

            // Los dos primeros dígitos deben corresponder a una provincia válida
            if (!int.TryParse(ruc.Substring(0, 2), out int provincia))
                return false;

            if (provincia < 1 || provincia > 24)
                return false;

            // El tercer dígito debe ser 6, 7, 8 o 9 para sociedades
            int tercerDigito = int.Parse(ruc[2].ToString());
            if (tercerDigito < 6 || tercerDigito > 9)
                return false;

            // Los últimos 3 dígitos deben ser "001" o mayores
            string establecimiento = ruc.Substring(10, 3);
            if (!int.TryParse(establecimiento, out int numEstablecimiento) || numEstablecimiento < 1)
                return false;

            return true;
        }

        /// <summary>
        /// Valida formato de pasaporte (6-20 caracteres alfanuméricos)
        /// </summary>
        private static bool ValidarPasaporte(string pasaporte)
        {
            if (string.IsNullOrWhiteSpace(pasaporte))
                return false;

            pasaporte = pasaporte.Trim();

            // Entre 6 y 20 caracteres alfanuméricos
            return pasaporte.Length >= 6 &&
                   pasaporte.Length <= 20 &&
                   pasaporte.All(c => char.IsLetterOrDigit(c));
        }

        /// <summary>
        /// Obtiene un mensaje de error descriptivo según el código SRI
        /// </summary>
        public static string ObtenerMensajeError(string codigoSRI)
        {
            return codigoSRI switch
            {
                "04" => "El RUC de persona natural debe tener 10 dígitos válidos de cédula",
                "05" => "La cédula debe tener exactamente 10 dígitos numéricos válidos",
                "06" => "El pasaporte debe contener entre 6 y 20 caracteres alfanuméricos",
                "07" => "El RUC de sociedad privada debe tener 13 dígitos numéricos válidos",
                "08" => "El RUC de sociedad pública debe tener 13 dígitos numéricos válidos",
                _ => "Formato de identificación inválido"
            };
        }
    }
}