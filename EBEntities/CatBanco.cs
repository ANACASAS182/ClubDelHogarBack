using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations.Schema;

namespace EBEntities
{
    // Ajusta el esquema si tu tabla vive en otro schema (p.ej. "fiscal")
    [Table("CatBanco", Schema = "fiscal")]
    public class CatBanco
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = "";
        public bool Activo { get; set; }
    }

}
