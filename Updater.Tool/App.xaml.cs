using System;
using System.IO;
using System.Windows;
using Updater.Tool.Services;

namespace Updater.Tool
{
    public partial class App : System.Windows.Application
    {
        public static string? PatchDirectory { get; set; } // reso set pubblico
        public static PatchToolSettings Settings { get; private set; } = null!;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            Settings = PatchToolSettings.Load();
            PatchDirectory = Settings.LastPatchDirectory;

            if (!string.IsNullOrEmpty(PatchDirectory) && Directory.Exists(PatchDirectory))
            {
                var result = System.Windows.MessageBox.Show(
                    $"Use last patch directory?\n{PatchDirectory}",
                    "Patch Directory",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);
                if (result == MessageBoxResult.No)
                {
                    PatchDirectory = SelectPatchDirectory();
                }
            }
            else
            {
                PatchDirectory = SelectPatchDirectory();
            }

            if (!string.IsNullOrEmpty(PatchDirectory))
            {
                Settings.LastPatchDirectory = PatchDirectory;
                Settings.Save();
            }
            else
            {
                System.Windows.MessageBox.Show("No patch directory selected. The tool will now exit.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Shutdown();
            }
        }

        private static string? SelectPatchDirectory()
        {
            var dialog = new System.Windows.Forms.FolderBrowserDialog
            {
                Description = "Select the patch directory (where patch files are stored/applied)",
                ShowNewFolderButton = true
            };
            var result = dialog.ShowDialog();
            if (result == System.Windows.Forms.DialogResult.OK)
                return dialog.SelectedPath;
            return null;
        }
    }
}
