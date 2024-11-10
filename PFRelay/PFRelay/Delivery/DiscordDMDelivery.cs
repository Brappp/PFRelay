using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Flurl.Http;
using PFRelay.Util;
using System.Net.Http;
using Newtonsoft.Json.Linq;

namespace PFRelay.Delivery
{
    public class DiscordDMDelivery : IDelivery, IDisposable
    {
        private static readonly string BOT_SERVICE_URL = "https://relay.wahapp.com";
        private const int DiscordCharacterLimit = 2000; // Discord DM character limit
        private bool disposed = false;

        public bool IsActive => Plugin.Configuration.EnableDiscordBot &&
                                !string.IsNullOrWhiteSpace(Plugin.Configuration.DiscordUserToken) &&
                                !string.IsNullOrWhiteSpace(Plugin.Configuration.UserSecretKey);

        // Handles custom IPC messages received from other plugins
        public void SendCustomMessage(string title, string message)
        {
            if (!IsActive)
            {
                LoggerHelper.LogError("Discord DM bot is inactive or misconfigured; cannot send message.");
                return;
            }

            LoggerHelper.LogDebug($"IPC Received - Title: {title}, Message: {message}");

            // Ensure message length is within the Discord character limit
            if (message.Length > DiscordCharacterLimit)
            {
                LoggerHelper.LogError($"Message exceeds Discord character limit of {DiscordCharacterLimit} characters. Message not sent.");
                return;
            }

            Deliver(title, message); // Send message if within character limit
        }

        public void Deliver(string title, string text)
        {
            if (!IsActive)
            {
                LoggerHelper.LogError("Discord DM bot is not enabled or misconfigured.");
                return;
            }

            Task.Run(() => DeliverAsync(title, text));
        }

        private async void DeliverAsync(string title, string text)
        {
            if (string.IsNullOrWhiteSpace(Plugin.Configuration.DiscordUserToken) ||
                string.IsNullOrWhiteSpace(Plugin.Configuration.UserSecretKey))
            {
                LoggerHelper.LogError("User token or secret key is missing for Discord bot.");
                return;
            }

            string timestamp;
            try
            {
                timestamp = await GetNtpTimeAsync();
            }
            catch (Exception ex)
            {
                LoggerHelper.LogError("Failed to sync with NTP server", ex);
                return;
            }

            var apiUrl = $"{BOT_SERVICE_URL}/send";
            var nonce = Guid.NewGuid().ToString();

            var args = new Dictionary<string, string>
            {
                { "user_token", Plugin.Configuration.DiscordUserToken },
                { "title", title },
                { "text", text },
                { "nonce", nonce },
                { "timestamp", timestamp }
            };

            var messageData = $"{Plugin.Configuration.DiscordUserToken}{title}{text}{nonce}{timestamp}";
            args["hash"] = GenerateHmacHash(messageData, Plugin.Configuration.UserSecretKey);

            LoggerHelper.LogDebug("Attempting to send data to Discord bot service...");
            try
            {
                await apiUrl.PostJsonAsync(args);
                LoggerHelper.LogDebug("Data sent successfully to Discord bot service.");
            }
            catch (FlurlHttpException e)
            {
                LoggerHelper.LogError("Failed to forward message to Discord bot service", e);
                LoggerHelper.LogDebug($"Status: {e.StatusCode}, Response Body: {await e.GetResponseStringAsync()}");
            }
            catch (Exception e)
            {
                LoggerHelper.LogError("Unexpected error during Discord message delivery", e);
            }
        }

        public void SendTestNotification(string title = "Test Notification", string message = "This is a test notification to Discord.")
        {
            if (!IsActive)
            {
                LoggerHelper.LogError("Cannot send test notification. Discord DM bot is inactive or misconfigured.");
                return;
            }

            // Ensure message length is within the Discord character limit
            if (message.Length > DiscordCharacterLimit)
            {
                LoggerHelper.LogError($"Test message exceeds Discord character limit of {DiscordCharacterLimit} characters. Test message not sent.");
                return;
            }

            Deliver(title, message); // Send message if within character limit
            LoggerHelper.LogDebug("Test notification sent to Discord bot.");
        }

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
            try
            {
                return NtpTimeFetcher.GetNtpTime();
            }
            catch (Exception ex)
            {
                LoggerHelper.LogError("Primary NTP query failed, attempting fallback method", ex);
                return await FallbackNtpTimeAsync(new HttpClient());
            }
        }

        private async Task<string> FallbackNtpTimeAsync(HttpClient client)
        {
            HttpResponseMessage response;
            try
            {
                response = await client.GetAsync("http://worldclockapi.com/api/json/utc/now");
            }
            catch (Exception ex)
            {
                LoggerHelper.LogError("Fallback NTP request failed", ex);
                throw new Exception("Both primary and fallback NTP requests failed.", ex);
            }

            if (response.IsSuccessStatusCode)
            {
                var jsonResponse = JObject.Parse(await response.Content.ReadAsStringAsync());
                var utcDateTime = DateTimeOffset.Parse(jsonResponse["currentDateTime"].ToString()).ToUnixTimeSeconds();
                return utcDateTime.ToString();
            }
            else
            {
                throw new Exception("Failed to retrieve time from both NTP servers.");
            }
        }

        public void Dispose()
        {
            if (!disposed)
            {
                disposed = true;
                LoggerHelper.LogDebug("Disposed of DiscordDMDelivery.");
            }
        }
    }
}
