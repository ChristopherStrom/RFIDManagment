using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using ChipLogic.Utils;

namespace ChipLogic.Database
{
    public class UserPermissions
    {
        public bool IsAdmin { get; set; }
        public bool CanScanIn { get; set; }
        public bool CanScanOut { get; set; }
        public bool CanAssign { get; set; }
        public bool CanViewReports { get; set; }
    }

    public static class DBCommands
    {
        public static void CreateUser(string username, string password, string connectionString)
        {
            string salt = DatabaseInitializer.GetStoredSalt(connectionString);
            string hashedPassword = DatabaseInitializer.HashPassword(password, salt);

            Logger.Log($"Creating user {username}. Using stored salt: {salt}, Hashed password: {hashedPassword}", isError: false);

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();

                string query = "INSERT INTO Users (Username, PasswordHash) VALUES (@Username, @PasswordHash);" +
                               "INSERT INTO UserPermissions (UserID, IsAdmin, CanScanIn, CanScanOut, CanAssign, CanViewReports) " +
                               "VALUES ((SELECT UserID FROM Users WHERE Username = @Username), 0, 0, 0, 0, 0);";

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

                // Delete UserPermissions first
                string deletePermissionsQuery = "DELETE FROM UserPermissions WHERE UserID = (SELECT UserID FROM Users WHERE Username = @Username);";

                using (SqlCommand deletePermissionsCommand = new SqlCommand(deletePermissionsQuery, connection))
                {
                    deletePermissionsCommand.Parameters.AddWithValue("@Username", username);
                    deletePermissionsCommand.ExecuteNonQuery();
                }

                // Then delete the user
                string deleteUserQuery = "DELETE FROM Users WHERE Username = @Username;";

                using (SqlCommand deleteUserCommand = new SqlCommand(deleteUserQuery, connection))
                {
                    deleteUserCommand.Parameters.AddWithValue("@Username", username);
                    deleteUserCommand.ExecuteNonQuery();
                }
            }

            Logger.Log($"User {username} deleted successfully.", isError: false);
        }

        public static void ChangePassword(string username, string newPassword, string connectionString)
        {
            string salt = DatabaseInitializer.GetStoredSalt(connectionString);
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
                                string storedSalt = DatabaseInitializer.GetStoredSalt(connectionString);
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

        public static void UpdateUserPermissions(string username, bool isAdmin, bool canScanIn, bool canScanOut, bool canAssign, bool canViewReports, string connectionString)
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();

                string query = "UPDATE UserPermissions SET IsAdmin = @IsAdmin, CanScanIn = @CanScanIn, CanScanOut = @CanScanOut, CanAssign = @CanAssign, CanViewReports = @CanViewReports " +
                               "WHERE UserID = (SELECT UserID FROM Users WHERE Username = @Username);";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Username", username);
                    command.Parameters.AddWithValue("@IsAdmin", isAdmin);
                    command.Parameters.AddWithValue("@CanScanIn", canScanIn);
                    command.Parameters.AddWithValue("@CanScanOut", canScanOut);
                    command.Parameters.AddWithValue("@CanAssign", canAssign);
                    command.Parameters.AddWithValue("@CanViewReports", canViewReports);

                    command.ExecuteNonQuery();
                }
            }
        }

        public static (bool IsAdmin, bool CanScanIn, bool CanScanOut, bool CanAssign, bool CanViewReports) GetUserPermissions(string username, string connectionString)
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();

                string query = "SELECT IsAdmin, CanScanIn, CanScanOut, CanAssign, CanViewReports FROM UserPermissions " +
                               "WHERE UserID = (SELECT UserID FROM Users WHERE Username = @Username);";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Username", username);

                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            bool isAdmin = Convert.ToBoolean(reader["IsAdmin"]);
                            bool canScanIn = Convert.ToBoolean(reader["CanScanIn"]);
                            bool canScanOut = Convert.ToBoolean(reader["CanScanOut"]);
                            bool canAssign = Convert.ToBoolean(reader["CanAssign"]);
                            bool canViewReports = Convert.ToBoolean(reader["CanViewReports"]);

                            return (isAdmin, canScanIn, canScanOut, canAssign, canViewReports);
                        }
                        else
                        {
                            Logger.Log($"Permissions for user {username} not found.", isError: true);
                            return (false, false, false, false, false); // Return default permissions if not found
                        }
                    }
                }
            }
        }

        public static List<string> GetAllUsers(string connectionString)
        {
            List<string> users = new List<string>();

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();

                string query = "SELECT Username FROM Users";
                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            users.Add(reader["Username"].ToString());
                        }
                    }
                }
            }

            return users;
        }
    }
}
