using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EBDTOs
{
    public class CelulaDTO
    {
        public UsuarioDTO? Yo { get; set; }
        public UsuarioDTO? Padre { get; set; }
        public List<UsuarioDTO>? Hijos { get; set; }
        
    }
}
