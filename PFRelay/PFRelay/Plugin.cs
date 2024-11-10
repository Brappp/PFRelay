using System;
using Dalamud.Game.Command;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using PFRelay.Delivery;
using PFRelay.Impl;
using PFRelay.IPC;
using PFRelay.Util;
using PFRelay.Windows;

namespace PFRelay
{
    public sealed class Plugin : IDalamudPlugin
    {
        public string Name => "PFRelay";
        private const string CommandName = "/pfrelay";

        public static Configuration Configuration { get; private set; }
        public TelegramDelivery? TelegramDelivery { get; set; }
        public DiscordDMDelivery? DiscordDMDelivery { get; set; }

        public WindowSystem WindowSystem = new("PFRelay");
        private ConfigWindow ConfigWindow { get; init; }

        public Plugin(IDalamudPluginInterface pluginInterface, ICommandManager commandManager)
        {
            try
            {
                // Initialize Dalamud services first
                pluginInterface.Create<Service>();

                // Load configuration explicitly
                Configuration = Configuration.Load();
                Configuration.Initialize(Service.PluginInterface);

                // Logging after services are ready
                LoggerHelper.LogDebug("PFRelay Plugin constructor called.");

                // Set up UI and commands
                ConfigWindow = new ConfigWindow(this);
                WindowSystem.AddWindow(ConfigWindow);

                Service.CommandManager.AddHandler(CommandName, new CommandInfo(OnCommand)
                {
                    HelpMessage = "Opens the configuration window."
                });

                Service.PluginInterface.UiBuilder.Draw += DrawUI;
                Service.PluginInterface.UiBuilder.OpenConfigUi += DrawConfigUI;
                Service.PluginInterface.UiBuilder.OpenMainUi += OpenMainUi;

                // Temporarily ensure DiscordBot is enabled for testing
                Configuration.EnableDiscordBot = true;

                if (Configuration.EnableDiscordBot)
                {
                    LoggerHelper.LogDebug("Starting Discord bot in PFRelay Plugin constructor.");
                    StartDiscordBot();

                    if (DiscordDMDelivery != null)
                    {
                        LoggerHelper.LogDebug("Initializing IPC in PFRelay Plugin constructor.");
                        PFRelayIpcHelper.Initialize(Service.PluginInterface, DiscordDMDelivery);
                    }
                    else
                    {
                        LoggerHelper.LogError("DiscordDMDelivery is null in Plugin constructor.");
                    }
                }

                if (Configuration.EnableTelegramBot) StartTelegramBot();

                CrossWorldPartyListSystem.Start();
                PartyListener.On();
                DutyListener.On();

                LoggerHelper.LogDebug("PFRelay Plugin has been successfully initialized.");
            }
            catch (Exception ex)
            {
                LoggerHelper.LogError("Error during PFRelay Plugin initialization", ex);
                throw;
            }
        }

        public void Dispose()
        {
            try
            {
                Service.PluginInterface.UiBuilder.OpenMainUi -= OpenMainUi;
                Service.PluginInterface.UiBuilder.Draw -= DrawUI;
                Service.PluginInterface.UiBuilder.OpenConfigUi -= DrawConfigUI;

                WindowSystem.RemoveAllWindows();
                ConfigWindow.Dispose();

                CrossWorldPartyListSystem.Stop();
                PartyListener.Off();
                DutyListener.Off();

                Service.CommandManager.RemoveHandler(CommandName);

                PFRelayIpcHelper.Dispose();
                DiscordDMDelivery?.Dispose();
                if (Configuration.EnableDiscordBot)
                {
                    StopDiscordBot();
                    Configuration.EnableDiscordBot = false;
                    Configuration.Save();
                }

                if (Configuration.EnableTelegramBot)
                {
                    StopTelegramBot();
                    Configuration.EnableTelegramBot = false;
                    Configuration.Save();
                }

                LoggerHelper.LogDebug("PFRelay Plugin has been successfully disposed.");
            }
            catch (Exception ex)
            {
                LoggerHelper.LogError("Error during PFRelay Plugin disposal", ex);
            }
        }

        public void OpenMainUi() => ConfigWindow.IsOpen = true;

        private void OnCommand(string command, string args)
        {
            if (args == "testipc")
            {
                DiscordDMDelivery?.SendCustomMessage("Test Title", "Test Message from PFRelay Command");
                LoggerHelper.LogError("Sent test IPC message to Discord via PFRelay.");
            }
            else
            {
                ConfigWindow.IsOpen = true;
            }
        }

        private void DrawUI() => WindowSystem.Draw();

        public void DrawConfigUI() => ConfigWindow.IsOpen = true;

        public void StartDiscordBot()
        {
            try
            {
                if (DiscordDMDelivery == null)
                {
                    LoggerHelper.LogDebug("Creating new DiscordDMDelivery instance.");
                    DiscordDMDelivery = new DiscordDMDelivery();
                }
                LoggerHelper.LogDebug("Discord DM bot setup completed.");
            }
            catch (Exception ex)
            {
                LoggerHelper.LogError("Error starting Discord bot", ex);
            }
        }

        public void StopDiscordBot()
        {
            try
            {
                if (DiscordDMDelivery != null)
                {
                    LoggerHelper.LogDebug("Discord DM bot stopped.");
                }
            }
            catch (Exception ex)
            {
                LoggerHelper.LogError("Error stopping Discord bot", ex);
            }
        }

        public void StartTelegramBot()
        {
            try
            {
                if (TelegramDelivery == null) TelegramDelivery = new TelegramDelivery();
                LoggerHelper.LogDebug("Telegram bot started.");
            }
            catch (Exception ex)
            {
                LoggerHelper.LogError("Error starting Telegram bot", ex);
            }
        }

        public void StopTelegramBot()
        {
            try
            {
                if (TelegramDelivery != null)
                {
                    LoggerHelper.LogDebug("Telegram bot stopped.");
                }
            }
            catch (Exception ex)
            {
                LoggerHelper.LogError("Error stopping Telegram bot", ex);
            }
        }
    }
}
