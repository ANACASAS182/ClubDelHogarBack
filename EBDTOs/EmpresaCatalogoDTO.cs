using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EBDTOs
{
    public class EmpresaCatalogoDTO
    {
        public long ID {  get; set; }
        public  string? RFC { get; set; }
        public  string? RazonSocial { get; set; }
        public  string? NombreComercial { get; set; }
        public string? Descripcion { get; set; }
        public List<GrupoDTO>? Grupos { get; set; } 
    }
}
