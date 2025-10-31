// using necesarios:
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace EBEntities.Common
{
    public class Base
    {
        [Key]
        [Column("ID")]
        [JsonIgnore]               // 👈 evita la colisión con BancoUsuario.Id
        public long ID { get; set; }

        public DateTime? FechaCreacion { get; set; }
        public bool Eliminado { get; set; }
        public DateTime? FechaEliminacion { get; set; }
    }
}
