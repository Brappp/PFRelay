using System;
using System.Numerics;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using ImGuiNET;
using PFRelay.Delivery;

namespace PFRelay.Windows
{
    public class ConfigWindow : Window, IDisposable
    {
        private readonly Configuration Configuration;
        private readonly Plugin plugin;

        public ConfigWindow(Plugin plugin) : base(
            "PFRelay Configuration",
            ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoScrollbar |
            ImGuiWindowFlags.NoScrollWithMouse | ImGuiWindowFlags.AlwaysAutoResize)
        {
            this.plugin = plugin;
            Configuration = Plugin.Configuration;
        }

        public void Dispose() { }

        // Discord DM Bot Configuration UI
        private void DrawDiscordDMConfig()
        {
            var enableDiscordBot = Configuration.EnableDiscordBot;
            if (ImGui.Checkbox("Enable Discord DM Bot", ref enableDiscordBot))
            {
                Configuration.EnableDiscordBot = enableDiscordBot;
                Configuration.Save();

                if (enableDiscordBot)
                {
                    Service.PluginLog.Debug("Starting Discord DM bot...");
                    if (plugin.DiscordDMDelivery == null)
                        plugin.DiscordDMDelivery = new DiscordDMDelivery();
                    plugin.DiscordDMDelivery.StartListening();
                }
                else
                {
                    Service.PluginLog.Debug("Stopping Discord DM bot...");
                    plugin.DiscordDMDelivery?.StopListening();
                }
            }

            ImGui.TextWrapped("To set up the Discord DM bot, type `hello` in a direct message to the bot in Discord. The bot will respond with interactive buttons to register, show, or remove your credentials.");

            var userToken = Configuration.DiscordUserToken;
            if (ImGui.InputText("User Token", ref userToken, 2048u))
                Configuration.DiscordUserToken = userToken;
            ImGui.TextWrapped("Paste the Token provided by the bot here.");

            var userSecretKey = Configuration.UserSecretKey;
            if (ImGui.InputText("User Secret Key", ref userSecretKey, 2048u))
                Configuration.UserSecretKey = userSecretKey;
            ImGui.TextWrapped("Paste the Secret Key provided by the bot here.");

            ImGui.TextColored(new Vector4(0.9f, 0.9f, 0.1f, 1.0f), "Remember to save your configuration after entering the Token and Secret Key.");

            Configuration.Save();
        }

        // Telegram Bot Configuration UI
        private void DrawTelegramConfig()
        {
            var enableTelegramBot = Configuration.EnableTelegramBot;
            if (ImGui.Checkbox("Enable Telegram Bot", ref enableTelegramBot))
            {
                Configuration.EnableTelegramBot = enableTelegramBot;
                Configuration.Save();

                if (enableTelegramBot)
                {
                    Service.PluginLog.Debug("Starting Telegram bot...");
                    if (plugin.TelegramDelivery == null)
                        plugin.TelegramDelivery = new TelegramDelivery();
                    plugin.TelegramDelivery.StartListening();
                }
                else
                {
                    Service.PluginLog.Debug("Stopping Telegram bot...");
                    plugin.TelegramDelivery?.StopListening();
                }
            }

            var botToken = Configuration.TelegramBotToken;
            if (ImGui.InputText("Bot Token", ref botToken, 2048u))
                Configuration.TelegramBotToken = botToken;
            ImGui.TextWrapped("Enter your Telegram bot token.");

            var chatId = Configuration.TelegramChatId;
            if (ImGui.InputText("Chat ID", ref chatId, 2048u))
                Configuration.TelegramChatId = chatId;
            ImGui.TextWrapped("Enter the chat ID where notifications should be sent.");

            Configuration.Save();
        }

        public override void Draw()
        {
            using (var tabBar = ImRaii.TabBar("Services"))
            {
                if (tabBar)
                {
                    using (var discordDMTab = ImRaii.TabItem("Discord DM"))
                    {
                        if (discordDMTab) DrawDiscordDMConfig();
                    }
                    using (var telegramTab = ImRaii.TabItem("Telegram"))
                    {
                        if (telegramTab) DrawTelegramConfig();
                    }
                }
            }

            ImGui.NewLine();

            if (ImGui.Button("Save and close"))
            {
                Configuration.Save();
                IsOpen = false;
            }
        }
    }
}
