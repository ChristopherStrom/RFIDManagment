using System;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Data.SqlClient;
using ChipLogic.Configuration;
using ChipLogic.Utils;

namespace ChipLogic.Database
{
    public class DatabaseInitializer
    {
        public static bool CreateDatabase(DatabaseConfig config)
        {
            if (config.IsDatabaseCreated)
            {
                // Attempt to connect to verify that the connection string is valid
                try
                {
                    using (SqlConnection connection = new SqlConnection(config.ConnectionString))
                    {
                        connection.Open();
                    }
                    Logger.Log("Connection to the database was successful.", isError: false, debug: config.Debug);
                    return true;
                }
                catch (Exception ex)
                {
                    Logger.Log($"Error connecting to the database: {ex.Message}");
                    return false;
                }
            }

            // Parse the connection string to get the server and database names
            var connectionStringBuilder = new SqlConnectionStringBuilder(config.ConnectionString);
            string initialCatalog = connectionStringBuilder.InitialCatalog;

            // Create a connection string without the InitialCatalog to connect to the server
            connectionStringBuilder.InitialCatalog = string.Empty;

            try
            {
                using (SqlConnection connection = new SqlConnection(connectionStringBuilder.ToString()))
                {
                    connection.Open();

                    // Check if the database exists
                    string checkDbQuery = $"SELECT COUNT(*) FROM sys.databases WHERE name = '{initialCatalog}'";

                    using (SqlCommand command = new SqlCommand(checkDbQuery, connection))
                    {
                        int databaseCount = (int)command.ExecuteScalar();
                        if (databaseCount > 0)
                        {
                            Logger.Log($"Database '{initialCatalog}' already exists. No new database created.");
                            return false;
                        }
                    }

                    // Create the database if it doesn't exist
                    string createDbQuery = $"CREATE DATABASE [{initialCatalog}]";

                    using (SqlCommand command = new SqlCommand(createDbQuery, connection))
                    {
                        command.ExecuteNonQuery();
                    }

                    // Now connect to the new database and create tables if necessary
                    connection.ChangeDatabase(initialCatalog);

                    // Create tables and seed default data
                    string createTableQuery = @"
                        CREATE TABLE Users (
                            Id INT PRIMARY KEY IDENTITY,
                            Username NVARCHAR(50) UNIQUE,
                            PasswordHash NVARCHAR(256),
                            Salt NVARCHAR(256)
                        );
                        CREATE TABLE Keys (
                            Id INT PRIMARY KEY IDENTITY,
                            Name NVARCHAR(50) UNIQUE,
                            Value NVARCHAR(256)
                        )";

                    using (SqlCommand command = new SqlCommand(createTableQuery, connection))
                    {
                        command.ExecuteNonQuery();
                    }

                    // Seed default user
                    SeedDefaultUser(connection);

                    // Update config to mark the database as created
                    config.IsDatabaseCreated = true;
                    ConfigManager.UpdateConfig(config);

                    Logger.Log("Database created and initialized successfully.", isError: false, debug: config.Debug);
                    return true;
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"Error creating or initializing the database: {ex.Message}");
                return false;
            }
        }

        private static void SeedDefaultUser(SqlConnection connection)
        {
            string defaultUsername = "ChipLogic";
            string defaultPassword = "Bullet99";

            // Generate a random salt
            byte[] saltBytes = new byte[16];
            using (var rng = new RNGCryptoServiceProvider())
            {
                rng.GetBytes(saltBytes);
            }
            string salt = Convert.ToBase64String(saltBytes);

            // Hash the password with the salt
            string passwordHash = HashPassword(defaultPassword, salt);

            // Insert the default user
            string insertUserQuery = "INSERT INTO Users (Username, PasswordHash, Salt) VALUES (@Username, @PasswordHash, @Salt)";
            using (SqlCommand command = new SqlCommand(insertUserQuery, connection))
            {
                command.Parameters.AddWithValue("@Username", defaultUsername);
                command.Parameters.AddWithValue("@PasswordHash", passwordHash);
                command.Parameters.AddWithValue("@Salt", salt);
                command.ExecuteNonQuery();
            }

            // Store the salt in the Keys table
            string insertSaltQuery = "INSERT INTO Keys (Name, Value) VALUES ('UserSalt', @Salt)";
            using (SqlCommand command = new SqlCommand(insertSaltQuery, connection))
            {
                command.Parameters.AddWithValue("@Salt", salt);
                command.ExecuteNonQuery();
            }
        }

        public static string HashPassword(string password, string salt)
        {
            using (var sha256 = SHA256.Create())
            {
                var saltedPassword = password + salt;
                byte[] saltedPasswordBytes = Encoding.UTF8.GetBytes(saltedPassword);
                byte[] hashBytes = sha256.ComputeHash(saltedPasswordBytes);
                return Convert.ToBase64String(hashBytes);
            }
        }
    }
}
