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

                // Connection string to the master database
                string masterConnectionString = @"Server=.\SQLEXPRESS;Database=master;User Id=ChipLogic;Password=ChipLogic;";

                var builder = new SqlConnectionStringBuilder(config.ConnectionString)
                {
                    TrustServerCertificate = true,
                    Encrypt = false
                };
                string databaseName = builder.InitialCatalog;
                string connectionStringWithOptions = builder.ConnectionString;

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
                        }
                    }
                    catch (SqlException sqlEx)
                    {
                        Logger.Log($"SQL error during database creation: {sqlEx.Message}", isError: true, debug: config.Debug);
                        return false;
                    }
                }

                Logger.Log("Attempting to open SQL connection to specific database...", isError: false, debug: config.Debug);
                using (SqlConnection connection = new SqlConnection(connectionStringWithOptions))
                {
                    try
                    {
                        connection.Open();
                        Logger.Log("Connection to specific database established.", isError: false, debug: config.Debug);

                        Logger.Log("Checking/creating tables...", isError: false, debug: config.Debug);
                        string createTablesQuery = @"
                            IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Users')
                            BEGIN
                                CREATE TABLE Users (
                                    UserID INT PRIMARY KEY IDENTITY,
                                    Username NVARCHAR(50) NOT NULL,
                                    PasswordHash NVARCHAR(128) NOT NULL
                                );
                            END
                            IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Keys')
                            BEGIN
                                CREATE TABLE Keys (
                                    KeyID INT PRIMARY KEY IDENTITY,
                                    Name NVARCHAR(50) NOT NULL,
                                    KeyValue NVARCHAR(128) NOT NULL
                                );
                            END";

                        using (SqlCommand command = new SqlCommand(createTablesQuery, connection))
                        {
                            command.ExecuteNonQuery();
                            Logger.Log("Tables checked/created successfully.", isError: false, debug: config.Debug);
                        }

                        Logger.Log("Generating random salt...", isError: false, debug: config.Debug);
                        string salt = GenerateSalt();

                        Logger.Log("Inserting password salt into Keys table...", isError: false, debug: config.Debug);
                        string insertSaltQuery = "INSERT INTO Keys (Name, KeyValue) VALUES ('PasswordSalt', @Salt)";
                        using (SqlCommand command = new SqlCommand(insertSaltQuery, connection))
                        {
                            command.Parameters.AddWithValue("@Salt", salt);
                            command.ExecuteNonQuery();
                            Logger.Log("Password salt inserted successfully.", isError: false, debug: config.Debug);
                        }

                        Logger.Log("Generating random password...", isError: false, debug: config.Debug);
                        string password = GenerateRandomPassword();

                        Logger.Log("Hashing password with the generated salt...", isError: false, debug: config.Debug);
                        string hashedPassword = HashPassword(password, salt);

                        Logger.Log("Inserting default user with hashed password...", isError: false, debug: config.Debug);
                        string insertUserQuery = "INSERT INTO Users (Username, PasswordHash) VALUES ('ChipLogic', @PasswordHash)";
                        using (SqlCommand command = new SqlCommand(insertUserQuery, connection))
                        {
                            command.Parameters.AddWithValue("@PasswordHash", hashedPassword);
                            command.ExecuteNonQuery();
                            Logger.Log("Default user created successfully.", isError: false, debug: config.Debug);
                        }

                        Logger.Log("Saving default password to file...", isError: false, debug: config.Debug);
                        SavePasswordToFile(password);

                        Logger.Log("Updating configuration to indicate that the database has been created...", isError: false, debug: config.Debug);
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

        public static string GenerateSalt()
        {
            // Generate a cryptographic salt
            using (var rng = new System.Security.Cryptography.RNGCryptoServiceProvider())
            {
                byte[] saltBytes = new byte[16];
                rng.GetBytes(saltBytes);
                return Convert.ToBase64String(saltBytes);
            }
        }

        public static string HashPassword(string password, string salt)
        {
            using (var sha256 = System.Security.Cryptography.SHA256.Create())
            {
                byte[] saltedPassword = System.Text.Encoding.UTF8.GetBytes(password + salt);
                byte[] hashBytes = sha256.ComputeHash(saltedPassword);
                return Convert.ToBase64String(hashBytes);
            }
        }

        private static string GenerateRandomPassword()
        {
            const string validChars = "ABCDEFGHJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789!@#$%^&*()_-+=[{]}|;:<>/?";
            StringBuilder password = new StringBuilder();
            Random random = new Random();

            int length = random.Next(10, 13); // Password length between 10 and 12 characters
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
    }
}
