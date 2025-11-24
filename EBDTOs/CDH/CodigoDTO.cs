using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EBDTOs.CDH
{
    public class CodigoEnviarDTO
    {
        public string? Telefono { get; set; }
        public string? Correo { get; set; }
        public bool UsarCorreo { get; set; }
    }

    public class CodigoValidarDTO
    {
        public string Codigo { get; set; } = "";
        public string? Telefono { get; set; }
        public string? Correo { get; set; }
    }

    public class CDHRegistroVerificacionDTO
    {
        public int ID { get; set; }
    }
}
