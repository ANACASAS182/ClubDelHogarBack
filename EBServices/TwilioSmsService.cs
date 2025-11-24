using Microsoft.Extensions.Configuration;
using Twilio;
using Twilio.Rest.Api.V2010.Account;
using Twilio.Types;

namespace EmbassyBusinessBack.Services
{
    public class TwilioSmsService
    {
        private readonly string _sid;
        private readonly string _token;
        private readonly string _from;

        public TwilioSmsService(IConfiguration config)
        {
            _sid = config["Twilio:AccountSid"]!;
            _token = config["Twilio:AuthToken"]!;
            _from = config["Twilio:FromNumber"]!;
        }

        public async Task<bool> EnviarSmsAsync(string telefono, string mensaje)
        {
            TwilioClient.Init(_sid, _token);

            var result = await MessageResource.CreateAsync(
                to: new PhoneNumber("+52" + telefono),
                from: new PhoneNumber(_from),
                body: mensaje
            );

            return result.ErrorCode == null;
        }
    }
}
