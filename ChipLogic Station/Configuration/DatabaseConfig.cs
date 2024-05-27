using System;

namespace ChipLogic.Configuration
{
    [Serializable]
    public class DatabaseConfig
    {
        public string ConnectionString { get; set; }
        public bool IsDatabaseCreated { get; set; }
        public bool Debug { get; set; }
    }
}
