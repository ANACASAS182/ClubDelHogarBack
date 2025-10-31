using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EBDTOs
{
    public class PaginationModelDTO<T>
    {
        public T? Items { get; set; }
        public int Total {  get; set; }
    }
}
