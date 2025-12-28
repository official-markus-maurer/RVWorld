using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;

namespace ROMVault.Avalonia.Utils
{
    public static class AppSettings
    {
        private static Dictionary<string, string> _settings = new Dictionary<string, string>();
        private static string _filePath = "TrrntZipSettings.xml";

        static AppSettings()
        {
            LoadSettings();
        }

        private static void LoadSettings()
        {
            if (File.Exists(_filePath))
            {
                try
                {
                    XmlSerializer serializer = new XmlSerializer(typeof(List<Entry>));
                    using (FileStream fs = new FileStream(_filePath, FileMode.Open))
                    {
                        var entries = serializer.Deserialize(fs) as List<Entry>;
                        if (entries != null)
                        {
                            foreach (var entry in entries)
                            {
                                if (entry.Key != null && entry.Value != null)
                                {
                                    _settings[entry.Key] = entry.Value;
                                }
                            }
                        }
                    }
                }
                catch
                {
                    // Ignore errors for now
                }
            }
        }

        private static void SaveSettings()
        {
            try
            {
                var entries = new List<Entry>();
                foreach (var kvp in _settings)
                {
                    entries.Add(new Entry { Key = kvp.Key, Value = kvp.Value });
                }

                XmlSerializer serializer = new XmlSerializer(typeof(List<Entry>));
                using (FileStream fs = new FileStream(_filePath, FileMode.Create))
                {
                    serializer.Serialize(fs, entries);
                }
            }
            catch
            {
                // Ignore errors
            }
        }

        public static string? ReadSetting(string key)
        {
            if (_settings.ContainsKey(key))
            {
                return _settings[key];
            }
            return null;
        }

        public static void AddUpdateAppSettings(string key, string value)
        {
            if (value == null) return;
            _settings[key] = value;
            SaveSettings();
        }

        public class Entry
        {
            public string? Key { get; set; }
            public string? Value { get; set; }
        }
    }
}
