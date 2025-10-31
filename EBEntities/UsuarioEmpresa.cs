using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EBEntities
{
    public class UsuarioEmpresa
    {
        public long ID { get; set; }
        public long EmpresaID { get; set; }
        public long UsuarioID { get; set; }
    }
}
