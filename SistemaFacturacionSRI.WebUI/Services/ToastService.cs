namespace SistemaFacturacionSRI.WebUI.Services
{
    /// <summary>
    /// Servicio para mostrar notificaciones toast en la interfaz.
    /// </summary>
    public class ToastService
    {
        /// <summary>
        /// Evento que se dispara cuando se muestra un toast.
        /// </summary>
        public event Action<string, ToastType>? OnShow;

        /// <summary>
        /// Muestra un toast de éxito.
        /// </summary>
        public void ShowSuccess(string message)
        {
            OnShow?.Invoke(message, ToastType.Success);
        }

        /// <summary>
        /// Muestra un toast de error.
        /// </summary>
        public void ShowError(string message)
        {
            OnShow?.Invoke(message, ToastType.Error);
        }

        /// <summary>
        /// Muestra un toast de advertencia.
        /// </summary>
        public void ShowWarning(string message)
        {
            OnShow?.Invoke(message, ToastType.Warning);
        }

        /// <summary>
        /// Muestra un toast de información.
        /// </summary>
        public void ShowInfo(string message)
        {
            OnShow?.Invoke(message, ToastType.Info);
        }
    }

    /// <summary>
    /// Tipos de toast disponibles.
    /// </summary>
    public enum ToastType
    {
        Success,
        Error,
        Warning,
        Info
    }
}