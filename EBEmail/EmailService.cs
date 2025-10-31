using MailKit.Security;
using MimeKit;
using MailKit.Net.Smtp;
using Microsoft.Extensions.Options;
using static System.Runtime.InteropServices.JavaScript.JSType;


namespace EBEmail
{
    public class EmailService: IEmailService
    {
        private readonly EmailSettings _settings;

        public EmailService(IOptions<EmailSettings> settings)
        {
            _settings = settings.Value;
        }


        public async Task<EmailResult> SendEmailAsync(string to, string subject, string htmlBody, string FromName)
        {
            var result = new EmailResult();

            try
            {
                var email = new MimeMessage();

                if (FromName != "")
                {
                    email.From.Add(new MailboxAddress(FromName, _settings.SenderEmail));
                }
                else { 
                
                    email.From.Add(MailboxAddress.Parse(_settings.SenderEmail));
                }


                email.To.Add(MailboxAddress.Parse(to));
                email.Subject = subject;

                var builder = new BodyBuilder
                {
                    HtmlBody = htmlBody
                };
                email.Body = builder.ToMessageBody();

                using var smtp = new SmtpClient();
                await smtp.ConnectAsync(_settings.SmtpServer, _settings.Port);
                await smtp.AuthenticateAsync(_settings.Username, _settings.Password);
                await smtp.SendAsync(email);
                await smtp.DisconnectAsync(true);
                result.Success = true;
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.ErrorMessage = ex.Message;
            }
            return result;


        }

       


        public async Task<EmailResult> SendPasswordRecoveryTemplate(string to, string recoveryURL, string timeExpireHours)
        {

            var templatePath = Path.Combine(Directory.GetCurrentDirectory(), "Templates", "PasswordRecoveryEmailTemplate.html");
            var htmlBody = await File.ReadAllTextAsync(templatePath);

            htmlBody = htmlBody.Replace("{{RecoveryURL}}", recoveryURL);
            htmlBody = htmlBody.Replace("{{ExpireHours}}", timeExpireHours);


            var result = await SendEmailAsync(to, "Cambio de contraseña", htmlBody, "");

            return result;
        }



    }
}
