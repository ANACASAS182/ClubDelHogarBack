using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EBDTOs
{
    public class PeriodoDTO
    {
        public long? ID {  get; set; }
        public int Anio { get; set; }
        public int Mes { get; set; }
        public string MesLetra { get; set; }
        public DateTime FechaInicio { get; set; }
        public DateTime FechaFin { get; set; }
        public DateTime FechaPagoEmpresas { get; set; }
        public DateTime FechaPagoEmbajadores { get; set; }
    }
}
