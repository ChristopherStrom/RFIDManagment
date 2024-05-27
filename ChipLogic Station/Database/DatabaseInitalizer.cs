using System;
using System.Data.SqlClient;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using ChipLogic.Configuration;
using ChipLogic.Utils;

namespace ChipLogic.Database
{
    public static class DatabaseInitializer
    {
        public static bool CreateDatabaseAndSeed(DatabaseConfig config)
        {
            try
            {
                Logger.Log("Starting database creation and seeding process.", isError: false, debug: config.Debug);

                var builder = new SqlConnectionStringBuilder(config.ConnectionString)
                {
                    InitialCatalog = "master",
                    TrustServerCertificate = true,
                    Encrypt = true
                };
                string masterConnectionString = builder.ConnectionString;

                builder.InitialCatalog = "";
                string connectionStringWithOptions = builder.ConnectionString;
                string databaseName = new SqlConnectionStringBuilder(config.ConnectionString).InitialCatalog;

                if (!CreateDatabaseIfNotExists(masterConnectionString, databaseName, config))
                {
                    return false;
                }

                using (SqlConnection connection = new SqlConnection(config.ConnectionString))
                {
                    try
                    {
                        connection.Open();
                        Logger.Log("Connection to specific database established.", isError: false, debug: config.Debug);

                        if (!CreateTables(connection, config)) return false;
                        if (!SeedKeysTable(connection, config)) return false;
                        if (!SeedDefaultUser(connection, config)) return false;
                        if (!SeedInitialVersion(connection, config)) return false;

                        config.IsDatabaseCreated = true;
                        ConfigManager.SaveConfig(config);

                        Logger.Log("Database created and seeded successfully.", isError: false, debug: config.Debug);
                        return true;
                    }
                    catch (SqlException sqlEx)
                    {
                        Logger.Log($"SQL error during table creation or seeding: {sqlEx.Message}", isError: true, debug: config.Debug);
                        return false;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"Error seeding database: {ex.Message}", isError: true, debug: config.Debug);
                return false;
            }
        }

        private static bool CreateDatabaseIfNotExists(string masterConnectionString, string databaseName, DatabaseConfig config)
        {
            Logger.Log("Attempting to open SQL connection to master database...", isError: false, debug: config.Debug);
            using (SqlConnection connection = new SqlConnection(masterConnectionString))
            {
                try
                {
                    connection.Open();
                    Logger.Log("Connection to SQL Server established.", isError: false, debug: config.Debug);

                    Logger.Log($"Creating database '{databaseName}' if it does not exist.", isError: false, debug: config.Debug);
                    string createDbQuery = $@"
                        IF NOT EXISTS (SELECT * FROM sys.databases WHERE name = '{databaseName}')
                        BEGIN
                            CREATE DATABASE [{databaseName}];
                        END";

                    using (SqlCommand command = new SqlCommand(createDbQuery, connection))
                    {
                        command.ExecuteNonQuery();
                        Logger.Log($"Database '{databaseName}' checked and created if not present.", isError: false, debug: config.Debug);
                        return true;
                    }
                }
                catch (SqlException sqlEx)
                {
                    Logger.Log($"SQL error during database creation: {sqlEx.Message}", isError: true, debug: config.Debug);
                    return false;
                }
            }
        }

        private static bool CreateTables(SqlConnection connection, DatabaseConfig config)
        {
            Logger.Log("Checking/creating tables...", isError: false, debug: config.Debug);
            string createTablesQuery = @"
                IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Users')
                BEGIN
                    CREATE TABLE Users (
                        UserID UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
                        Username NVARCHAR(50) NOT NULL UNIQUE,
                        PasswordHash NVARCHAR(128) NOT NULL,
                        isActive BIT NOT NULL DEFAULT 1
                    );
                END
                IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Keys')
                BEGIN
                    CREATE TABLE Keys (
                        KeyID UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
                        Name NVARCHAR(50) NOT NULL,
                        KeyValue NVARCHAR(128) NOT NULL
                    );
                END
                IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'UserPermissions')
                BEGIN
                    CREATE TABLE UserPermissions (
                        PermissionID UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
                        UserID UNIQUEIDENTIFIER NOT NULL FOREIGN KEY REFERENCES Users(UserID),
                        IsAdmin BIT NOT NULL,
                        CanScanIn BIT NOT NULL,
                        CanScanOut BIT NOT NULL,
                        CanAssign BIT NOT NULL,
                        CanViewReports BIT NOT NULL
                    );
                END
                IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Versions')
                BEGIN
                    CREATE TABLE Versions (
                        VersionID UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
                        VersionNumber NVARCHAR(50) NOT NULL,
                        AppliedOn DATETIME NOT NULL DEFAULT GETDATE()
                    );
                END";

            using (SqlCommand command = new SqlCommand(createTablesQuery, connection))
            {
                try
                {
                    command.ExecuteNonQuery();
                    Logger.Log("Tables checked/created successfully.", isError: false, debug: config.Debug);
                    return true;
                }
                catch (SqlException sqlEx)
                {
                    Logger.Log($"SQL error during table creation: {sqlEx.Message}", isError: true, debug: config.Debug);
                    return false;
                }
            }
        }

