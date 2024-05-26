using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace ChipLogic.Configuration
{
    public class ConfigManager
    {
        private static readonly Dictionary<string, string> DefaultConfigItems = new Dictionary<string, string>
        {
            { "ConnectionString", "Server=.\\sqlexpress;Database=ChipLogic;User Id=ChipLogic;Password=ChipLogic;TrustServerCertificate=True;" },
            { "IsDatabaseCreated", "false" },
            { "Debug", "false" }
        };

        #region Config File Path

        private static string GetConfigFilePath()
        {
            string installDirectory = AppDomain.CurrentDomain.BaseDirectory;
            return Path.Combine(installDirectory, "config.xml");
        }

        #endregion

        #region Load or Create Config

        public static DatabaseConfig LoadOrCreateConfig()
        {
            string configFilePath = GetConfigFilePath();

            if (!File.Exists(configFilePath))
            {
                CreateDefaultConfig(configFilePath);
            }

            return LoadConfig(configFilePath);
        }

        private static void CreateDefaultConfig(string filePath)
        {
            XElement config = new XElement("Configuration",
                new XElement("Database",
                    DefaultConfigItems.Select(kv => new XElement(kv.Key, kv.Value))
                )
            );

            config.Save(filePath);
        }

        private static void ValidateAndUpdateConfig(string filePath)
        {
            XElement config = XElement.Load(filePath);
            XElement databaseElement = config.Element("Database");

            foreach (var kv in DefaultConfigItems)
            {
                if (databaseElement.Element(kv.Key) == null)
                {
                    databaseElement.Add(new XElement(kv.Key, kv.Value));
                }
            }

            config.Save(filePath);
        }

        private static DatabaseConfig LoadConfig(string filePath)
        {
            ValidateAndUpdateConfig(filePath);

            XElement config = XElement.Load(filePath);
            XElement databaseElement = config.Element("Database");

            DatabaseConfig dbConfig = new DatabaseConfig();
            foreach (var element in databaseElement.Elements())
            {
                dbConfig.ConfigItems[element.Name.LocalName] = element.Value;
            }

            return dbConfig;
        }

        #endregion

        #region Update Config

        public static void UpdateConfig(DatabaseConfig config)
        {
            string configFilePath = GetConfigFilePath();

            XElement xmlConfig = new XElement("Configuration",
                new XElement("Database",
                    config.ConfigItems.Select(kv => new XElement(kv.Key, kv.Value))
                )
            );

            xmlConfig.Save(configFilePath);
        }

        #endregion
    }
}
