using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EBDTOs
{
    public class BancoUsuarioDTO
    {
        public long Id { get; set; }
        public long UsuarioId { get; set; }
        public string NombreBanco { get; set; } = "";
        public string NombreTitular { get; set; } = "";
        public string NumeroCuenta { get; set; } = "";
        public int? CatBancoId { get; set; }
        public string? BancoOtro { get; set; }
        /// <summary>0 = Tarjeta, 1 = CLABE (o lo que manejes)</summary>
        public int TipoCuenta { get; set; }
        public DateTime? FechaCreacion { get; set; }
        public bool Eliminado { get; set; }
    }
}
