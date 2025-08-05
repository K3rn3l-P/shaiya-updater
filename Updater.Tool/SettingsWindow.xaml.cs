using System.Windows;
using Updater.Tool.Services;

namespace Updater.Tool
{
    public partial class SettingsWindow : Window
    {
        private string? _selectedPatchDir;
        public string? SelectedPatchDir => _selectedPatchDir;

        public SettingsWindow(string? currentPatchDir)
        {
            InitializeComponent();
            _selectedPatchDir = currentPatchDir;
            PatchDirTextBox.Text = currentPatchDir ?? string.Empty;
        }

        private void ChangePatchDir_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new System.Windows.Forms.FolderBrowserDialog
            {
                Description = "Seleziona la cartella patch",
                ShowNewFolderButton = true
            };
            if (!string.IsNullOrEmpty(_selectedPatchDir))
                dialog.SelectedPath = _selectedPatchDir;
            var result = dialog.ShowDialog();
            if (result == System.Windows.Forms.DialogResult.OK)
            {
                _selectedPatchDir = dialog.SelectedPath;
                PatchDirTextBox.Text = _selectedPatchDir;
            }
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }
    }
}
