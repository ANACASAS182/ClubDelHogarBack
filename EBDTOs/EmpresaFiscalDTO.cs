using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EBDTOs
{
    public class EmpresaFiscalDTO
    {
        public long EmpresaID { get; set; }
        public string RFC { get; set; } = "";
        public string RazonSocialSAT { get; set; } = "";
        public string CodigoPostal { get; set; } = "";
        public string? MetodoPago { get; set; }  // PUE|PPD
        public string? UsoCFDI { get; set; }     // G03|P01...
        public string RegimenClave { get; set; } = "601"; // opcional: default PM
    }
}
