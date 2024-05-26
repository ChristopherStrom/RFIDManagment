using System.Collections.Generic;

namespace ChipLogic.Configuration
{
    public class DatabaseConfig
    {
        public Dictionary<string, string> ConfigItems { get; set; } = new Dictionary<string, string>();

        public string ConnectionString
        {
            get => ConfigItems.ContainsKey("ConnectionString") ? ConfigItems["ConnectionString"] : null;
            set => ConfigItems["ConnectionString"] = value;
        }

        public bool IsDatabaseCreated
        {
            get => ConfigItems.ContainsKey("IsDatabaseCreated") && bool.Parse(ConfigItems["IsDatabaseCreated"]);
            set => ConfigItems["IsDatabaseCreated"] = value.ToString().ToLower();
        }

        public bool Debug
        {
            get => ConfigItems.ContainsKey("Debug") && bool.Parse(ConfigItems["Debug"]);
            set => ConfigItems["Debug"] = value.ToString().ToLower();
        }
    }
}
