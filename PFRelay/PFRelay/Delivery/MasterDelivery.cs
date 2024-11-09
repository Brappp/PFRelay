using System;
using System.Collections.Generic;
using PFRelay.Util; 
namespace PFRelay.Delivery
{
    internal interface IDelivery
    {
        bool IsActive { get; }
        void Deliver(string title, string text);
    }

    public static class MasterDelivery
    {
        private static List<IDelivery> _deliveries = new List<IDelivery>();

        public static void InitializeDeliveries()
        {
            _deliveries.Clear();

            try
            {
                // Add only Discord and Telegram deliveries
                if (Plugin.Configuration.EnableDiscordBot)
                {
                    try
                    {
                        var discordDelivery = new DiscordDMDelivery();
                        if (discordDelivery.IsActive)
                        {
                            _deliveries.Add(discordDelivery);
                            LoggerHelper.LogDebug("DiscordDMDelivery added to active deliveries.");
                        }
                        else
                        {
                            LoggerHelper.LogDebug("DiscordDMDelivery is inactive and was not added.");
                        }
                    }
                    catch (Exception ex)
                    {
                        LoggerHelper.LogError("Error initializing DiscordDMDelivery", ex);
                    }
                }

                if (Plugin.Configuration.EnableTelegramBot)
                {
                    try
                    {
                        var telegramDelivery = new TelegramDelivery();
                        if (telegramDelivery.IsActive)
                        {
                            _deliveries.Add(telegramDelivery);
                            LoggerHelper.LogDebug("TelegramDelivery added to active deliveries.");
                        }
                        else
                        {
                            LoggerHelper.LogDebug("TelegramDelivery is inactive and was not added.");
                        }
                    }
                    catch (Exception ex)
                    {
                        LoggerHelper.LogError("Error initializing TelegramDelivery", ex);
                    }
                }
            }
            catch (Exception ex)
            {
                LoggerHelper.LogError("Error initializing deliveries", ex);
            }
        }

        public static void Deliver(string title, string text)
        {
            try
            {
                InitializeDeliveries(); 

                foreach (var delivery in _deliveries)
                {
                    try
                    {
                        if (delivery.IsActive)
                        {
                            LoggerHelper.LogDebug($"Sending '{title}' to delivery type: {delivery.GetType().Name}");
                            delivery.Deliver(title, text);
                        }
                        else
                        {
                            LoggerHelper.LogDebug($"Delivery type {delivery.GetType().Name} is inactive and will not send.");
                        }
                    }
                    catch (Exception ex)
                    {
                        LoggerHelper.LogError($"Error during delivery with {delivery.GetType().Name}", ex);
                    }
                }
            }
            catch (Exception ex)
            {
                LoggerHelper.LogError("Error in Deliver method", ex);
            }
        }
    }
}
