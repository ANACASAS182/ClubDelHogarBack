using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EBDTOs
{
    public sealed class UsuarioFiscalDTO
    {
        public long UsuarioID { get; set; }
        public string? NombreSAT { get; set; }
        public string RFC { get; set; } = "";
        public string? CURP { get; set; }

        // En DB a veces es int; en FE lo manejas como string.
        public string CodigoPostal { get; set; } = "";

        // Catálogo/clave puede venir vacío o null.
        public string? RegimenClave { get; set; }

        public string? ConstanciaPath { get; set; }
        public string? ConstanciaHash { get; set; }

        // Estos dos deben ser anulables si la verificación aún no ocurre.
        public bool? VerificadoSAT { get; set; }
        public DateTime? FechaVerificacion { get; set; }

        public DateTime FechaCreacion { get; set; }    // si en DB es NOT NULL, déjalo no anulable
        public DateTime? FechaActualizacion { get; set; }
    }


}
