using EBServices.Interfaces;
using System.Text;

namespace EBServices
{
    public class BaseService : IBaseService
    {
        public string LastError { get; set; } = string.Empty;
        public List<string> Errors { get; set; } = new();
        public bool HasError { get; set; } = false;

        /// <summary>Limpiar estado de error antes de cada operación pública del service.</summary>
        public void ResetError()
        {
            HasError = false;
            LastError = string.Empty;
            Errors.Clear();
        }

        /// <summary>
        /// Convierte una excepción en un mensaje de error para la API.
        /// En DEBUG incluye inner exceptions y stack trace; en Release, solo el mensaje.
        /// </summary>
        public void ExceptionToMessage(Exception ex, string firstMessage = "")
        {
            HasError = true;

            var sb = new StringBuilder();

            if (!string.IsNullOrWhiteSpace(firstMessage))
                sb.AppendLine(firstMessage.Trim());

#if DEBUG
            // Detalle completo en desarrollo
            sb.AppendLine(BuildExceptionDetails(ex));
#else
            // Resumen en producción
            sb.AppendLine(ex.Message);
            if (ex.InnerException != null)
                sb.AppendLine(ex.InnerException.Message);
#endif

            LastError = sb.ToString().Trim();

            // Log al stdout (o cambia por tu logger)
            try
            {
                Console.WriteLine("====== SERVICE ERROR ======");
                Console.WriteLine(LastError);
                Console.WriteLine("===========================");
            }
            catch { /* no-op */ }
        }

        /// <summary>Agrega un error a la lista y actualiza LastError.</summary>
        public void AddError(string message)
        {
            HasError = true;
            if (!string.IsNullOrWhiteSpace(message))
            {
                Errors.Add(message);
                LastError = string.Join(Environment.NewLine, Errors);
            }
        }

        /// <summary>Marca error si la condición es true.</summary>
        public void FailIf(bool condition, string message)
        {
            if (condition) AddError(message);
        }

        // ===== Helpers =====

        private static string BuildExceptionDetails(Exception ex)
        {
            var sb = new StringBuilder();

            var cur = ex;
            int depth = 0;
            while (cur != null && depth < 10)
            {
                sb.AppendLine($"[{depth}] {cur.GetType().FullName}: {cur.Message}");
                if (!string.IsNullOrWhiteSpace(cur.StackTrace))
                    sb.AppendLine(cur.StackTrace);

                cur = cur.InnerException!;
                depth++;
                if (cur != null) sb.AppendLine(); // separador entre niveles
            }

            return sb.ToString();
        }
    }
}
