using System;
using System.IO;
using System.Xml.Serialization;
using ChipLogic.Utils;

namespace ChipLogic.Configuration
{
    public static class ConfigManager
    {
        private static readonly string configFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.xml");

        public static DatabaseConfig LoadOrCreateConfig()
        {
            if (File.Exists(configFilePath))
            {
                return LoadConfig();
            }
            else
            {
                return CreateDefaultConfig();
            }
        }

        public static DatabaseConfig LoadConfig()
        {
            try
            {
                XmlSerializer serializer = new XmlSerializer(typeof(DatabaseConfig));
                using (FileStream stream = new FileStream(configFilePath, FileMode.Open))
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
            DatabaseConfig config = new DatabaseConfig
            {
                ConnectionString = @"Server=.\SQLEXPRESS;Database=ChipLogic;User Id=ChipLogic;Password=ChipLogic;TrustServerCertificate=True;Encrypt=False",
                IsDatabaseCreated = false,
                Debug = false
            };
            SaveConfig(config);
            return config;
        }

        public static bool ValidateConfig(DatabaseConfig config)
        {
            return !string.IsNullOrWhiteSpace(config.ConnectionString);
        }
    }
}
