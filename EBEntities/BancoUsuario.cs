using EBEntities.Common;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EBEntities
{
    public class BancoUsuario : Base, ISoftDeletable
    {
        // AHORA SÍ MAPEADO (ya NO [NotMapped])
        [Key]
        [Column("ID")]                   // columna real en SQL
        public long Id
        {
            get => ID;                   // usa el backing de Base
            set => ID = value;
        }

        [Column("UsuarioID")]
        public long UsuarioId { get; set; }

        public string NombreBanco { get; set; } = "";
        public string NombreTitular { get; set; } = "";
        public string NumeroCuenta { get; set; } = "";
        public int? CatBancoId { get; set; }
        public string? BancoOtro { get; set; }
        /// <summary>0 = Tarjeta, 1 = CLABE</summary>
        public byte TipoCuenta { get; set; }
    }
}
