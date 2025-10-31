using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EBDTOs
{
    public class EmpresaDTO
    {
        public int id { get; set; }
        public string razonSocial { get; set; }
        public string nombreComercial { get; set; }
        public int GrupoID { get; set; }

        // 👇 FALTABA (minúsculas para coincidir con el alias del SELECT)
        public string logotipoBase64 { get; set; } = "";
        // (opcional)
        public string logotipoPath { get; set; } = "";
    }

    public class GrupoDTO {
        public int? id { get; set; }
        public string nombre { get; set; }
    }

    public class EmbajadorDTO
    {
        public int id { get; set; }
        public string nombres { get; set; }
        public string apellidos { get; set; }
    }
}
