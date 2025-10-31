using EBEntities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EBEntities
{
    public class ProductoComision
    {
        public long ID { get; set; }
        public long ProductoID { get; set; }
        public int TipoComision { get; set; }
        public decimal nivel_1 { get; set; }
        public decimal nivel_2 { get; set; }
        public decimal nivel_3 { get; set; }
        public decimal nivel_4 { get; set; }
        public decimal nivel_master { get; set; }
        public decimal nivel_base { get; set; }

    }
}
