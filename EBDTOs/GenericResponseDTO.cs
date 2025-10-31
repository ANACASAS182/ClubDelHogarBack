using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EBDTOs
{
    public class GenericResponseDTO<T>
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
        public T? Data { get; set; }
        public List<string>? Errors { get; set; }
        public bool AutoShowError { get; set; }

        public GenericResponseDTO(bool success, string message, T? data = default, bool autoShowError = true, List<string>? errors = null)
        {
            Success = success;
            Message = message;
            Data = data;
            Errors = errors;
            AutoShowError = autoShowError;
        }

        public static GenericResponseDTO<T> Ok(T data, string message = "Operación exitosa") =>
            new GenericResponseDTO<T>(true, message, data);

        public static GenericResponseDTO<T> Fail(string message, bool autoShowError = true, List<string>? errors = null) =>
            new GenericResponseDTO<T>(false, message, default , autoShowError, errors);

    }
}
