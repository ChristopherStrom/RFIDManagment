using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using ChipLogic.Configuration;
using ChipLogic.Database;
using ChipLogic.Utils;

namespace ChipLogic.Pages.Settings
{
    public partial class ManageUsersPage : Page
    {
        private DatabaseConfig config;

        public ManageUsersPage()
        {
            InitializeComponent();
            config = ConfigManager.LoadOrCreateConfig();  // Initialize the config variable
            LoadExistingUsers();
        }

        private void LoadExistingUsers()
        {
            try
            {
                var users = DBCommands.GetAllUsers(config.ConnectionString);
                ExistingUsersComboBox.ItemsSource = users;
                DeleteUserComboBox.ItemsSource = users;
                ChangePasswordUserComboBox.ItemsSource = users;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading users: {ex.Message}");
            }
        }

        private void OnUserSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            string selectedUsername = (string)ExistingUsersComboBox.SelectedItem;
            if (!string.IsNullOrEmpty(selectedUsername))
            {
                LoadUserPermissions(selectedUsername);
            }
        }

        private void LoadUserPermissions(string username)
        {
            var permissions = DBCommands.GetUserPermissions(username, config.ConnectionString);
            IsAdminCheckBox.IsChecked = permissions.IsAdmin;
            CanScanInCheckBox.IsChecked = permissions.CanScanIn;
            CanScanOutCheckBox.IsChecked = permissions.CanScanOut;
            CanAssignCheckBox.IsChecked = permissions.CanAssign;
            CanViewReportsCheckBox.IsChecked = permissions.CanViewReports;
        }

        private void SavePermissionsButton_Click(object sender, RoutedEventArgs e)
        {
            string selectedUsername = (string)ExistingUsersComboBox.SelectedItem;
            bool isAdmin = IsAdminCheckBox.IsChecked == true;
            bool canScanIn = CanScanInCheckBox.IsChecked == true;
            bool canScanOut = CanScanOutCheckBox.IsChecked == true;
            bool canAssign = CanAssignCheckBox.IsChecked == true;
            bool canViewReports = CanViewReportsCheckBox.IsChecked == true;

            DBCommands.UpdateUserPermissions(selectedUsername, isAdmin, canScanIn, canScanOut, canAssign, canViewReports, config.ConnectionString);
            MessageBox.Show("Permissions updated successfully.");
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
                DBCommands.CreateUser(username, password, config.ConnectionString);
                Logger.Log($"User {username} created successfully.", isError: false, debug: config.Debug);
                MessageBox.Show($"User {username} created successfully.");
                LoadExistingUsers(); // Reload users to reflect the new user
            }
            catch (Exception ex)
            {
                Logger.Log($"Error creating user: {ex.Message}", isError: true, debug: config.Debug);
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
                DBCommands.DeleteUser(username, config.ConnectionString);
                Logger.Log($"User {username} deleted successfully.", isError: false, debug: config.Debug);
                MessageBox.Show($"User {username} deleted successfully.");
                LoadExistingUsers(); // Reload users to reflect the deletion
            }
            catch (Exception ex)
            {
                Logger.Log($"Error deleting user: {ex.Message}", isError: true, debug: config.Debug);
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
                DBCommands.ChangePassword(username, newPassword, config.ConnectionString);
                Logger.Log($"Password for user {username} changed successfully.", isError: false, debug: config.Debug);
                MessageBox.Show($"Password for user {username} changed successfully.");
            }
            catch (Exception ex)
            {
                Logger.Log($"Error changing password: {ex.Message}", isError: true, debug: config.Debug);
                MessageBox.Show($"Error changing password: {ex.Message}");
            }
        }
    }
}
