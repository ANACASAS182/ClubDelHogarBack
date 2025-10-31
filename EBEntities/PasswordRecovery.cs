using EBEntities.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EBEntities
{
    public class PasswordRecovery : Base, ISoftDeletable
    {
        public long UsuarioID { get; set; }
        public required string Token { get; set; }   
        public DateTime FechaVencimiento { get; set; }
        public bool Usado {  get; set; }    

        public Usuario? Usuario { get; set; }

    }
}
