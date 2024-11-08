using System;
using Dalamud.Configuration;
using Dalamud.Plugin;

namespace PFRelay
{
    [Serializable]
    public class Configuration : IPluginConfiguration
    {
        [NonSerialized]
        private IDalamudPluginInterface? PluginInterface;

        public int Version { get; set; } = 1;

        // Discord Configuration
        public string DiscordUserToken { get; set; } = "";
        public string UserSecretKey { get; set; } = "";
        public bool EnableDiscordBot { get; set; } = false;

        // Telegram Configuration
        public string TelegramBotToken { get; set; } = "";
        public string TelegramChatId { get; set; } = "";
        public bool EnableTelegramBot { get; set; } = false;

        // Additional properties for DutyListener and AFK status checks
        public bool EnableForDutyPops { get; set; } = true;
        public bool IgnoreAfkStatus { get; set; } = false;

        public void Initialize(IDalamudPluginInterface pluginInterface)
        {
            PluginInterface = pluginInterface;
        }

        public void Save()
        {
            PluginInterface!.SavePluginConfig(this);
        }
    }
}
