using System;
using System.Data.SqlClient;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using ChipLogic.Configuration;
using ChipLogic.Database;
using ChipLogic.Pages;
using ChipLogic.Pages.Settings;
using ChipLogic.Utils;

namespace ChipLogic
{
    public partial class MainWindow : Window
    {
        private bool isLoggedIn = false;
        private bool debug;
        private bool isMaximized = false;
        private DatabaseConfig config;

        public MainWindow()
        {
            InitializeComponent();

            #region Database Initialization

            // Load configuration
            config = ConfigManager.LoadOrCreateConfig();
            debug = config.Debug;

            if (!ConfigManager.ValidateConfig(config))
            {
                Logger.Log("Configuration is invalid. Please check the config file.", isError: true, debug: debug);
                SetDatabaseStatus(false);
                return;
            }

            bool isDatabaseConnected = false;

            if (config.IsDatabaseCreated)
            {
                isDatabaseConnected = DatabaseInitializer.CheckDatabaseConnection(config);

                if (!isDatabaseConnected)
                {
                    Logger.Log("Database connection failed.", isError: true, debug: debug);
                    SetDatabaseStatus(false);
                }
                else
                {
                    DatabaseInitializer.CheckAndUpdateDatabase(config, GlobalConstants.CurrentVersion);
                    Logger.Log("Database connection successful.", isError: false, debug: debug);
                    SetDatabaseStatus(true);
                }
            }
            else
            {
                isDatabaseConnected = DatabaseInitializer.CreateDatabaseAndSeed(config);

                if (!isDatabaseConnected)
                {
                    Logger.Log("Database creation and seeding failed.", isError: true, debug: debug);
                    SetDatabaseStatus(false);
                }
                else
                {
                    Logger.Log("Database created and seeded successfully.", isError: false, debug: debug);
                    SetDatabaseStatus(true);
                }
            }
            #endregion

            // Navigate to WelcomePage on application start
            MainFrame.Navigate(new WelcomePage());
        }

        private void SetDatabaseStatus(bool isConnected)
        {
            Dispatcher.Invoke(() =>
            {
                DbStatusTextBlock.Text = isConnected ? "Database connected" : "Database not connected";
                DbStatusTextBlock.Foreground = isConnected ? Brushes.Green : Brushes.Red;
            });
        }

        private void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            string username = UsernameTextBox.Text;
            string password = PasswordBox.Password;

            if (DBCommands.ValidateUser(username, password, config.ConnectionString))
            {
                isLoggedIn = true;
                Logger.Log("Login successful.", isError: false, debug: debug);

                var permissions = DBCommands.GetUserPermissions(username, config.ConnectionString);
                UpdateLoginState(permissions);

                // Navigate to WelcomePage after login
                MainFrame.Navigate(new WelcomePage());
            }
            else
            {
                MessageBox.Show("Invalid username or password.");
                Logger.Log("Invalid username or password.", isError: true, debug: debug);
            }
        }

