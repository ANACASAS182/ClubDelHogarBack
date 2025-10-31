using EBEnums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EBDTOs
{
    public class EstatusReferidoDTO
    {
        public long ID { get; set; }                 // ID del Referido
        public int EstatusReferenciaEnum { get; set; } // 1,2,3

        // opcionales (útiles si guardas seguimiento)
        public string? Comentario { get; set; }
        public long? UsuarioID { get; set; }
    }
}
