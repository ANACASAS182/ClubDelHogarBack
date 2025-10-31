using EBEnums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EBDTOs
{
    public class ReferidoDTO
    {
        public long? ID { get; set; }
        public required string NombreCompleto { get; set; }
        public string? Email { get; set; }
        public required string Celular { get; set; }
        public long? UsuarioID { get; set; }
        public long ProductoID { get; set; }
        public string? UsuarioNombre { get; set; }   
        public string? UsuarioApellido { get; set; }
        public string? UsuarioNombreCompleto { get; set; }
        public long? EmpresaID { get; set; }
        public long? EstatusReferenciaID { get; set; }
        public string? EstatusReferenciaDescripcion { get; set; }
        public EstatusReferenciaEnum EstatusReferenciaEnum { get; set; }
        public string? EmpresaRazonSocial { get; set; }
        public string? ProductoNombre { get; set; }
        public int tipoComision { get; set; }
        public decimal? Comision { get; set; }
        public string ComisionTexto { get; set; }
        public decimal ComisionCantidad { get; set; }
        public decimal ComisionPorcentaje { get; set; }
        public DateTime? FechaVencimiento { get; set; }
        public DateTime? FechaRegistro { get; set; }

    }

    public class IdsRequest
    {
        public List<long> ids { get; set; } = new();
    }

    public class UltimoSeguimientoDTO
    {
        public long ReferidoId { get; set; }
        public string Texto { get; set; } = "";
        public DateTime Fecha { get; set; }
    }

}
