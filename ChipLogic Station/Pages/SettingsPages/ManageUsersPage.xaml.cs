using System;
using System.Data.SqlClient; // Add this using directive
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using ChipLogic.Configuration;
using ChipLogic.Database;
using ChipLogic.Utils;

namespace ChipLogic.Pages.Settings.SettingsPages
{
    public partial class ManageUsersPage : Page
    {
        private bool debug;

        public ManageUsersPage()
        {
            InitializeComponent();
            DatabaseConfig config = ConfigManager.LoadOrCreateConfig();
            debug = config.Debug;
            LoadExistingUsers();
        }

        private void RemovePlaceholderText(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox textBox)
            {
                if (textBox.Text == "Username")
                {
                    textBox.Text = "";
                    textBox.Foreground = new SolidColorBrush(Colors.Black);
                }
            }
            else if (sender is PasswordBox passwordBox)
            {
                if ((string)passwordBox.Tag == "Placeholder")
                {
                    passwordBox.Password = "";
                    passwordBox.Foreground = new SolidColorBrush(Colors.Black);
                    passwordBox.Tag = null;
                }
            }
        }

        private void AddPlaceholderText(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox textBox && string.IsNullOrWhiteSpace(textBox.Text))
            {
                if (textBox.Name == "CreateUsernameTextBox")
                {
                    textBox.Text = "Username";
                    textBox.Foreground = new SolidColorBrush(Colors.Gray);
                }
            }
            else if (sender is PasswordBox passwordBox && string.IsNullOrWhiteSpace(passwordBox.Password))
            {
                if (passwordBox.Name == "CreatePasswordBox" || passwordBox.Name == "NewPasswordBox")
                {
                    passwordBox.Password = "Password";
                    passwordBox.Tag = "Placeholder";
                    passwordBox.Foreground = new SolidColorBrush(Colors.Gray);
                }
            }
        }

        private void CreateUserButton_Click(object sender, RoutedEventArgs e)
        {
            string username = CreateUsernameTextBox.Text;
            string password = CreatePasswordBox.Password;

            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                MessageBox.Show("Username and password cannot be empty.");
                return;
            }

            try
            {
                DatabaseConfig config = ConfigManager.LoadOrCreateConfig();
                DBCommands.CreateUser(username, password, config.ConnectionString);
                Logger.Log($"User {username} created successfully.", isError: false, debug: debug);
                MessageBox.Show($"User {username} created successfully.");
                LoadExistingUsers(); // Reload users to reflect the new user
            }
            catch (Exception ex)
            {
                Logger.Log($"Error creating user: {ex.Message}", isError: true, debug: debug);
                MessageBox.Show($"Error creating user: {ex.Message}");
            }
        }

        private void DeleteUserButton_Click(object sender, RoutedEventArgs e)
        {
            string username = DeleteUserComboBox.SelectedItem as string;

            if (string.IsNullOrWhiteSpace(username))
            {
                MessageBox.Show("Please select a user to delete.");
                return;
            }

            try
            {
                DatabaseConfig config = ConfigManager.LoadOrCreateConfig();
                DBCommands.DeleteUser(username, config.ConnectionString);
                Logger.Log($"User {username} deleted successfully.", isError: false, debug: debug);
                MessageBox.Show($"User {username} deleted successfully.");
                LoadExistingUsers(); // Reload users to reflect the deletion
            }
            catch (Exception ex)
            {
                Logger.Log($"Error deleting user: {ex.Message}", isError: true, debug: debug);
                MessageBox.Show($"Error deleting user: {ex.Message}");
            }
        }

        private void ChangePasswordButton_Click(object sender, RoutedEventArgs e)
        {
            string username = ChangePasswordUserComboBox.SelectedItem as string;
            string newPassword = NewPasswordBox.Password;

            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(newPassword))
            {
                MessageBox.Show("Please select a user and provide a new password.");
                return;
            }

            try
            {
                DatabaseConfig config = ConfigManager.LoadOrCreateConfig();
                DBCommands.ChangePassword(username, newPassword, config.ConnectionString);
                Logger.Log($"Password for user {username} changed successfully.", isError: false, debug: debug);
                MessageBox.Show($"Password for user {username} changed successfully.");
            }
            catch (Exception ex)
            {
                Logger.Log($"Error changing password: {ex.Message}", isError: true, debug: debug);
                MessageBox.Show($"Error changing password: {ex.Message}");
            }
        }

        private void SavePermissionsButton_Click(object sender, RoutedEventArgs e)
        {
            // Implement your permissions saving logic here
        }

        private void LoadExistingUsers()
        {
            try
            {
                DatabaseConfig config = ConfigManager.LoadOrCreateConfig();
                using (SqlConnection connection = new SqlConnection(config.ConnectionString))
                {
                    connection.Open();
                    string query = "SELECT Username FROM Users";
                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            DeleteUserComboBox.Items.Clear();
                            ChangePasswordUserComboBox.Items.Clear();
                            ModifyUserComboBox.Items.Clear();
                            while (reader.Read())
                            {
                                string username = reader["Username"].ToString();
                                DeleteUserComboBox.Items.Add(username);
                                ChangePasswordUserComboBox.Items.Add(username);
                                ModifyUserComboBox.Items.Add(username);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"Error loading existing users: {ex.Message}", isError: true, debug: debug);
                MessageBox.Show($"Error loading existing users: {ex.Message}");
            }
        }
    }
}
