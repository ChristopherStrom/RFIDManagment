using System;
using CommandLine;
using ChipLogic.Configuration;
using ChipLogic.Database;
using ChipLogic.Utils;
using System.Collections.Generic;
using System.Data.SqlClient;

namespace Support
{
    class Program
    {
        public class Options
        {
            [Option('n', "name", Required = true, HelpText = "Username to add.")]
            public string Username { get; set; }

            [Option('p', "pass", Required = true, HelpText = "Password for the new user.")]
            public string Password { get; set; }
        }

        static void Main(string[] args)
        {
            Parser.Default.ParseArguments<Options>(args)
                .WithParsed<Options>(opts => AddUser(opts))
                .WithNotParsed<Options>((errs) => HandleParseError(errs));
        }

        static void AddUser(Options opts)
        {
            try
            {
                // Load the configuration
                DatabaseConfig config = ConfigManager.LoadOrCreateConfig();

                // Use the CreateUser method from the ChipLogic.Database.DBCommands class
                DBCommands.CreateUser(opts.Username, opts.Password, config.ConnectionString);

                Console.WriteLine($"User {opts.Username} added successfully.");
                MakeUserAdmin(opts.Username,config.ConnectionString);
            }
            catch (SqlException ex)
            {
                Console.WriteLine($"SQL error: {ex.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error adding user: {ex.Message}");
            }
        }

        public static void MakeUserAdmin(string username, string connectionString)
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();

                string query = "UPDATE UserPermissions SET IsAdmin = @IsAdmin, CanScanIn = @CanScanIn, CanScanOut = @CanScanOut, CanAssign = @CanAssign, CanViewReports = @CanViewReports " +
                               "WHERE UserID = (SELECT UserID FROM Users WHERE Username = @Username);";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Username", username);
                    command.Parameters.AddWithValue("@IsAdmin", 1);
                    command.Parameters.AddWithValue("@CanScanIn", 1);
                    command.Parameters.AddWithValue("@CanScanOut", 1);
                    command.Parameters.AddWithValue("@CanAssign", 1);
                    command.Parameters.AddWithValue("@CanViewReports", 1);

                    command.ExecuteNonQuery();
                }
            }
        }

        static void HandleParseError(IEnumerable<Error> errs)
        {
            // Handle errors here
        }
    }
}
