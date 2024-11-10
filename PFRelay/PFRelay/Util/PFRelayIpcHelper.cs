using Dalamud.Plugin;
using Dalamud.Plugin.Ipc;
using PFRelay.Delivery;
using PFRelay.Util;
using System;

namespace PFRelay.IPC
{
    public static class PFRelayIpcHelper
    {
        private static ICallGateProvider<string, string, object>? sendCustomMessageProvider;
        private const string SendCustomMessageStr = "PFRelay.SendCustomMessage";

        public static void Initialize(IDalamudPluginInterface pluginInterface, DiscordDMDelivery? discordDMDelivery)
        {
            try
            {
                LoggerHelper.LogDebug("Starting IPC registration in PFRelayIpcHelper.");

                // Check if discordDMDelivery is not null
                if (discordDMDelivery == null)
                {
                    LoggerHelper.LogError("DiscordDMDelivery is null in PFRelayIpcHelper.Initialize.");
                    return;
                }

                // Define the IPC provider and register action
                sendCustomMessageProvider = pluginInterface.GetIpcProvider<string, string, object>(SendCustomMessageStr);
                sendCustomMessageProvider.RegisterAction((title, message) => discordDMDelivery.SendCustomMessage(title, message));

                LoggerHelper.LogDebug($"Registered IPC action: {SendCustomMessageStr}");
            }
            catch (Exception ex)
            {
                LoggerHelper.LogError("Error during IPC registration in PFRelayIpcHelper", ex);
            }
        }

        public static void Dispose()
        {
            try
            {
                sendCustomMessageProvider?.UnregisterAction();
                sendCustomMessageProvider = null;
                LoggerHelper.LogDebug("Unregistered IPC action in PFRelayIpcHelper.");
            }
            catch (Exception ex)
            {
                LoggerHelper.LogError("Error during IPC disposal in PFRelayIpcHelper", ex);
            }
        }
    }
}
