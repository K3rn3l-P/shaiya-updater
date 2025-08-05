using System.Diagnostics;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Navigation;

namespace Updater
{
    public partial class CustomDialogWindow : Window
    {
        public bool IsOk { get; private set; } = false;
        public CustomDialogWindow(string title, string message, string? secondary = null, string? hyperlinkText = null, string? hyperlinkUrl = null, bool showCancel = false)
        {
            InitializeComponent();
            TitleBlock.Text = title;
            MessageBlock.Text = message;
            if (!string.IsNullOrWhiteSpace(secondary))
            {
                SecondaryBlock.Text = secondary;
                SecondaryBlock.Visibility = Visibility.Visible;
            }
            if (!string.IsNullOrWhiteSpace(hyperlinkText) && !string.IsNullOrWhiteSpace(hyperlinkUrl))
            {
                DialogHyperlink.Inlines.Clear();
                DialogHyperlink.Inlines.Add(hyperlinkText);
                DialogHyperlink.NavigateUri = new System.Uri(hyperlinkUrl);
                HyperlinkBlock.Visibility = Visibility.Visible;
            }
            CancelButton.Visibility = showCancel ? Visibility.Visible : Visibility.Collapsed;
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            IsOk = true;
            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            IsOk = false;
            DialogResult = false;
            Close();
        }

        private void DialogHyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
            e.Handled = true;
        }
    }
}
