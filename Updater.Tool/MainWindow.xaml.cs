using System;
using System.Windows;
using System.IO;
using System.Linq;
using Microsoft.Win32; // Per OpenFileDialog
using System.Windows.Forms; // Solo per FolderBrowserDialog
using Updater.Tool.Services;

namespace Updater.Tool
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly string PatchRoot = Path.Combine(Directory.GetCurrentDirectory(), "patch");

        public MainWindow()
        {
            InitializeComponent();
            BtnTabCreaPatch.IsEnabled = false;
            PanelCreaPatch.Visibility = Visibility.Visible;
            PanelApplicaPatch.Visibility = Visibility.Collapsed;
            LoadPatchFolders();
            LoadPatchFiles();
        }

        private void LoadPatchFolders()
        {
            lstPatchFolders.Items.Clear();
            if (Directory.Exists(PatchRoot))
            {
                var dirs = Directory.GetDirectories(PatchRoot);
                foreach (var dir in dirs)
                    lstPatchFolders.Items.Add(dir);
            }
        }

        private void LoadPatchFiles()
        {
            lstPatchFiles.Items.Clear();
            if (Directory.Exists(PatchRoot))
            {
                var files = Directory.GetFiles(PatchRoot, "*.patch")
                    .OrderBy(f => f)
                    .ToList();
                foreach (var file in files)
                    lstPatchFiles.Items.Add(file);
            }
        }

        private void BtnCreaPatch_Click(object sender, RoutedEventArgs e)
        {
            txtResultCrea.Clear();
            if (lstPatchFolders.SelectedItems.Count == 0)
            {
                txtResultCrea.Text = "Seleziona almeno una cartella.";
                return;
            }
            foreach (var item in lstPatchFolders.SelectedItems)
            {
                try
                {
                    PatchBuilder.CreatePatch(item.ToString());
                    txtResultCrea.AppendText($"Patch creata per: {item}\n");
                }
                catch (Exception ex)
                {
                    txtResultCrea.AppendText($"Errore per {item}: {ex.Message}\n");
                    Logger.LogError($"Errore creazione patch per {item}", ex);
                }
            }
            LoadPatchFiles(); // Aggiorna lista patch files
        }

        private void BtnApplicaPatch_Click(object sender, RoutedEventArgs e)
        {
            txtResultApplica.Clear();
            var dataFolder = txtDataFolder.Text;
            if (string.IsNullOrWhiteSpace(dataFolder) || !Directory.Exists(dataFolder))
            {
                txtResultApplica.Text = "Seleziona una cartella dati valida.";
                return;
            }
            if (lstPatchFiles.SelectedItems.Count == 0)
            {
                txtResultApplica.Text = "Seleziona almeno una patch.";
                return;
            }
            // Ordina le patch selezionate per numero
            var selected = lstPatchFiles.SelectedItems.Cast<string>()
                .OrderBy(f => f)
                .ToList();
            try
            {
                PatchApplier.ApplyPatch(dataFolder, selected.ToArray());
                // Mostra percorso e hash data.meta.duf
                string hashMetaDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "hash-meta");
                string metaTxt = Path.Combine(hashMetaDir, "data.meta.txt");
                string metaDuf = Path.Combine(hashMetaDir, "data.meta.duf");
                txtResultApplica.AppendText($"Patch applicate: {string.Join(", ", selected.Select(System.IO.Path.GetFileName))}\n");
                txtResultApplica.AppendText($"Percorso data.meta.duf: {metaDuf}\n");
                if (File.Exists(metaTxt))
                {
                    string hash = File.ReadAllText(metaTxt).Trim();
                    txtResultApplica.AppendText($"Hash: {hash}\n\n");
                }
            }
            catch (Exception ex)
            {
                txtResultApplica.AppendText($"Errore: {ex.Message}\n");
                Logger.LogError($"Errore applicazione patch", ex);
            }
        }

        private void BtnBrowseDataFolder_Click(object sender, RoutedEventArgs e)
        {
            using (var dlg = new FolderBrowserDialog())
            {
                if (dlg.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                    txtDataFolder.Text = dlg.SelectedPath;
            }
        }

        private void lstPatchFiles_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {

        }

        private void txtResultCrea_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {

        }

        private void txtResultApplica_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {

        }

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            var win = new SettingsWindow(App.PatchDirectory);
            win.Owner = this;
            if (win.ShowDialog() == true)
            {
                if (!string.IsNullOrEmpty(win.SelectedPatchDir) && win.SelectedPatchDir != App.PatchDirectory)
                {
                    // Usa reflection per settare la proprietà privata
                    typeof(App).GetProperty("PatchDirectory")?.SetValue(App.Current, win.SelectedPatchDir);
                    App.Settings.LastPatchDirectory = win.SelectedPatchDir;
                    App.Settings.Save();
                    System.Windows.MessageBox.Show($"Cartella patch aggiornata:\n{win.SelectedPatchDir}", "Impostazioni", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
        }

        private void BtnTabCreaPatch_Click(object sender, RoutedEventArgs e)
        {
            PanelCreaPatch.Visibility = Visibility.Visible;
            PanelApplicaPatch.Visibility = Visibility.Collapsed;
            BtnTabCreaPatch.IsEnabled = false;
            BtnTabApplicaPatch.IsEnabled = true;
        }

        private void BtnTabApplicaPatch_Click(object sender, RoutedEventArgs e)
        {
            PanelCreaPatch.Visibility = Visibility.Collapsed;
            PanelApplicaPatch.Visibility = Visibility.Visible;
            BtnTabCreaPatch.IsEnabled = true;
            BtnTabApplicaPatch.IsEnabled = false;
        }

        private void BtnCreaSpecialPatch_Click(object sender, RoutedEventArgs e)
        {
            txtResultCrea.Clear();
            string specialFolder = Path.Combine(PatchRoot, "special");
            if (!Directory.Exists(specialFolder))
            {
                txtResultCrea.Text = "Cartella 'special' non trovata in 'patch'.";
                return;
            }
            try
            {
                PatchBuilder.CreateSpecialPatch(specialFolder);
                txtResultCrea.AppendText($"Special patch creata per: {specialFolder}\n");
            }
            catch (Exception ex)
            {
                txtResultCrea.AppendText($"Errore special patch: {ex.Message}\n");
                Logger.LogError($"Errore creazione special patch", ex);
            }
            LoadPatchFiles(); // Aggiorna lista patch files
        }
    }
}