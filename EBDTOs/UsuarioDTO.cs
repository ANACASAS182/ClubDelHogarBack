using System.ComponentModel.DataAnnotations;

namespace EBDTOs
{

    public class UsuarioRegistrarBasicoDTO
    {
        public string email { get; set; } = string.Empty;
        public string password { get; set; } = string.Empty;
        public string codigoInvitacion { get; set; } = string.Empty;
    }


    public class UsuarioBasico
    {
        public int id { get; set; }
        public string nombre { get; set; }
        public string email { get; set; }
    }

    public class UsuarioDTO
    {
        public required string Nombres { get; set; }

        public required string Apellidos { get; set; }

        public required string Email { get; set; }

        public string? Password { get; set; }

        public required string Celular { get; set; }
        public long? CatalogoPaisID { get; set; }
        public long? CatalogoEstadoID { get; set; }
        public string? Ciudad { get; set; }
        public string? EstadoTexto { get; set; }
        public long? FuenteOrigenID { get; set; }
        public long? UsuarioParent { get; set; }
        public string? CodigoInvitacion { get; set; }

        public int? id { get; set; }


    }
}
