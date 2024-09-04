using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using ChipLogic.Configuration;
using ChipLogic.Database;
using System.Data.SqlClient;

namespace ChipLogicSupportApp
{
    public partial class MainWindow : Window
    {
        private readonly DatabaseConfig config;

        public MainWindow()
        {
            InitializeComponent();
            config = ConfigManager.LoadOrCreateConfig();
            LoadExistingUsers(); // Populate the user dropdown
        }

        private void LoadExistingUsers()
        {
            try
            {
                // Get all users from the database
                List<string> users = DBCommands.GetAllUsers(config.ConnectionString);
                ResetUserComboBox.ItemsSource = users; // Populate the dropdown
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading users: {ex.Message}");
            }
        }

        private void AddUserButton_Click(object sender, RoutedEventArgs e)
        {
            string username = UsernameTextBox.Text;
            string password = PasswordBox.Password;

            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                MessageBox.Show("Username and password cannot be empty.");
                return;
            }

            try
            {
                // Add user to the database
                DBCommands.CreateUser(username, password, config.ConnectionString);
                OutputTextBox.AppendText($"User {username} added successfully.\n");

                // Optionally make the user an admin
                MakeUserAdmin(username);
                OutputTextBox.AppendText($"User {username} was granted admin rights.\n");

                // Refresh user list in dropdown
                LoadExistingUsers();
            }
            catch (SqlException ex)
            {
                OutputTextBox.AppendText($"SQL error: {ex.Message}\n");
            }
            catch (Exception ex)
            {
                OutputTextBox.AppendText($"Error adding user: {ex.Message}\n");
            }
        }

        private void ResetPasswordButton_Click(object sender, RoutedEventArgs e)
        {
            if (ResetUserComboBox.SelectedItem == null)
            {
                MessageBox.Show("Please select a user.");
                return;
            }

            string username = ResetUserComboBox.SelectedItem.ToString();
            string newPassword = NewPasswordBox.Password;

            if (string.IsNullOrWhiteSpace(newPassword))
            {
                MessageBox.Show("New password cannot be empty.");
                return;
            }

            try
            {
                // Reset user password in the database
                DBCommands.ChangePassword(username, newPassword, config.ConnectionString);
                OutputTextBox.AppendText($"Password for {username} reset successfully.\n");
            }
            catch (SqlException ex)
            {
                OutputTextBox.AppendText($"SQL error: {ex.Message}\n");
            }
            catch (Exception ex)
            {
                OutputTextBox.AppendText($"Error resetting password: {ex.Message}\n");
            }
        }

        private void MakeUserAdmin(string username)
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(config.ConnectionString))
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
            catch (SqlException ex)
            {
                OutputTextBox.AppendText($"SQL error: {ex.Message}\n");
            }
            catch (Exception ex)
            {
                OutputTextBox.AppendText($"Error making user admin: {ex.Message}\n");
            }
        }
    }
}
