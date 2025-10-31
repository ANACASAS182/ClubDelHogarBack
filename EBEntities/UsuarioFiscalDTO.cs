using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EBEntities
{
    public class UsuarioFiscalDTO
    {
        public long UsuarioID { get; set; }
        public string NombreSAT { get; set; } = "";
        public string RFC { get; set; } = "";
        public string? CURP { get; set; }
        public string CodigoPostal { get; set; } = "";
        public string RegimenClave { get; set; } = "";
        public string? ConstanciaPath { get; set; }
        public string? ConstanciaHash { get; set; }
        public bool VerificadoSAT { get; set; }
        public DateTime? FechaVerificacion { get; set; }
    }
}
