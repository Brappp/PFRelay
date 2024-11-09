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
        private bool disposed = false;

        public bool IsActive => Plugin.Configuration.EnableDiscordBot &&
                                !string.IsNullOrWhiteSpace(Plugin.Configuration.DiscordUserToken) &&
                                !string.IsNullOrWhiteSpace(Plugin.Configuration.UserSecretKey);

        public void Deliver(string title, string text)
        {
            if (!IsActive)
            {
                LoggerHelper.LogError("Discord DM bot is not enabled, or user token/secret key is missing.", new InvalidOperationException("Discord bot inactive or misconfigured."));
                return;
            }

            Task.Run(() => DeliverAsync(title, text));
        }

        private async void DeliverAsync(string title, string text)
        {
            if (string.IsNullOrWhiteSpace(Plugin.Configuration.DiscordUserToken) ||
                string.IsNullOrWhiteSpace(Plugin.Configuration.UserSecretKey))
            {
                LoggerHelper.LogError("User is not fully configured with the Discord bot. Ensure both the token and secret key are set in the plugin.", new ArgumentException("Invalid configuration"));
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

        public void SendTestNotification(string title, string message)
        {
            try
            {
                Deliver(title, message);
            }
            catch (Exception ex)
            {
                LoggerHelper.LogError("Error sending test notification", ex);
            }
        }

        private string GenerateHmacHash(string message, string userSecretKey)
        {
            try
            {
                byte[] key = Encoding.UTF8.GetBytes(userSecretKey);
                using (var hmac = new HMACSHA256(key))
                {
                    byte[] messageBytes = Encoding.UTF8.GetBytes(message);
                    byte[] hashBytes = hmac.ComputeHash(messageBytes);
                    return Convert.ToBase64String(hashBytes);
                }
            }
            catch (Exception ex)
            {
                LoggerHelper.LogError("Error generating HMAC hash", ex);
                throw;
            }
        }

        private async Task<string> GetNtpTimeAsync()
        {
            try
            {
                // Attempt direct NTP query
                return NtpTimeFetcher.GetNtpTime();
            }
            catch (Exception ex)
            {
                LoggerHelper.LogError("Primary NTP query failed, attempting fallback method", ex);
                return await FallbackNtpTimeAsync(new HttpClient());
            }
        }

        // Fallback method for HTTP-based time fetch if direct NTP fails
        private async Task<string> FallbackNtpTimeAsync(HttpClient client)
        {
            HttpResponseMessage response;
            try
            {
                response = await client.GetAsync("http://worldclockapi.com/api/json/utc/now");
            }
            catch (Exception ex)
            {
                LoggerHelper.LogError("Fallback NTP request to worldclockapi.com failed", ex);
                throw new Exception("Both primary and fallback NTP requests failed.", ex);
            }

            if (response.IsSuccessStatusCode)
            {
                try
                {
                    var jsonResponse = JObject.Parse(await response.Content.ReadAsStringAsync());
                    var utcDateTime = DateTimeOffset.Parse(jsonResponse["currentDateTime"].ToString()).ToUnixTimeSeconds();
                    return utcDateTime.ToString(); // Ensuring the returned time is in UTC
                }
                catch (Exception ex)
                {
                    LoggerHelper.LogError("Error parsing response from fallback NTP server", ex);
                    throw;
                }
            }
            else
            {
                LoggerHelper.LogError($"Fallback NTP server response failed with status code: {response.StatusCode}");
                throw new Exception("Failed to retrieve time from both NTP servers.");
            }
        }

        public void StartListening()
        {
            try
            {
                LoggerHelper.LogDebug("DiscordDMDelivery is now listening for requests.");
            }
            catch (Exception ex)
            {
                LoggerHelper.LogError("Error starting listening service", ex);
            }
        }

        public void StopListening()
        {
            try
            {
                LoggerHelper.LogDebug("DiscordDMDelivery has stopped listening.");
            }
            catch (Exception ex)
            {
                LoggerHelper.LogError("Error stopping listening service", ex);
            }
        }

        public void Dispose()
        {
            try
            {
                if (!disposed)
                {
                    StopListening();
                    disposed = true;
                }
            }
            catch (Exception ex)
            {
                LoggerHelper.LogError("Error during disposal", ex);
            }
        }
    }
}
