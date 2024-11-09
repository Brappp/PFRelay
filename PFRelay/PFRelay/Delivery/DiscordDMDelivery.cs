using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Flurl.Http;
using Dalamud.Utility;
using System.Net;
using System.Net.Http;
using System.Threading;
using Newtonsoft.Json.Linq;

namespace PFRelay.Delivery
{
    public class DiscordDMDelivery : IDelivery
    {
        private static readonly string BOT_SERVICE_URL = "https://relay.wahapp.com"; // Local testing URL
        private static readonly string NTP_SERVER = "pool.ntp.org"; // Default NTP server

        public bool IsActive => Plugin.Configuration.EnableDiscordBot &&
                                !string.IsNullOrWhiteSpace(Plugin.Configuration.DiscordUserToken) &&
                                !string.IsNullOrWhiteSpace(Plugin.Configuration.UserSecretKey);

        public void Deliver(string title, string text)
        {
            if (!IsActive)
            {
                Service.PluginLog.Error("Discord DM bot is not enabled, or user token/secret key is missing.");
                return;
            }

            Task.Run(() => DeliverAsync(title, text));
        }

        private async void DeliverAsync(string title, string text)
        {
            if (string.IsNullOrWhiteSpace(Plugin.Configuration.DiscordUserToken) ||
                string.IsNullOrWhiteSpace(Plugin.Configuration.UserSecretKey))
            {
                Service.PluginLog.Error("User is not fully configured with the Discord bot. Ensure both the token and secret key are set in the plugin.");
                return;
            }

            // Fetch NTP-synced UTC timestamp
            string timestamp;
            try
            {
                timestamp = await GetNtpTimeAsync();
            }
            catch (Exception ex)
            {
                Service.PluginLog.Error($"Failed to sync with NTP server: {ex.Message}");
                return;
            }

            var apiUrl = $"{BOT_SERVICE_URL}/send";
            var nonce = Guid.NewGuid().ToString(); // Generate a unique nonce for this request

            var args = new Dictionary<string, string>
            {
                { "user_token", Plugin.Configuration.DiscordUserToken },
                { "title", title },
                { "text", text },
                { "nonce", nonce },
                { "timestamp", timestamp }
            };

            // Concatenate message data for hashing
            var messageData = $"{Plugin.Configuration.DiscordUserToken}{title}{text}{nonce}{timestamp}";
            args["hash"] = GenerateHmacHash(messageData, Plugin.Configuration.UserSecretKey);

            Service.PluginLog.Debug("Attempting to send data to Discord bot service...");
            try
            {
                await apiUrl.PostJsonAsync(args);
                Service.PluginLog.Debug("Data sent successfully to Discord bot service.");
            }
            catch (FlurlHttpException e)
            {
                Service.PluginLog.Error($"Failed to forward message to Discord bot service: '{e.Message}'");
                Service.PluginLog.Error($"Status: {e.StatusCode}, Response Body: {await e.GetResponseStringAsync()}");
            }
            catch (Exception e)
            {
                Service.PluginLog.Error($"Unexpected error: '{e.Message}'");
                Service.PluginLog.Error($"{e.StackTrace}");
            }
        }

        // HMAC hash generation with the user-specific secret key
        private string GenerateHmacHash(string message, string userSecretKey)
        {
            byte[] key = Encoding.UTF8.GetBytes(userSecretKey);
            using (var hmac = new HMACSHA256(key))
            {
                byte[] messageBytes = Encoding.UTF8.GetBytes(message);
                byte[] hashBytes = hmac.ComputeHash(messageBytes);
                return Convert.ToBase64String(hashBytes);
            }
        }

        private async Task<string> GetNtpTimeAsync()
        {
            using (var client = new HttpClient())
            {
                client.Timeout = TimeSpan.FromSeconds(5);
                HttpResponseMessage response = await client.GetAsync($"http://worldtimeapi.org/api/timezone/Etc/UTC");

                if (response.IsSuccessStatusCode)
                {
                    var jsonResponse = JObject.Parse(await response.Content.ReadAsStringAsync());
                    var utcDateTime = DateTimeOffset.Parse(jsonResponse["utc_datetime"].ToString()).ToUnixTimeSeconds();
                    return utcDateTime.ToString();
                }
                else
                {
                    throw new Exception("Failed to get NTP time from server.");
                }
            }
        }

        public void StartListening()
        {
            Service.PluginLog.Debug("DiscordDMDelivery is now listening for requests.");
        }

        public void StopListening()
        {
            Service.PluginLog.Debug("DiscordDMDelivery has stopped listening.");
        }
    }
}
