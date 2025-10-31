using EBEntities.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EBEntities
{
    public class Referido : Base, ISoftDeletable
    {
        public required string NombreCompleto {  get; set; }    
        public required string Email { get; set; }
        public required string Celular {  get; set; }
        public required long UsuarioID { get; set; }    
        public required long ProductoID { get; set; }
        public required long? EstatusReferenciaID { get; set; }
        public Usuario? Usuario { get; set; }
        public Producto? Producto { get; set; }
        public EstatusReferencia? EstatusReferencia { get; set; }


    }
}
