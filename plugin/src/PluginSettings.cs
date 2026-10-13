using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;

namespace Kennet.DeswikClaude
{
    /// <summary>Per-user settings in %AppData%\Kennet.DeswikClaude. API key is encrypted with Windows DPAPI.</summary>
    public class PluginSettings
    {
        public string Model = "claude-sonnet-5-5";
        public string ApiKeyProtected = "";
        public int MaxTokens = 16000;
        /// <summary>Run read-only tool calls and code (modifies=false) without the approval window.</summary>
        public bool AutoApproveReadOnly = true;
        /// <summary>Run changes without the approval window. Off by default; save often if you turn it on.</summary>
        public bool AutoApproveChanges = false;
        /// <summary>Panel toolbar (model, buttons, checkboxes) hidden behind the "Options" toggle.</summary>
        public bool ToolbarCollapsed = false;

        public static readonly string[] Models = { "claude-sonnet-5-5", "claude-opus-5-5", "claude-haiku-5-5" };

        public static string Folder
        {
            get
            {
                string f = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Kennet.DeswikClaude");
                Directory.CreateDirectory(f);
                return f;
            }
        }

        public static string ScriptsFolder
        {
            get
            {
                string f = Path.Combine(Folder, "Scripts");
                Directory.CreateDirectory(f);
                return f;
            }
        }

        /// <summary>Extra guides (*.md) appended to the built-in guide. Files here replace built-in ones with the same name.</summary>
        public static string KnowledgeFolder
        {
            get
            {
                string f = Path.Combine(Folder, "Knowledge");
                Directory.CreateDirectory(f);
                return f;
            }
        }

        private static string FilePath { get { return Path.Combine(Folder, "settings.json"); } }

        public static PluginSettings Load()
        {
            try
            {
                if (File.Exists(FilePath))
                    return JsonConvert.DeserializeObject<PluginSettings>(File.ReadAllText(FilePath)) ?? new PluginSettings();
            }
            catch { }
            return new PluginSettings();
        }

        public void Save()
        {
            File.WriteAllText(FilePath, JsonConvert.SerializeObject(this, Formatting.Indented));
        }

        [JsonIgnore]
        public string ApiKey
        {
            get
            {
                // Environment variable wins, so the key can also be managed centrally.
                string env = Environment.GetEnvironmentVariable("ANTHROPIC_API_KEY");
                if (!string.IsNullOrWhiteSpace(env)) return env.Trim();
                if (string.IsNullOrEmpty(ApiKeyProtected)) return "";
                try
                {
                    byte[] raw = ProtectedData.Unprotect(Convert.FromBase64String(ApiKeyProtected), null, DataProtectionScope.CurrentUser);
                    return Encoding.UTF8.GetString(raw);
                }
                catch { return ""; }
            }
            set
            {
                if (string.IsNullOrWhiteSpace(value)) { ApiKeyProtected = ""; return; }
                byte[] enc = ProtectedData.Protect(Encoding.UTF8.GetBytes(value.Trim()), null, DataProtectionScope.CurrentUser);
                ApiKeyProtected = Convert.ToBase64String(enc);
            }
        }
    }
}