        private void UsernameTextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            if (UsernameTextBox.Text == "Username")
            {
                UsernameTextBox.Text = "";
                UsernameTextBox.Foreground = new SolidColorBrush(Colors.Black);
            }
        }

        private void UsernameTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(UsernameTextBox.Text))
            {
                UsernameTextBox.Text = "Username";
                UsernameTextBox.Foreground = new SolidColorBrush(Colors.Gray);
            }
        }

        private void PasswordBox_GotFocus(object sender, RoutedEventArgs e)
        {
            if (PasswordBox.Tag?.ToString() == "Placeholder")
            {
                PasswordBox.Password = "";
                PasswordBox.Foreground = new SolidColorBrush(Colors.Black);
                PasswordBox.Tag = null;
            }
        }

        private void PasswordBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(PasswordBox.Password))
            {
                PasswordBox.Password = "********";
                PasswordBox.Foreground = new SolidColorBrush(Colors.Gray);
                PasswordBox.Tag = "Placeholder";
            }
        }

        private void PasswordBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                LoginButton_Click(sender, e);
            }
        }

        private void PerformLogout_Click(object sender, RoutedEventArgs e)
        {
            isLoggedIn = false;
            Logger.Log("Logout successful.", isError: false, debug: debug);

            var defaultPermissions = (IsAdmin: false, CanScanIn: false, CanScanOut: false, CanAssign: false, CanViewReports: false);
            UpdateLoginState(defaultPermissions);

            MainFrame.Navigate(new WelcomePage());
        }

        private void UpdateLoginState((bool IsAdmin, bool CanScanIn, bool CanScanOut, bool CanAssign, bool CanViewReports) permissions)
        {
            Logger.Log("Updating login state.", isError: false, debug: debug);
            if (isLoggedIn)
            {
                Logger.Log("User logged in. Updating UI to logged-in state.", isError: false, debug: debug);

                LoginPanel.Visibility = Visibility.Collapsed;
                MainFrame.Visibility = Visibility.Visible;

                AssignButton.Visibility = permissions.CanAssign ? Visibility.Visible : Visibility.Collapsed;
                ScanInButton.Visibility = permissions.CanScanIn ? Visibility.Visible : Visibility.Collapsed;
                ScanOutButton.Visibility = permissions.CanScanOut ? Visibility.Visible : Visibility.Collapsed;
                SetupButton.Visibility = permissions.IsAdmin ? Visibility.Visible : Visibility.Collapsed;
                LogoutButton.Visibility = Visibility.Visible;
                LoggedInUserText.Visibility = Visibility.Visible;

                LoggedInUserText.Text = $"Logged in as: {UsernameTextBox.Text}";
            }
            else
            {
                Logger.Log("User logged out. Updating UI to logged-out state.", isError: false, debug: debug);

                LoginPanel.Visibility = Visibility.Visible;
                MainFrame.Visibility = Visibility.Collapsed;

                AssignButton.Visibility = Visibility.Collapsed;
                ScanInButton.Visibility = Visibility.Collapsed;
                ScanOutButton.Visibility = Visibility.Collapsed;
                SetupButton.Visibility = Visibility.Collapsed;
                LogoutButton.Visibility = Visibility.Collapsed;
                LoggedInUserText.Visibility = Visibility.Collapsed;

                UsernameTextBox.Text = "Username";
                UsernameTextBox.Foreground = Brushes.Gray;
                PasswordBox.Password = "********";
                PasswordBox.Tag = "Placeholder";
                PasswordBox.Foreground = Brushes.Gray;
            }
        }

        private void RemovePlaceholderText(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox textBox)
            {
                if (textBox.Text == "Username")
                {
                    textBox.Text = "";
                    textBox.Foreground = Brushes.Black;
                }
            }
            else if (sender is PasswordBox passwordBox)
            {
                if ((string)passwordBox.Tag == "Placeholder")
                {
                    passwordBox.Password = "";
                    passwordBox.Foreground = Brushes.Black;
                    passwordBox.Tag = null;
                }
            }
        }

        private void AddPlaceholderText(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox textBox && string.IsNullOrWhiteSpace(textBox.Text))
            {
                if (textBox.Name == "UsernameTextBox")
                {
                    textBox.Text = "Username";
                    textBox.Foreground = Brushes.Gray;
                }
            }
            else if (sender is PasswordBox passwordBox && string.IsNullOrWhiteSpace(passwordBox.Password))
            {
                if (passwordBox.Name == "PasswordBox")
                {
                    passwordBox.Password = "********";
                    passwordBox.Tag = "Placeholder";
                    passwordBox.Foreground = Brushes.Gray;
                }
            }
        }

        #region Title Bar Events

        private void TitleBar_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
            {
                ToggleWindowState();
            }
            else if (e.ChangedButton == MouseButton.Left)
            {
                this.DragMove();
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void MaximizeRestoreButton_Click(object sender, RoutedEventArgs e)
        {
            ToggleWindowState();
        }

        private void ToggleWindowState()
        {
            if (isMaximized)
            {
                this.WindowState = WindowState.Normal;
                this.ResizeMode = ResizeMode.CanResize;
                this.BorderThickness = new Thickness(1);
                isMaximized = false;
            }
            else
            {
                this.WindowState = WindowState.Maximized;
                this.ResizeMode = ResizeMode.NoResize;
                this.BorderThickness = new Thickness(0);
                isMaximized = true;
            }
        }

        #endregion

        #region Window Resize Event

        private void ResizeThumb_DragDelta(object sender, DragDeltaEventArgs e)
        {
            if (this.Width + e.HorizontalChange > this.MinWidth)
            {
                this.Width += e.HorizontalChange;
            }
            if (this.Height + e.VerticalChange > this.MinHeight)
            {
                this.Height += e.VerticalChange;
            }
        }

        #endregion

        #region Navigation Events

        private void AssignButton_Click(object sender, RoutedEventArgs e)
        {
            MainFrame.Navigate(new AssignPage());
        }

        private void ScanInButton_Click(object sender, RoutedEventArgs e)
        {
            MainFrame.Navigate(new ScanInPage());
        }

        private void ScanOutButton_Click(object sender, RoutedEventArgs e)
        {
            MainFrame.Navigate(new ScanOutPage());
        }

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            MainFrame.Navigate(new SettingsPage());
        }

        #endregion
    }
}
