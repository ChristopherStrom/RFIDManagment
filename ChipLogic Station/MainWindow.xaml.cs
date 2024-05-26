using System;
using System.Data.SqlClient;
using System.Security.Cryptography;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using ChipLogic.Configuration;
using ChipLogic.Database;
using ChipLogic.Pages;
using ChipLogic.Utils;

namespace ChipLogic
{
    public partial class MainWindow : Window
    {
        private bool isLoggedIn = false;
        private bool debug;

        public MainWindow()
        {
            InitializeComponent();

            #region Database Initialization

            // Create or connect to DB
            bool isDatabaseConnected = false;
            try
            {
                DatabaseConfig config = ConfigManager.LoadOrCreateConfig();
                debug = config.Debug;  // Set the debug flag based on the config file

                isDatabaseConnected = DatabaseInitializer.CreateDatabase(config);

                if (!isDatabaseConnected && !config.IsDatabaseCreated)
                {
                    Logger.Log("Database could not be created because it already exists.");
                    SetDatabaseStatus(false);
                }
                else if (isDatabaseConnected)
                {
                    Logger.Log("Database initialization checked successfully.", isError: false, debug: config.Debug);
                    SetDatabaseStatus(true);
                }
                else
                {
                    SetDatabaseStatus(false);
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"Error initializing database: {ex.Message}");
                SetDatabaseStatus(false);
            }

            #endregion
        }

        private void SetDatabaseStatus(bool isConnected)
        {
            Dispatcher.Invoke(() =>
            {
                DbStatusTextBlock.Text = isConnected ? "Database connected" : "Database not connected";
                DbStatusTextBlock.Foreground = isConnected ? System.Windows.Media.Brushes.Green : System.Windows.Media.Brushes.Red;
            });
        }

        private void ShutdownApplication()
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                Application.Current.Shutdown();
            });
        }

        private void PerformLogin_Click(object sender, RoutedEventArgs e)
        {
            string username = UsernameTextBox.Text;
            string password = PasswordBox.Password;

            if (ValidateUser(username, password))
            {
                isLoggedIn = true;
                Logger.Log("Login successful.", isError: false, debug: debug);
                UpdateLoginState();
            }
            else
            {
                MessageBox.Show("Invalid username or password.");
                Logger.Log("Invalid username or password.", isError: true, debug: debug);
            }
        }

        private void PerformLogout_Click(object sender, RoutedEventArgs e)
        {
            isLoggedIn = false;
            Logger.Log("Logout successful.", isError: false, debug: debug);
            UpdateLoginState();
        }

        private void UpdateLoginState()
        {
            Logger.Log("Updating login state.", isError: false, debug: debug);
            if (isLoggedIn)
            {
                Logger.Log("User logged in. Updating UI to logged-in state.", isError: false, debug: debug);

                LoginPanel.Visibility = Visibility.Collapsed;
                MainFrame.Visibility = Visibility.Visible;

                AssignButton.Visibility = Visibility.Visible;
                ScanInButton.Visibility = Visibility.Visible;
                ScanOutButton.Visibility = Visibility.Visible;
                SetupButton.Visibility = Visibility.Visible;
                LogoutButton.Visibility = Visibility.Visible;
                LoggedInUserText.Visibility = Visibility.Visible;

                LoginButton.Visibility = Visibility.Collapsed;

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

                LoginButton.Visibility = Visibility.Visible;

                UsernameTextBox.Text = "Username";
                UsernameTextBox.Foreground = System.Windows.Media.Brushes.Gray;
                PasswordBox.Password = "Password";
                PasswordBox.Foreground = System.Windows.Media.Brushes.Gray;
            }
        }

        private bool ValidateUser(string username, string password)
        {
            try
            {
                DatabaseConfig config = ConfigManager.LoadOrCreateConfig();
                using (SqlConnection connection = new SqlConnection(config.ConnectionString))
                {
                    connection.Open();

                    string query = "SELECT PasswordHash, Salt FROM Users WHERE Username = @Username";
                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@Username", username);
                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                string storedHash = reader["PasswordHash"].ToString();
                                string storedSalt = reader["Salt"].ToString();
                                string hash = DatabaseInitializer.HashPassword(password, storedSalt);

                                return hash == storedHash;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"Error validating user: {ex.Message}", isError: true, debug: debug);
            }

            return false;
        }

        private void RemovePlaceholderText(object sender, RoutedEventArgs e)
        {
            TextBox textBox = sender as TextBox;
            if (textBox.Text == "Username")
            {
                textBox.Text = "";
                textBox.Foreground = System.Windows.Media.Brushes.Black;
            }
        }

        private void AddPlaceholderText(object sender, RoutedEventArgs e)
        {
            TextBox textBox = sender as TextBox;
            if (string.IsNullOrWhiteSpace(textBox.Text))
            {
                textBox.Text = "Username";
                textBox.Foreground = System.Windows.Media.Brushes.Gray;
            }
        }

        private void RemovePasswordPlaceholder(object sender, RoutedEventArgs e)
        {
            PasswordBox passwordBox = sender as PasswordBox;
            if (passwordBox.Password == "Password")
            {
                passwordBox.Password = "";
                passwordBox.Foreground = System.Windows.Media.Brushes.Black;
            }
        }

        private void AddPasswordPlaceholder(object sender, RoutedEventArgs e)
        {
            PasswordBox passwordBox = sender as PasswordBox;
            if (string.IsNullOrWhiteSpace(passwordBox.Password))
            {
                passwordBox.Password = "Password";
                passwordBox.Foreground = System.Windows.Media.Brushes.Gray;
            }
        }

        #region Title Bar Events

        private void TitleBar_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                this.DragMove();
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
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
