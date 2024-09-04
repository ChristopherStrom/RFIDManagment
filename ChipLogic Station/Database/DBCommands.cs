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

                string query = "INSERT INTO Users (Username, PasswordHash, isActive) VALUES (@Username, @PasswordHash, 1);" +
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

                string query = "DELETE FROM UserPermissions WHERE UserID = (SELECT UserID FROM Users WHERE Username = @Username);" +
                               "DELETE FROM Users WHERE Username = @Username;";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Username", username);
                    command.ExecuteNonQuery();
                }
            }
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

                    string query = "SELECT U.PasswordHash, U.isActive FROM Users U WHERE U.Username = @Username";
                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@Username", username);

                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                bool isActive = Convert.ToBoolean(reader["isActive"]);
                                if (!isActive)
                                {
                                    Logger.Log($"User {username} is not active.", isError: true);
                                    return false;
                                }

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


        public static void AddCustomer(string customerNumber, string companyName, string contactName, string contactPhone, string contactEmail, string customerAddress, bool isActive, string connectionString)
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                string query = @"INSERT INTO Customers (CustomerNumber, CompanyName, ContactName, ContactPhone, ContactEmail, CustomerAddress, IsActive)
                         VALUES (@CustomerNumber, @CompanyName, @ContactName, @ContactPhone, @ContactEmail, @CustomerAddress, @IsActive)";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@CustomerNumber", customerNumber);
                    command.Parameters.AddWithValue("@CompanyName", companyName);
                    command.Parameters.AddWithValue("@ContactName", contactName);
                    command.Parameters.AddWithValue("@ContactPhone", contactPhone);
                    command.Parameters.AddWithValue("@ContactEmail", contactEmail);
                    command.Parameters.AddWithValue("@CustomerAddress", customerAddress);
                    command.Parameters.AddWithValue("@IsActive", isActive);

                    try
                    {
                        connection.Open();
                        command.ExecuteNonQuery();
                    }
                    catch (SqlException sqlEx)
                    {
                        Logger.Log($"SQL error during customer insertion: {sqlEx.Message}", isError: true, debug: true);
                        throw;
                    }
                }
            }
        }


        public static void DeleteCustomer(string customerNumber, string connectionString)
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                string query = "DELETE FROM Customers WHERE CustomerNumber = @CustomerNumber";
                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@CustomerNumber", customerNumber);
                    command.ExecuteNonQuery();
                }
            }
        }

        public static void UpdateCustomer(string customerNumber, string companyName, string contactName, string contactPhone, string contactEmail, string customerAddress, bool isActive, string connectionString)
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                string query = "UPDATE Customers SET CompanyName = @CompanyName, ContactName = @ContactName, ContactPhone = @ContactPhone, " +
                               "ContactEmail = @ContactEmail, CustomerAddress = @CustomerAddress, IsActive = @IsActive WHERE CustomerNumber = @CustomerNumber";
                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@CustomerNumber", customerNumber);
                    command.Parameters.AddWithValue("@CompanyName", companyName);
                    command.Parameters.AddWithValue("@ContactName", contactName);
                    command.Parameters.AddWithValue("@ContactPhone", contactPhone);
                    command.Parameters.AddWithValue("@ContactEmail", contactEmail);
                    command.Parameters.AddWithValue("@CustomerAddress", customerAddress);
                    command.Parameters.AddWithValue("@IsActive", isActive);
                    command.ExecuteNonQuery();
                }
            }
        }

        public static List<string> GetAllCustomers(string connectionString)
        {
            List<string> customers = new List<string>();
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                string query = "SELECT CustomerNumber FROM Customers";
                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            customers.Add(reader["CustomerNumber"].ToString());
                        }
                    }
                }
            }
            return customers;
        }

        public static (string CustomerNumber, string CompanyName, string ContactName, string ContactPhone, string ContactEmail, string CustomerAddress, bool IsActive) GetCustomerDetails(string customerNumber, string connectionString)
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                string query = "SELECT CustomerNumber, CompanyName, ContactName, ContactPhone, ContactEmail, CustomerAddress, IsActive FROM Customers WHERE CustomerNumber = @CustomerNumber";
                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@CustomerNumber", customerNumber);
                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            string companyName = reader["CompanyName"].ToString();
                            string contactName = reader["ContactName"].ToString();
                            string contactPhone = reader["ContactPhone"].ToString();
                            string contactEmail = reader["ContactEmail"].ToString();
                            string customerAddress = reader["CustomerAddress"].ToString();
                            bool isActive = Convert.ToBoolean(reader["IsActive"]);
                            return (customerNumber, companyName, contactName, contactPhone, contactEmail, customerAddress, isActive);
                        }
                    }
                }
            }
            return (null, null, null, null, null, null, false);
        }

        public static bool CheckCustomerNumberExists(string customerNumber, string connectionString)
        {
            bool exists = false;

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                string query = "SELECT COUNT(*) FROM Customers WHERE CustomerNumber = @CustomerNumber";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@CustomerNumber", customerNumber);

                    try
                    {
                        connection.Open();
                        exists = (int)command.ExecuteScalar() > 0;
                    }
                    catch (Exception ex)
                    {
                        Logger.Log($"Error checking customer number: {ex.Message}", isError: true, debug: true);
                    }
                }
            }

            return exists;
        }

    }
}
