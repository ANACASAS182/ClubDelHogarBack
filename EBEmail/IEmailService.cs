using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EBEmail
{
    public interface IEmailService
    {
        Task<EmailResult> SendEmailAsync(string to, string subject, string htmlBody, string FromName);
        Task<EmailResult> SendPasswordRecoveryTemplate(string to, string recoveryURL, string timeExpireHours);
    }
}
