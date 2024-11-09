using Dalamud.Game.Command;
using Dalamud.Interface.Windowing;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using Flurl.Http;
using Flurl.Http.Configuration;
using Newtonsoft.Json;
using PFRelay.Impl;
using PFRelay.Util;
using PFRelay.Windows;
using PFRelay.Delivery;

namespace PFRelay;

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
        // Ensure Service is initialized with dependency injection before accessing any of its properties
        pluginInterface.Create<Service>();

        PluginInterface = pluginInterface;
        CommandManager = commandManager;

        // Configuration initialization
        Configuration = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
        Configuration.Initialize(PluginInterface);

        // Window setup
        ConfigWindow = new ConfigWindow(this);
        WindowSystem.AddWindow(ConfigWindow);

        // Command handler setup
        CommandManager.AddHandler(CommandName, new CommandInfo(OnCommand)
        {
            HelpMessage = "Opens the configuration window."
        });

        // UI builder events
        PluginInterface.UiBuilder.Draw += DrawUI;
        PluginInterface.UiBuilder.OpenConfigUi += DrawConfigUI;

        // Initialize bots if enabled in configuration
        if (Configuration.EnableTelegramBot)
        {
            StartTelegramBot();
        }
        if (Configuration.EnableDiscordBot)
        {
            StartDiscordBot();
        }

        // Start necessary systems
        CrossWorldPartyListSystem.Start();
        PartyListener.On();
        DutyListener.On();

        // Log a message to confirm initialization
        Service.PluginLog.Debug("PFRelay Plugin initialization started.");

        // Debug logging for each service
        Service.PluginLog.Debug($"Plugin Interface Initialized: {Service.PluginInterface != null}");
        Service.PluginLog.Debug($"Command Manager Initialized: {Service.CommandManager != null}");
        Service.PluginLog.Debug($"Client State Initialized: {Service.ClientState != null}");
        Service.PluginLog.Debug($"Party List Initialized: {Service.PartyList != null}");
        Service.PluginLog.Debug($"Framework Initialized: {Service.Framework != null}");
        Service.PluginLog.Debug($"Chat GUI Initialized: {Service.ChatGui != null}");
        Service.PluginLog.Debug($"Data Manager Initialized: {Service.DataManager != null}");
        Service.PluginLog.Debug($"Plugin Log Initialized: {Service.PluginLog != null}");

        Service.PluginLog.Debug("PFRelay Plugin has been successfully initialized.");
    }

    public void Dispose()
    {
        // Dispose and cleanup
        WindowSystem.RemoveAllWindows();
        ConfigWindow.Dispose();

        CrossWorldPartyListSystem.Stop();
        PartyListener.Off();
        DutyListener.Off();

        CommandManager.RemoveHandler(CommandName);

        // Stop bots and update configuration if enabled
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

    private void OnCommand(string command, string args)
    {
        if (args == "debugOnlineStatus")
        {
            Service.ChatGui.Print($"OnlineStatus ID = {Service.ClientState.LocalPlayer!.OnlineStatus.Id}");
            return;
        }

        ConfigWindow.IsOpen = true;
    }

    private void DrawUI()
    {
        WindowSystem.Draw();
    }

    public void DrawConfigUI()
    {
        ConfigWindow.IsOpen = true;
    }
}
