using System;
using System.IO;
using System.Text.Json;

namespace PokerOrganizer.Core.Models
{
    public class UserSettings
    {
        public string Language { get; set; } = "en-US";

        private static string SettingsPath => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "PokerOrganizer",
            "settings.json"
        );

        public static UserSettings Load()
        {
            try
            {
                var directory = Path.GetDirectoryName(SettingsPath);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory!);
                }

                if (File.Exists(SettingsPath))
                {
                    var json = File.ReadAllText(SettingsPath);
                    return JsonSerializer.Deserialize<UserSettings>(json) ?? new UserSettings();
                }
            }
            catch
            {
                // Em caso de erro, retorna as configurações padrão
            }

            return new UserSettings();
        }

        public void Save()
        {
            try
            {
                var directory = Path.GetDirectoryName(SettingsPath);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory!);
                }

                var json = JsonSerializer.Serialize(this);
                File.WriteAllText(SettingsPath, json);
            }
            catch
            {
                // Log do erro se necessário
            }
        }
    }
} 