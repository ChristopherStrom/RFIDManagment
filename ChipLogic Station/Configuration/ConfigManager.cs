using System;
using System.IO;
using System.Reflection;
using System.Xml.Serialization;
using ChipLogic.Utils;

namespace ChipLogic.Configuration
{
    [Serializable]
    public class DatabaseConfig
    {
        public string ConnectionString { get; set; } = @"Server=.\SQLEXPRESS;Database=ChipLogic;User Id=ChipLogic;Password=ChipLogic;TrustServerCertificate=True;Encrypt=True";
        public bool IsDatabaseCreated { get; set; } = false;
        public bool Debug { get; set; } = false;
        public int StationNumber { get; set; } = 1;
        public string InstallPath { get; set; } = @"C:\ChipLogic\";
    }

    public static class ConfigManager
    {
        private static string configFilePath;

        public static DatabaseConfig LoadOrCreateConfig()
        {
            DatabaseConfig tempConfig = CreateDefaultConfig();

            string initialConfigFilePath = Path.Combine(tempConfig.InstallPath, "config.xml");

            if (File.Exists(initialConfigFilePath))
            {
                tempConfig = LoadConfig(initialConfigFilePath);
                configFilePath = Path.Combine(tempConfig.InstallPath, "config.xml");
            }
            else
            {
                configFilePath = initialConfigFilePath;
            }

            DatabaseConfig config;

            if (File.Exists(configFilePath))
            {
                config = LoadConfig(configFilePath);
                ValidateAndUpdateConfig(ref config);
                SaveConfig(config);
            }
            else
            {
                config = CreateDefaultConfig();
                SaveConfig(config);
            }

            return config;
        }

        public static DatabaseConfig LoadConfig(string filePath)
        {
            try
            {
                XmlSerializer serializer = new XmlSerializer(typeof(DatabaseConfig));
                using (FileStream stream = new FileStream(filePath, FileMode.Open))
                {
                    return (DatabaseConfig)serializer.Deserialize(stream);
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"Error loading config: {ex.Message}", isError: true, debug: true);
                return CreateDefaultConfig();
            }
        }

        public static void SaveConfig(DatabaseConfig config)
        {
            try
            {
                XmlSerializer serializer = new XmlSerializer(typeof(DatabaseConfig));
                using (FileStream stream = new FileStream(configFilePath, FileMode.Create))
                {
                    serializer.Serialize(stream, config);
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"Error saving config: {ex.Message}", isError: true, debug: config.Debug);
            }
        }

        private static DatabaseConfig CreateDefaultConfig()
        {
            return new DatabaseConfig();
        }

        private static void ValidateAndUpdateConfig(ref DatabaseConfig config)
        {
            var defaultConfig = new DatabaseConfig();

            foreach (PropertyInfo property in typeof(DatabaseConfig).GetProperties())
            {
                var currentValue = property.GetValue(config);
                var defaultValue = property.GetValue(defaultConfig);

                if (currentValue == null || (currentValue is string str && string.IsNullOrWhiteSpace(str)))
                {
                    property.SetValue(config, defaultValue);
                }
            }
        }

        public static bool ValidateConfig(DatabaseConfig config)
        {
            return !string.IsNullOrWhiteSpace(config.ConnectionString);
        }

        public static void ReportConfigTypes()
        {
            foreach (PropertyInfo property in typeof(DatabaseConfig).GetProperties())
            {
                string propertyName = property.Name;
                string propertyType = property.PropertyType.Name;
                Logger.Log($"Property: {propertyName}, Type: {propertyType}", isError: false, debug: true);
            }
        }
    }
}
