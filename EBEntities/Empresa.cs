using EBEntities.Common;
using System.ComponentModel.DataAnnotations.Schema;

namespace EBEntities
{
    public class Empresa : Base, ISoftDeletable
    {
        public required string RFC {  get; set; }
        public required string RazonSocial { get; set; }
        public required string NombreComercial { get; set; }
        public int? Giro { get; set; }
        public int? Grupo { get; set; }
        public string? Descripcion { get; set; }    
        public string? LogotipoPath {  get; set; }  
        public string? LogotipoBase64 { get; set; }

        public int? embajadorId{ get; set; }

        [NotMapped]
        public string embajadorNombre { get; set; } = string.Empty;

    }
}
