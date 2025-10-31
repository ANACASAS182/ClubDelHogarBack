using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EBDTOs
{
    public class ReferidoCatalogoDTO
    {

        // 🔹 EXISTENTES (no cambiar nombres)
        public long ID { get; set; }
        public string? Nombre { get; set; }
        public string? Email { get; set; }
        public string? Celular { get; set; }
        public string? Producto { get; set; }
        public DateTime Vigencia { get; set; }
        public bool? ProductoVigente { get; set; }
        public string? EstatusRerefencia { get; set; } 
        public string? Embajador { get; set; }
        public string? Empresa { get; set; }
        public string? CodigoCupon { get; set; }

        // NUEVOS (para evitar "Data is Null")
        public int? UsuarioActivaId { get; set; }
        public DateTime? FechaHoraActivacion { get; set; }

    }
}
