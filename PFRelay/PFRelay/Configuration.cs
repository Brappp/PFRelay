using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using Dalamud.Configuration;
using Dalamud.Plugin;
using PFRelay.Util;

namespace PFRelay
{
    [Serializable]
    public class Configuration : IPluginConfiguration
    {
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

        // Define the file path for the configuration file
        [JsonIgnore]
        private static string ConfigFilePath => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "XIVLauncher", "pluginConfigs", "PFRelay", "config.json"
        );

        public void Initialize(IDalamudPluginInterface pluginInterface)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(ConfigFilePath)!);
        }

        // Save configuration to a JSON file
        public void Save()
        {
            var jsonOptions = new JsonSerializerOptions
            {
                WriteIndented = true
            };

            try
            {
                var jsonString = JsonSerializer.Serialize(this, jsonOptions);
                File.WriteAllText(ConfigFilePath, jsonString);
            }
            catch (Exception ex)
            {
                LoggerHelper.LogError("Failed to save configuration to file.", ex);
            }
        }

        // Load configuration from a JSON file, if it exists
        public static Configuration Load()
        {
            if (File.Exists(ConfigFilePath))
            {
                try
                {
                    var jsonString = File.ReadAllText(ConfigFilePath);
                    var loadedConfig = JsonSerializer.Deserialize<Configuration>(jsonString);

                    if (loadedConfig != null)
                    {
                        return loadedConfig;
                    }
                }
                catch (Exception ex)
                {
                    LoggerHelper.LogError("Failed to load configuration from file.", ex);
                }
            }
            return new Configuration();
        }
    }
}
