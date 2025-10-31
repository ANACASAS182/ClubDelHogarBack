using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EBServices.Interfaces
{
    public interface IBaseService
    {
        string LastError { get; set; }
        List<string> Errors { get; set; }
        bool HasError { get; set; }
    }
}