        private static bool SeedKeysTable(SqlConnection connection, DatabaseConfig config)
        {
            Logger.Log("Generating random salt...", isError: false, debug: config.Debug);
            string salt = GenerateSalt();

            Logger.Log("Inserting password salt into Keys table...", isError: false, debug: config.Debug);
            string insertSaltQuery = "INSERT INTO Keys (Name, KeyValue) VALUES ('PasswordSalt', @Salt)";
            using (SqlCommand command = new SqlCommand(insertSaltQuery, connection))
            {
                try
                {
                    command.Parameters.AddWithValue("@Salt", salt);
                    command.ExecuteNonQuery();
                    Logger.Log("Password salt inserted successfully.", isError: false, debug: config.Debug);
                    return true;
                }
                catch (SqlException sqlEx)
                {
                    Logger.Log($"SQL error during salt insertion: {sqlEx.Message}", isError: true, debug: config.Debug);
                    return false;
                }
            }
        }

        private static bool SeedDefaultUser(SqlConnection connection, DatabaseConfig config)
        {
            Logger.Log("Generating random password...", isError: false, debug: config.Debug);
            string password = GenerateRandomPassword();
            string salt = GetStoredSalt(config.ConnectionString);
            Logger.Log("Hashing password with the generated salt...", isError: false, debug: config.Debug);
            string hashedPassword = HashPassword(password, salt);

            Logger.Log("Inserting default user with hashed password...", isError: false, debug: config.Debug);
            string insertUserQuery = "INSERT INTO Users (UserID, Username, PasswordHash, isActive) VALUES (NEWID(), 'ChipLogic', @PasswordHash, 1)";
            using (SqlCommand command = new SqlCommand(insertUserQuery, connection))
            {
                try
                {
                    command.Parameters.AddWithValue("@PasswordHash", hashedPassword);
                    command.ExecuteNonQuery();
                    Logger.Log("Default user created successfully.", isError: false, debug: config.Debug);
                }
                catch (SqlException sqlEx)
                {
                    Logger.Log($"SQL error during default user insertion: {sqlEx.Message}", isError: true, debug: config.Debug);
                    return false;
                }
            }

            Logger.Log("Inserting permissions for default user...", isError: false, debug: config.Debug);
            string insertPermissionsQuery = @"
                INSERT INTO UserPermissions (PermissionID, UserID, IsAdmin, CanScanIn, CanScanOut, CanAssign, CanViewReports)
                VALUES (NEWID(), (SELECT UserID FROM Users WHERE Username = 'ChipLogic'), 1, 1, 1, 1, 1);";
            using (SqlCommand command = new SqlCommand(insertPermissionsQuery, connection))
            {
                try
                {
                    command.ExecuteNonQuery();
                    Logger.Log("Permissions for default user inserted successfully.", isError: false, debug: config.Debug);
                }
                catch (SqlException sqlEx)
                {
                    Logger.Log($"SQL error during permissions insertion: {sqlEx.Message}", isError: true, debug: config.Debug);
                    return false;
                }
            }

            Logger.Log("Saving default password to file...", isError: false, debug: config.Debug);
            SavePasswordToFile(password);

            return true;
        }

        private static bool SeedInitialVersion(SqlConnection connection, DatabaseConfig config)
        {
            Logger.Log("Inserting initial version into Versions table...", isError: false, debug: config.Debug);
            string insertVersionQuery = "INSERT INTO Versions (VersionNumber) VALUES (@VersionNumber)";
            using (SqlCommand command = new SqlCommand(insertVersionQuery, connection))
            {
                try
                {
                    command.Parameters.AddWithValue("@VersionNumber", "1.0.0"); // Initial version
                    command.ExecuteNonQuery();
                    Logger.Log("Initial version inserted successfully.", isError: false, debug: config.Debug);
                    return true;
                }
                catch (SqlException sqlEx)
                {
                    Logger.Log($"SQL error during initial version insertion: {sqlEx.Message}", isError: true, debug: config.Debug);
                    return false;
                }
            }
        }

