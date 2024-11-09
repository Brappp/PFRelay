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
            // Initialize dependency injection for the Service class
            pluginInterface.Create<Service>();

            PluginInterface = pluginInterface;
            CommandManager = commandManager;

            // Load and initialize configuration
            Configuration = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
            Configuration.Initialize(PluginInterface);

            // Set up the configuration window
            ConfigWindow = new ConfigWindow(this);
            WindowSystem.AddWindow(ConfigWindow);

            // Register the main plugin command
            CommandManager.AddHandler(CommandName, new CommandInfo(OnCommand)
            {
                HelpMessage = "Opens the configuration window."
            });

            // Register UI callbacks with UiBuilder
            PluginInterface.UiBuilder.Draw += DrawUI;
            PluginInterface.UiBuilder.OpenConfigUi += DrawConfigUI;
            PluginInterface.UiBuilder.OpenMainUi += OpenMainUi; // Register as the main UI entry

            // Initialize bots if they are enabled in the configuration
            if (Configuration.EnableTelegramBot)
            {
                StartTelegramBot();
            }
            if (Configuration.EnableDiscordBot)
            {
                StartDiscordBot();
            }

            // Start other systems as needed
            CrossWorldPartyListSystem.Start();
            PartyListener.On();
            DutyListener.On();

            Service.PluginLog.Debug("PFRelay Plugin has been successfully initialized.");
        }

        public void Dispose()
        {
            // Unregister UI callbacks to clean up
            PluginInterface.UiBuilder.OpenMainUi -= OpenMainUi;
            PluginInterface.UiBuilder.Draw -= DrawUI;
            PluginInterface.UiBuilder.OpenConfigUi -= DrawConfigUI;

            // Dispose of the config window and remove all windows from WindowSystem
            WindowSystem.RemoveAllWindows();
            ConfigWindow.Dispose();

            // Stop other systems
            CrossWorldPartyListSystem.Stop();
            PartyListener.Off();
            DutyListener.Off();

            // Remove the command handler
            CommandManager.RemoveHandler(CommandName);

            // Stop and disable bots in the configuration if they are enabled
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
        }

        // Main UI method to open ConfigWindow for the plugin
        public void OpenMainUi()
        {
            ConfigWindow.IsOpen = true;
        }

        // Command handler to open the configuration window
        private void OnCommand(string command, string args)
        {
            ConfigWindow.IsOpen = true;
        }

        // Draw method for the WindowSystem
        private void DrawUI()
        {
            WindowSystem.Draw();
        }

        // Opens the ConfigWindow as a configuration UI
        public void DrawConfigUI()
        {
            ConfigWindow.IsOpen = true;
        }

        // Method to start the Discord DM bot
        public void StartDiscordBot()
        {
            if (DiscordDMDelivery == null)
                DiscordDMDelivery = new DiscordDMDelivery();
            if (!DiscordDMDelivery.IsActive)
            {
                DiscordDMDelivery.StartListening();
                Service.PluginLog.Debug("Discord DM bot started.");
            }
        }

        // Method to stop the Discord DM bot
        public void StopDiscordBot()
        {
            if (DiscordDMDelivery?.IsActive == true)
            {
                DiscordDMDelivery.StopListening();
                Service.PluginLog.Debug("Discord DM bot stopped.");
            }
        }

        // Method to start the Telegram bot
        public void StartTelegramBot()
        {
            if (TelegramDelivery == null)
                TelegramDelivery = new TelegramDelivery();
            if (!TelegramDelivery.IsActive)
            {
                TelegramDelivery.StartListening();
                Service.PluginLog.Debug("Telegram bot started.");
            }
        }

        // Method to stop the Telegram bot
        public void StopTelegramBot()
        {
            if (TelegramDelivery?.IsActive == true)
            {
                TelegramDelivery.StopListening();
                Service.PluginLog.Debug("Telegram bot stopped.");
            }
        }
    }
}
