using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EBDTOs
{
    public class EmpresaCreateDTO
    {
        public long? id { get; set; }
        public string? rfc { get; set; }
        public string? descripcion { get; set; }
        public string? razonSocial { get; set; }
        public string? nombreComercial { get; set; }
        public string? logotipoBase64 { get; set; }
        public int? giro { get; set; }
        public int? grupo { get; set; }
        public List<GrupoEmpresaDTO>? grupos { get; set; }

        public int? embajadorId { get; set; }
    }
}