        public static void CheckAndUpdateDatabase(DatabaseConfig config, string currentVersion)
        {
            using (SqlConnection connection = new SqlConnection(config.ConnectionString))
            {
                try
                {
                    connection.Open();
                    string query = "SELECT TOP 1 VersionNumber FROM Versions ORDER BY AppliedOn DESC";
                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        var versionInDb = command.ExecuteScalar()?.ToString();
                        if (string.Compare(versionInDb, currentVersion) < 0)
                        {
                            Logger.Log($"Updating database from version {versionInDb} to {currentVersion}.", isError: false, debug: config.Debug);
                            UpdateDatabaseSchema(connection, currentVersion);
                            UpdateVersionTable(connection, currentVersion);
                        }
                        else
                        {
                            Logger.Log("Database is up-to-date.", isError: false, debug: config.Debug);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.Log($"Error checking and updating database: {ex.Message}", isError: true, debug: config.Debug);
                }
            }
        }

        private static void UpdateVersionTable(SqlConnection connection, string currentVersion)
        {
            Logger.Log("Updating Versions table...", isError: false, debug: true);
            string updateVersionQuery = "INSERT INTO Versions (VersionNumber) VALUES (@VersionNumber)";
            using (SqlCommand command = new SqlCommand(updateVersionQuery, connection))
            {
                try
                {
                    command.Parameters.AddWithValue("@VersionNumber", currentVersion);
                    command.ExecuteNonQuery();
                    Logger.Log($"Versions table updated successfully to version {currentVersion}.", isError: false, debug: true);
                }
                catch (SqlException sqlEx)
                {
                    Logger.Log($"SQL error during version update: {sqlEx.Message}", isError: true, debug: true);
                }
            }
        }

        private static void UpdateDatabaseSchema(SqlConnection connection, string currentVersion)
        {
            Logger.Log($"Updating database schema to version {currentVersion}...", isError: false, debug: true);
            string updateSchemaQuery = @"
                IF NOT EXISTS (SELECT * FROM sys.columns WHERE Name = N'isActive' AND Object_ID = Object_ID(N'Users'))
                BEGIN
                    ALTER TABLE Users ADD isActive BIT NOT NULL DEFAULT 1;
                END";
            using (SqlCommand command = new SqlCommand(updateSchemaQuery, connection))
            {
                try
                {
                    command.ExecuteNonQuery();
                    Logger.Log($"Database schema updated successfully to version {currentVersion}.", isError: false, debug: true);
                }
                catch (SqlException sqlEx)
                {
                    Logger.Log($"SQL error during schema update: {sqlEx.Message}", isError: true, debug: true);
                }
            }
        }

        public static string GenerateSalt()
        {
            using (var rng = new RNGCryptoServiceProvider())
            {
                byte[] saltBytes = new byte[16];
                rng.GetBytes(saltBytes);
                return Convert.ToBase64String(saltBytes);
            }
        }

        public static string HashPassword(string password, string salt)
        {
            using (var sha256 = SHA256.Create())
            {
                byte[] saltedPassword = Encoding.UTF8.GetBytes(password + salt);
                byte[] hashBytes = sha256.ComputeHash(saltedPassword);
                return Convert.ToBase64String(hashBytes);
            }
        }

        private static string GenerateRandomPassword()
        {
            const string validChars = "ABCDEFGHJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789!@#$%^&*()_-+=[{]}|;:<>/?";
            StringBuilder password = new StringBuilder();
            Random random = new Random();

            int length = random.Next(10, 13); // Password length between 10 and 13 characters
            for (int i = 0; i < length; i++)
            {
                password.Append(validChars[random.Next(validChars.Length)]);
            }

            return password.ToString();
        }

        private static void SavePasswordToFile(string password)
        {
            string filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "defaultlogin.txt");
            try
            {
                File.WriteAllText(filePath, $"Default User: ChipLogic\nPassword: {password}");
                Logger.Log("Default password saved to file.", isError: false, debug: true);
            }
            catch (Exception ex)
            {
                Logger.Log($"Error saving password to file: {ex.Message}", isError: true, debug: true);
            }
        }

        public static bool CheckDatabaseConnection(DatabaseConfig config)
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(config.ConnectionString))
                {
                    Logger.Log("Attempting to open SQL connection...", isError: false, debug: config.Debug);
                    connection.Open();
                    Logger.Log("SQL connection opened successfully.", isError: false, debug: config.Debug);
                    return true;
                }
            }
            catch (SqlException sqlEx)
            {
                Logger.Log($"SQL error connecting to the database: {sqlEx.Message}", isError: true, debug: config.Debug);
                return false;
            }
            catch (Exception ex)
            {
                Logger.Log($"Error connecting to the database: {ex.Message}", isError: true, debug: config.Debug);
                return false;
            }
        }

        public static string GetStoredSalt(string connectionString)
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();

                string query = "SELECT KeyValue FROM Keys WHERE Name = 'PasswordSalt';";
                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return reader["KeyValue"].ToString();
                        }
                        else
                        {
                            throw new Exception("PasswordSalt not found in Keys table.");
                        }
                    }
                }
            }
        }

        public static bool CheckIfUpdateRequired(SqlConnection connection)
        {
            string getVersionQuery = "SELECT TOP 1 VersionNumber FROM Versions ORDER BY AppliedOn DESC";
            using (SqlCommand command = new SqlCommand(getVersionQuery, connection))
            {
                try
                {
                    string currentVersionInDb = command.ExecuteScalar()?.ToString();
                    if (currentVersionInDb != GlobalConstants.CurrentVersion)
                    {
                        UpdateDatabaseSchema(connection, GlobalConstants.CurrentVersion);
                        UpdateVersionTable(connection, GlobalConstants.CurrentVersion);
                        return true;
                    }
                    return false;
                }
                catch (SqlException sqlEx)
                {
                    Logger.Log($"SQL error during version check: {sqlEx.Message}", isError: true, debug: true);
                    return false;
                }
            }
        }
    }
}
