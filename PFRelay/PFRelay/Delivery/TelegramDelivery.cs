using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using Dalamud.Utility;
using Flurl.Http;
using PFRelay.Util; // Import LoggerHelper

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
            try
            {
                Task.Run(() => DeliverAsync(title, text));
            }
            catch (Exception ex)
            {
                LoggerHelper.LogError("Error starting asynchronous delivery task", ex);
            }
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
                LoggerHelper.LogDebug("Sent Telegram message");
            }
            catch (FlurlHttpException e)
            {
                LoggerHelper.LogError($"Failed to make Telegram request", e);
                LoggerHelper.LogDebug(JsonSerializer.Serialize(args));
            }
            catch (Exception ex)
            {
                LoggerHelper.LogError("Unexpected error during Telegram message delivery", ex);
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

        public void StartListening()
        {
            if (!Plugin.Configuration.EnableTelegramBot)
            {
                LoggerHelper.LogDebug("Telegram bot is disabled in configuration.");
                return;
            }

            LoggerHelper.LogDebug("Starting Telegram bot...");
            try
            {
                Task.Run(async () =>
                {
                    while (Plugin.Configuration.EnableTelegramBot)
                    {
                        try
                        {
                            await FetchAndSendChatId();
                        }
                        catch (Exception ex)
                        {
                            LoggerHelper.LogError("Error during Telegram bot polling", ex);
                        }
                        await Task.Delay(1000);
                    }
                    LoggerHelper.LogDebug("Telegram bot polling stopped.");
                });
            }
            catch (Exception ex)
            {
                LoggerHelper.LogError("Error starting Telegram bot polling", ex);
            }
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
                LoggerHelper.LogError("Failed to retrieve or send chat ID", e);
            }
            catch (Exception ex)
            {
                LoggerHelper.LogError("Unexpected error during FetchAndSendChatId", ex);
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
                LoggerHelper.LogDebug("Sent message to Telegram user");
            }
            catch (FlurlHttpException e)
            {
                LoggerHelper.LogError("Failed to send message", e);
            }
            catch (Exception ex)
            {
                LoggerHelper.LogError("Unexpected error during SendMessage", ex);
            }
        }

        public void StopListening()
        {
            try
            {
                Plugin.Configuration.EnableTelegramBot = false;
                Plugin.Configuration.Save();
                LoggerHelper.LogDebug("Telegram bot polling stopped.");
            }
            catch (Exception ex)
            {
                LoggerHelper.LogError("Error stopping Telegram bot", ex);
            }
        }
    }
}
