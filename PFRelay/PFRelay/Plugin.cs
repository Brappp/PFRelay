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
    public DiscordDMDelivery? DiscordDMDelivery { get; set; }  // Add DiscordDMDelivery instance

    public WindowSystem WindowSystem = new("PFRelay");
    private ConfigWindow ConfigWindow { get; init; }

    public Plugin(IDalamudPluginInterface pluginInterface, ICommandManager commandManager)
    {
        pluginInterface.Create<Service>();

        PluginInterface = pluginInterface;
        CommandManager = commandManager;

        Configuration = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
        Configuration.Initialize(PluginInterface);

        ConfigWindow = new ConfigWindow(this);
        WindowSystem.AddWindow(ConfigWindow);

        CommandManager.AddHandler(CommandName, new CommandInfo(OnCommand)
        {
            HelpMessage = "Opens the configuration window."
        });

        PluginInterface.UiBuilder.Draw += DrawUI;
        PluginInterface.UiBuilder.OpenConfigUi += DrawConfigUI;

        // Initialize Telegram bot if enabled
        if (Configuration.EnableTelegramBot)
        {
            TelegramDelivery = new TelegramDelivery();
            if (TelegramDelivery.IsActive) TelegramDelivery.StartListening();
        }

        // Initialize Discord DM bot if enabled
        if (Configuration.EnableDiscordBot)
        {
            DiscordDMDelivery = new DiscordDMDelivery();
            if (DiscordDMDelivery.IsActive) DiscordDMDelivery.StartListening();
        }

        CrossWorldPartyListSystem.Start();
        PartyListener.On();
        DutyListener.On();
    }

    public void Dispose()
    {
        WindowSystem.RemoveAllWindows();
        ConfigWindow.Dispose();

        CrossWorldPartyListSystem.Stop();
        PartyListener.Off();
        DutyListener.Off();

        CommandManager.RemoveHandler(CommandName);

        // Stop and disable Telegram bot if enabled
        if (Configuration.EnableTelegramBot)
        {
            TelegramDelivery?.StopListening();
            Configuration.EnableTelegramBot = false;
            Configuration.Save();
        }

        // Stop and disable Discord DM bot if enabled
        if (Configuration.EnableDiscordBot)
        {
            DiscordDMDelivery?.StopListening();
            Configuration.EnableDiscordBot = false;
            Configuration.Save();
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
