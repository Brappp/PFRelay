using System;
using Dalamud.Game.Command;
using Dalamud.Interface.Windowing;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using PFRelay.Delivery;
using PFRelay.Impl;
using PFRelay.Util;
using PFRelay.Windows;

namespace PFRelay
{
    public sealed class Plugin : IDalamudPlugin
    {
        public string Name => "PFRelay";
        private const string CommandName = "/PFRelay";

        private IDalamudPluginInterface PluginInterface { get; init; }
        private ICommandManager CommandManager { get; init; }

        public static Configuration Configuration { get; private set; }
        public TelegramDelivery? TelegramDelivery { get; set; }
        public DiscordDMDelivery? DiscordDMDelivery { get; set; }

        public WindowSystem WindowSystem = new("PFRelay");
        private ConfigWindow ConfigWindow { get; init; }

        public Plugin(IDalamudPluginInterface pluginInterface, ICommandManager commandManager)
        {
            try
            {
                pluginInterface.Create<Service>();

                PluginInterface = pluginInterface;
                CommandManager = commandManager;

                // Load configuration explicitly from file
                Configuration = Configuration.Load();
                Configuration.Initialize(PluginInterface);

                ConfigWindow = new ConfigWindow(this);
                WindowSystem.AddWindow(ConfigWindow);

                CommandManager.AddHandler(CommandName, new CommandInfo(OnCommand)
                {
                    HelpMessage = "Opens the configuration window."
                });

                PluginInterface.UiBuilder.Draw += DrawUI;
                PluginInterface.UiBuilder.OpenConfigUi += DrawConfigUI;
                PluginInterface.UiBuilder.OpenMainUi += OpenMainUi;

                // Start bots if they are enabled in the configuration
                if (Configuration.EnableDiscordBot) StartDiscordBot();
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
                PluginInterface.UiBuilder.OpenMainUi -= OpenMainUi;
                PluginInterface.UiBuilder.Draw -= DrawUI;
                PluginInterface.UiBuilder.OpenConfigUi -= DrawConfigUI;

                WindowSystem.RemoveAllWindows();
                ConfigWindow.Dispose();

                CrossWorldPartyListSystem.Stop();
                PartyListener.Off();
                DutyListener.Off();

                CommandManager.RemoveHandler(CommandName);

                // Stop and save bot states on disposal
                if (Configuration.EnableTelegramBot)
                {
                    StopTelegramBot();
                    Configuration.EnableTelegramBot = false;
                    Configuration.Save();
                }

                if (Configuration.EnableDiscordBot)
                {
                    StopDiscordBot();
                    Configuration.EnableDiscordBot = false;
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

        private void OnCommand(string command, string args) => ConfigWindow.IsOpen = true;

        private void DrawUI() => WindowSystem.Draw();

        public void DrawConfigUI() => ConfigWindow.IsOpen = true;

        public void StartDiscordBot()
        {
            try
            {
                if (DiscordDMDelivery == null) DiscordDMDelivery = new DiscordDMDelivery();
                if (!DiscordDMDelivery.IsActive)
                {
                    DiscordDMDelivery.StartListening();
                    LoggerHelper.LogDebug("Discord DM bot started.");
                }
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
                if (DiscordDMDelivery?.IsActive == true)
                {
                    DiscordDMDelivery.StopListening();
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
                if (!TelegramDelivery.IsActive)
                {
                    TelegramDelivery.StartListening();
                    LoggerHelper.LogDebug("Telegram bot started.");
                }
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
                if (TelegramDelivery?.IsActive == true)
                {
                    TelegramDelivery.StopListening();
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
