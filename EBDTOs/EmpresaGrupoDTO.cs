using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EBDTOs
{
    public class EmpresaGrupoDTO
    {
        public long ID { get; set; }
        public int GrupoID { get; set; }
        public long EmpresaID { get; set; }
        public string? NombreGrupo { get; set; }
    }
}
