using System.Windows;
using System.Windows.Media;
using System.Windows.Shapes;
using EngineeringOnboardingApp.Services;

namespace EngineeringOnboardingApp
{
    public partial class RoleSelectionDialog : Window
    {
        private readonly AppSession _session = AppSession.Shared;
        private bool _isContractor = true;

        public RoleSelectionDialog()
        {
            InitializeComponent();
            SelectContractor();
        }

        private void SelectContractor()
        {
            _isContractor = true;
            SetSelection(ContractorRadio, ContractorBox, ContractorTitle, true);
            SetSelection(EmployeeRadio, EmployeeBox, EmployeeTitle, false);
        }

        private void SelectEmployee()
        {
            _isContractor = false;
            SetSelection(ContractorRadio, ContractorBox, ContractorTitle, false);
            SetSelection(EmployeeRadio, EmployeeBox, EmployeeTitle, true);
        }

        private static void SetSelection(Ellipse radio, System.Windows.Controls.Button box, System.Windows.Controls.TextBlock title, bool selected)
        {
            radio.Fill = selected
                ? new SolidColorBrush(Color.FromRgb(0x22, 0xC5, 0x5E))
                : Brushes.Transparent;
            box.BorderBrush = new SolidColorBrush(selected
                ? Color.FromRgb(0x22, 0xC5, 0x5E)
                : Color.FromRgb(0x26, 0x2B, 0x36));
            box.Background = selected
                ? new SolidColorBrush(Color.FromArgb(0x12, 0x22, 0xC5, 0x5E))
                : Brushes.Transparent;
            title.Foreground = new SolidColorBrush(Color.FromRgb(0xEC, 0xEE, 0xF4));
        }

        private void ContractorBox_Click(object sender, RoutedEventArgs e) => SelectContractor();
        private void EmployeeBox_Click(object sender, RoutedEventArgs e) => SelectEmployee();

        private void Confirm_Click(object sender, RoutedEventArgs e)
        {
            _session.SetRole(_isContractor ? "contractor" : "employee");
            DialogResult = true;
            Close();
        }
    }
}
