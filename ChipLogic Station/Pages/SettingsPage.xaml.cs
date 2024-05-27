using System.Windows.Controls;
using ChipLogic.Pages.Settings.SettingsPages;

namespace ChipLogic.Pages.Settings
{
    public partial class SettingsPage : Page
    {
        public SettingsPage()
        {
            InitializeComponent();
        }

        private void ManageUsersButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            NavigationService?.Navigate(new ManageUsersPage());
        }

        private void ManageReadersButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            NavigationService?.Navigate(new ManageReadersPage());
        }

        private void ManageItemsButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            NavigationService?.Navigate(new ManageItemsPage());
        }

        private void ManageCustomersButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            NavigationService?.Navigate(new ManageCustomersPage());
        }
    }
}
