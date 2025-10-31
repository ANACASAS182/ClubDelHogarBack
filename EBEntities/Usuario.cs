using EBEntities.Common;

namespace EBEntities
{
    public class Usuario : Base, ISoftDeletable
    {
        public long ID { get; set; }

        // SIEMPRE obligatorios
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;

        // Opcionales al inicio (onboarding los llenará)
        public string? Nombres { get; set; }
        public string? Apellidos { get; set; }
        public string? Celular { get; set; }

        public long? CatalogoPaisID { get; set; }
        public long? CatalogoEstadoID { get; set; }
        public string? EstadoTexto { get; set; }
        public string? Ciudad { get; set; }

        public long FuenteOrigenID { get; set; }   // si siempre asignas 2, déjalo no-nullable
        public CatalogoPais? CatalogoPais { get; set; }
        public CatalogoEstado? CatalogoEstado { get; set; }
        public FuenteOrigen? FuenteOrigen { get; set; }

        public long? UsuarioParent { get; set; }
        public string? CodigoInvitacion { get; set; }

        public long RolesID { get; set; }
        public Roles? Roles { get; set; }

        public int? GrupoID { get; set; }
        public Grupo? Grupo { get; set; }

        public bool? mostrarOnboarding { get; set; } // null/true/false (front ya checa truthy)
    }
}
