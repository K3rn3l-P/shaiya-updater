using System.IO;
using System.Text.Json;

namespace Updater.Tool.Services
{
    public class PatchToolSettings
    {
        public string? LastPatchDirectory { get; set; }

        private static readonly string SettingsFile = Path.Combine(
            System.AppDomain.CurrentDomain.BaseDirectory, "PatchToolSettings.json");

        public static PatchToolSettings Load()
        {
            if (File.Exists(SettingsFile))
            {
                try
                {
                    var json = File.ReadAllText(SettingsFile);
                    return JsonSerializer.Deserialize<PatchToolSettings>(json) ?? new PatchToolSettings();
                }
                catch { }
            }
            return new PatchToolSettings();
        }

        public void Save()
        {
            var json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(SettingsFile, json);
        }
    }
}
