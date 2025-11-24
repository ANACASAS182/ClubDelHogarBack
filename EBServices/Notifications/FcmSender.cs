using System.Net.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using EBServices.Interfaces.Notifications;

namespace EBServices.Notifications
{
    public class FcmSender : IFcmSender
    {
        private readonly HttpClient _http;
        private readonly string _serverKey;

        public FcmSender(IHttpClientFactory f, IConfiguration cfg)
        {
            _http = f.CreateClient();
            _serverKey = cfg["Fcm:ServerKey"]
                ?? throw new InvalidOperationException("Fcm:ServerKey not configured");
        }

        public async Task SendToTokenAsync(string token, string title, string body, object? data = null)
        {
            var payload = new
            {
                to = token,
                notification = new { title, body, sound = "default" },
                data = data ?? new { }
            };

            using var req = new HttpRequestMessage(HttpMethod.Post, "https://fcm.googleapis.com/fcm/send");
            req.Headers.TryAddWithoutValidation("Authorization", $"key={_serverKey}");
            req.Content = new StringContent(
                System.Text.Json.JsonSerializer.Serialize(payload),
                System.Text.Encoding.UTF8,
                "application/json"
            );

            var res = await _http.SendAsync(req);
            res.EnsureSuccessStatusCode();
        }
    }
}