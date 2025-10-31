using EBEntities.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EBEntities
{
    public class Periodo : Base, ISoftDeletable 
    { 
    
        public int Anio { get; set; }   
        public int Mes { get; set; }
        public DateTime FechaInicio { get; set; }
        public DateTime FechaFin { get; set; }
        public DateTime FechaPagoEmpresas { get; set; }
        public DateTime FechaPagoEmbajadores { get; set; }

    }
}