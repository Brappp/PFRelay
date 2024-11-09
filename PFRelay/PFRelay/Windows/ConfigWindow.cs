using System;
using System.Numerics;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using ImGuiNET;
using PFRelay.Delivery;
using PFRelay.Util;

namespace PFRelay.Windows
{
    public class ConfigWindow : Window, IDisposable
    {
        private readonly Configuration Configuration;
        private readonly Plugin plugin;
        private readonly TimedBool notifSentMessageTimerDiscord = new(3.0f);
        private readonly TimedBool notifSentMessageTimerTelegram = new(3.0f);
        private bool showSetupGuidePopup = false;

        public ConfigWindow(Plugin plugin) : base(
            "PFRelay Configuration",
            ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoScrollbar |
            ImGuiWindowFlags.NoScrollWithMouse | ImGuiWindowFlags.AlwaysAutoResize)
        {
            this.plugin = plugin;
            Configuration = Plugin.Configuration;
        }

        public void Dispose() { }

        private void DrawGeneralSettings()
        {
            var enableForDutyPops = Configuration.EnableForDutyPops;
            if (ImGui.Checkbox("Enable notifications for duty pops", ref enableForDutyPops))
            {
                Configuration.EnableForDutyPops = enableForDutyPops;
                Configuration.Save();
            }

            var ignoreAfkStatus = Configuration.IgnoreAfkStatus;
            if (ImGui.Checkbox("Ignore AFK status and always notify", ref ignoreAfkStatus))
            {
                Configuration.IgnoreAfkStatus = ignoreAfkStatus;
                Configuration.Save();
            }
        }

        private void DrawDiscordDMConfig()
        {
            ImGui.Text("Discord DM Bot Settings");

            var enableDiscordBot = Configuration.EnableDiscordBot;
            if (ImGui.Checkbox("Enable Discord DM Bot", ref enableDiscordBot))
            {
                Configuration.EnableDiscordBot = enableDiscordBot;
                Configuration.Save();

                if (enableDiscordBot)
                {
                    LoggerHelper.LogDebug("Starting Discord DM bot...");
                    plugin.StartDiscordBot();
                }
                else
                {
                    LoggerHelper.LogDebug("Stopping Discord DM bot...");
                    plugin.StopDiscordBot();
                }
            }

            ImGui.TextWrapped("To set up the Discord DM bot, type `hello` in a direct message to the bot in Discord. The bot will respond with interactive buttons to register, show, or remove your credentials.");

            var userToken = Configuration.DiscordUserToken;
            if (ImGui.InputText("User Token", ref userToken, 2048u))
            {
                Configuration.DiscordUserToken = userToken;
                Configuration.Save();
            }
            ImGui.TextWrapped("Paste the Token provided by the bot here.");

            var userSecretKey = Configuration.UserSecretKey;
            if (ImGui.InputText("User Secret Key", ref userSecretKey, 2048u))
            {
                Configuration.UserSecretKey = userSecretKey;
                Configuration.Save();
            }
            ImGui.TextWrapped("Paste the Secret Key provided by the bot here.");

            ImGui.TextColored(new Vector4(0.9f, 0.9f, 0.1f, 1.0f), "Remember to save your configuration after entering the Token and Secret Key.");

            if (ImGui.Button("Send test Discord DM notification"))
            {
                notifSentMessageTimerDiscord.Start();
                plugin.DiscordDMDelivery?.SendTestNotification("Test notification", "If you received this, the Discord DM bot is configured correctly.");
            }

            if (notifSentMessageTimerDiscord.Value)
            {
                ImGui.SameLine();
                ImGui.Text("Discord notification sent!");
            }
        }

        private void DrawTelegramConfig()
        {
            ImGui.Text("Telegram Bot Settings");

            var enableTelegramBot = Configuration.EnableTelegramBot;
            if (ImGui.Checkbox("Enable Telegram Bot", ref enableTelegramBot))
            {
                Configuration.EnableTelegramBot = enableTelegramBot;
                Configuration.Save();

                if (enableTelegramBot)
                {
                    LoggerHelper.LogDebug("Starting Telegram bot...");
                    plugin.StartTelegramBot();
                }
                else
                {
                    LoggerHelper.LogDebug("Stopping Telegram bot...");
                    plugin.StopTelegramBot();
                }
            }

            var botToken = Configuration.TelegramBotToken;
            if (ImGui.InputText("Bot Token", ref botToken, 2048u))
            {
                Configuration.TelegramBotToken = botToken;
                Configuration.Save();
            }
            ImGui.TextWrapped("Enter your Telegram bot token.");

            var chatId = Configuration.TelegramChatId;
            if (ImGui.InputText("Chat ID", ref chatId, 2048u))
            {
                Configuration.TelegramChatId = chatId;
                Configuration.Save();
            }
            ImGui.TextWrapped("Enter the chat ID where notifications should be sent.");

            if (ImGui.Button("Telegram Setup Guide"))
            {
                showSetupGuidePopup = true;
            }

            ShowTelegramSetupGuide();

            if (ImGui.Button("Send test Telegram notification"))
            {
                notifSentMessageTimerTelegram.Start();
                plugin.TelegramDelivery?.SendTestNotification("Test notification", "If you received this, the Telegram bot is configured correctly.");
            }

            if (notifSentMessageTimerTelegram.Value)
            {
                ImGui.SameLine();
                ImGui.Text("Telegram notification sent!");
            }
        }

        private void ShowTelegramSetupGuide()
        {
            if (showSetupGuidePopup)
            {
                ImGui.OpenPopup("Telegram Setup Guide");
            }

            if (ImGui.BeginPopupModal("Telegram Setup Guide", ref showSetupGuidePopup, ImGuiWindowFlags.AlwaysAutoResize))
            {
                ImGui.TextWrapped("Follow these steps to set up Telegram notifications:");
                ImGui.BulletText("1. Open Telegram and search for the bot named @BotFather.");
                ImGui.TextWrapped("BotFather is an official bot by Telegram to help you create and manage your own bots.");

                ImGui.BulletText("2. Start a chat with BotFather and send the command: /newbot.");
                ImGui.TextWrapped("You will be prompted to enter a name and a username for your bot.");

                ImGui.BulletText("3. Once created, BotFather will provide you with a bot token.");
                ImGui.TextWrapped("Copy this token; you'll need it in the plugin's configuration under 'Bot Token'.");

                ImGui.BulletText("4. Add your bot to the group where you want to receive notifications.");
                ImGui.TextWrapped("Make sure the bot has permission to read messages in the group.");

                ImGui.BulletText("5. Retrieve your group chat ID.");
                ImGui.TextWrapped("To get the chat ID, send any message in the group with the bot added, then click 'Fetch Group Chat ID' in the plugin's configuration.");

                ImGui.BulletText("6. Once you have the Chat ID, paste it into the 'Chat ID' field in the configuration.");

                ImGui.Separator();
                ImGui.TextWrapped("Need help? Make sure your bot is added to the group and that you have the correct bot token.");
                ImGui.TextWrapped("If you encounter any errors, check the plugin logs for more information.");

                if (ImGui.Button("Close"))
                {
                    showSetupGuidePopup = false;
                    ImGui.CloseCurrentPopup();
                }

                ImGui.EndPopup();
            }
        }

        public override void Draw()
        {
            using (var tabBar = ImRaii.TabBar("SettingsTabs"))
            {
                if (tabBar)
                {
                    using (var generalTab = ImRaii.TabItem("Settings"))
                    {
                        if (generalTab) DrawGeneralSettings();
                    }
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
