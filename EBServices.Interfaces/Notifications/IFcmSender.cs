using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EBServices.Interfaces.Notifications
{
    public interface IFcmSender
    {
        Task SendToTokenAsync(string token, string title, string body, object? data = null);
    }
}
