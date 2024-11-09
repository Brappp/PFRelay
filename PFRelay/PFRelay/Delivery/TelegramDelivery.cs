using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using Dalamud.Utility;
using Flurl.Http;

namespace PFRelay.Delivery
{
    public class TelegramDelivery : IDelivery
    {
        private static readonly string TELEGRAM_API_URL = "https://api.telegram.org/bot";
        private int lastUpdateId = 0;

        public bool IsActive => !Plugin.Configuration.TelegramBotToken.IsNullOrWhitespace() &&
                                !Plugin.Configuration.TelegramChatId.IsNullOrWhitespace();

        public void Deliver(string title, string text)
        {
            Task.Run(() => DeliverAsync(title, text));
        }

        private async void DeliverAsync(string title, string text)
        {
            var apiUrl = $"{TELEGRAM_API_URL}{Plugin.Configuration.TelegramBotToken}/sendMessage";
            var args = new Dictionary<string, string>
            {
                { "chat_id", Plugin.Configuration.TelegramChatId },
                { "text", $"{title}\n{text}" },
                { "parse_mode", "Markdown" }
            };

            try
            {
                await apiUrl.PostJsonAsync(args);
                Service.PluginLog.Debug("Sent Telegram message");
            }
            catch (FlurlHttpException e)
            {
                Service.PluginLog.Error($"Failed to make Telegram request: '{e.Message}'");
                Service.PluginLog.Error($"{e.StackTrace}");
                Service.PluginLog.Debug(JsonSerializer.Serialize(args));
            }
        }

        // New method to send a test notification
        public void SendTestNotification(string title, string message)
        {
            Deliver(title, message);
        }

        public void StartListening()
        {
            if (!Plugin.Configuration.EnableTelegramBot)
            {
                Service.PluginLog.Debug("Telegram bot is disabled in configuration.");
                return;
            }

            Service.PluginLog.Debug("Starting Telegram bot...");
            Task.Run(async () =>
            {
                while (Plugin.Configuration.EnableTelegramBot)
                {
                    await FetchAndSendChatId();
                    await Task.Delay(1000);
                }
                Service.PluginLog.Debug("Telegram bot polling stopped.");
            });
        }

        private async Task FetchAndSendChatId()
        {
            var getUpdatesUrl = $"{TELEGRAM_API_URL}{Plugin.Configuration.TelegramBotToken}/getUpdates?offset={lastUpdateId + 1}";

            try
            {
                var response = await getUpdatesUrl.GetJsonAsync<JsonDocument>();

                foreach (var result in response.RootElement.GetProperty("result").EnumerateArray())
                {
                    lastUpdateId = result.GetProperty("update_id").GetInt32();

                    var message = result.GetProperty("message").GetProperty("text").GetString();
                    var chatId = result.GetProperty("message").GetProperty("chat").GetProperty("id").ToString();

                    if (message == "/start")
                    {
                        await SendMessage(chatId, "Welcome! This bot is active and ready to send notifications.");
                    }
                    else if (message == "/get_chat_id")
                    {
                        await SendMessage(chatId, $"Your Chat ID is: {chatId}");
                    }
                }
            }
            catch (FlurlHttpException e)
            {
                Service.PluginLog.Error($"Failed to retrieve or send chat ID: '{e.Message}'");
                Service.PluginLog.Error($"{e.StackTrace}");
            }
        }

        private async Task SendMessage(string chatId, string text)
        {
            var apiUrl = $"{TELEGRAM_API_URL}{Plugin.Configuration.TelegramBotToken}/sendMessage";
            var args = new Dictionary<string, string>
            {
                { "chat_id", chatId },
                { "text", text }
            };

            try
            {
                await apiUrl.PostJsonAsync(args);
                Service.PluginLog.Debug("Sent message to Telegram user");
            }
            catch (FlurlHttpException e)
            {
                Service.PluginLog.Error($"Failed to send message: '{e.Message}'");
                Service.PluginLog.Error($"{e.StackTrace}");
            }
        }

        public void StopListening()
        {
            Plugin.Configuration.EnableTelegramBot = false;
            Plugin.Configuration.Save();
            Service.PluginLog.Debug("Telegram bot polling stopped.");
        }
    }
}
