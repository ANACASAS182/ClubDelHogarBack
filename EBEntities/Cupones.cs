using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EBEntities
{
    public class Cupones
    {
        public long ID { get; set; }
        public string? codigo { get; set; } 
        public long productoID { get; set; }
        public long EmbajadorID { get; set; }   
        public int estatus { get; set; }
        public DateTime vigencia { get; set; }
        public int referidoID { get; set; }
        public DateTime fechacreacion { get; set; }
        public int usuarioActivaId { get; set; }
        public DateTime fechaHoraActivacion { get; set; }

    }
}