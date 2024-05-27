using System;
using System.Data.SqlClient;
using ChipLogic.Utils;

namespace ChipLogic.Database
{
    public static class DBCommands
    {
        public static void CreateUser(string username, string password, string connectionString)
        {
            string salt = GetStoredSalt(connectionString);
            string hashedPassword = DatabaseInitializer.HashPassword(password, salt);

            Logger.Log($"Creating user {username}. Using stored salt: {salt}, Hashed password: {hashedPassword}", isError: false);

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();

                string query = "INSERT INTO Users (Username, PasswordHash) VALUES (@Username, @PasswordHash);";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Username", username);
                    command.Parameters.AddWithValue("@PasswordHash", hashedPassword);

                    command.ExecuteNonQuery();
                }
            }

            Logger.Log($"User {username} created successfully with hashed password {hashedPassword}.", isError: false);
        }

        public static void DeleteUser(string username, string connectionString)
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();

                string query = "DELETE FROM Users WHERE Username = @Username;";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Username", username);
                    command.ExecuteNonQuery();
                }
            }
        }

        public static void ChangePassword(string username, string newPassword, string connectionString)
        {
            string salt = GetStoredSalt(connectionString);
            string newHashedPassword = DatabaseInitializer.HashPassword(newPassword, salt);

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();

                string query = "UPDATE Users SET PasswordHash = @NewPasswordHash WHERE Username = @Username;";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Username", username);
                    command.Parameters.AddWithValue("@NewPasswordHash", newHashedPassword);

                    command.ExecuteNonQuery();
                }
            }
        }

        private static string GetStoredSalt(string connectionString)
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

        public static bool ValidateUser(string username, string password, string connectionString)
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();

                    string query = "SELECT U.PasswordHash FROM Users U WHERE U.Username = @Username";
                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@Username", username);

                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                string storedHash = reader["PasswordHash"].ToString();
                                string storedSalt = GetStoredSalt(connectionString);
                                string hash = DatabaseInitializer.HashPassword(password, storedSalt);

                                Logger.Log($"Validating user {username}. Stored hash: {storedHash}, Stored salt: {storedSalt}, Computed hash: {hash}", isError: false);

                                return hash == storedHash;
                            }
                            else
                            {
                                Logger.Log($"User {username} not found in the database.", isError: true);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"Error validating user: {ex.Message}", isError: true);
            }

            return false;
        }
    }
}
