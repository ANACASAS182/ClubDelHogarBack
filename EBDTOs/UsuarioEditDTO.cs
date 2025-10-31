using EBEnums;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EBDTOs
{
    public class UsuarioEditDTO
    {
        public long? ID { get; set; }
        public required string Nombres { get; set; }
        public string? Password { get; set; }
        public required string Apellidos { get; set; }
        public required string Email { get; set; }
        public required string Celular { get; set; }
        public long? CatalogoPaisID { get; set; }
        public long? CatalogoEstadoID { get; set; }
        public string? EstadoTexto { get; set; }
        public string? Ciudad { get; set; }
        public long FuenteOrigenID { get; set; }
        public long? UsuarioParentID { get; set; }
        public string? UsuarioParentNombreCompleto { get; set; }
        public string? CodigoInvitacion { get; set; }
        public long? RolesID { get; set; }
        public RolesEnum? RolesEnum { get; set; }
        public int? GrupoID { get; set; }
        public long? EmpresaID { get; set; }
    }
}
