using System.Windows.Controls;

namespace ChipLogic.Pages
{
    public partial class WelcomePage : Page
    {
        public WelcomePage()
        {
            InitializeComponent();
            VersionTextBlock.Text = $"Version {GlobalConstants.CurrentVersion}";
        }
    }
}
