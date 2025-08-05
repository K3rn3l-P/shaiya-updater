using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Reflection;
using System.Windows;
using Updater.Common;
using Updater.Configuration;
using Updater.Core;
using Updater.Extensions;
using Updater.Helpers;
using Updater.Interop;
using Updater.Resources;

#if DEBUG
internal static class Logger
{
    private static readonly string LogFile = Path.Combine(Directory.GetCurrentDirectory(), "Updater_debug.log");
    public static void Log(string message)
    {
        File.AppendAllText(LogFile, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}\n");
    }
}
#endif

namespace Updater
{
    public static class Program
    {
        static Program()
        {
            EnsureDllVersionMatchesUpdater();
        }

        // Restituisce true se la DLL è stata aggiornata o mancava e il processo è stato terminato
        private static bool EnsureDllVersionMatchesUpdater()
        {
            string dllPath = Path.Combine(Directory.GetCurrentDirectory(), "Duff.Updater.dll");
            string markerPath = Path.Combine(Directory.GetCurrentDirectory(), ".dll_updated");
            var exeVersion = Assembly.GetExecutingAssembly().GetName().Version;
            Version? dllVersion = null;
            if (File.Exists(dllPath))
            {
                try { dllVersion = AssemblyName.GetAssemblyName(dllPath).Version; } catch { }
            }

            // Se esiste il marker, elimina e prosegui normalmente (evita loop)
            if (File.Exists(markerPath))
            {
                try { File.Delete(markerPath); } catch { }
                return false;
            }

            // Se la DLL non esiste o la versione non corrisponde, aggiorna e RIAVVIA
            if (!File.Exists(dllPath) || dllVersion == null || dllVersion != exeVersion)
            {
                var assembly = Assembly.GetExecutingAssembly();
                string? resourceName = assembly.GetManifestResourceNames()
                    .FirstOrDefault(n => n.EndsWith("Duff.Updater.dll", StringComparison.OrdinalIgnoreCase));
                if (resourceName != null)
                {
                    try { if (File.Exists(dllPath)) File.Delete(dllPath); } catch { }
                    using var stream = assembly.GetManifestResourceStream(resourceName);
                    using var fs = new FileStream(dllPath, FileMode.Create, FileAccess.Write);
                    stream?.CopyTo(fs);
                }
                // Crea marker e riavvia
                File.WriteAllText(markerPath, "updated");
                Process.Start(new ProcessStartInfo
                {
                    FileName = Process.GetCurrentProcess().MainModule?.FileName ?? "Updater.exe",
                    WorkingDirectory = Directory.GetCurrentDirectory(),
                    UseShellExecute = true
                });
                Environment.Exit(0);
                return true;
            }
            return false;
        }

        private static void ExtractEmbeddedDllIfMissing()
        {
            string dllPath = Path.Combine(Directory.GetCurrentDirectory(), "Duff.Updater.dll");
            string markerPath = Path.Combine(Directory.GetCurrentDirectory(), ".dll_updated");
            if (File.Exists(markerPath))
            {
                try { File.Delete(markerPath); } catch { }
                return;
            }
            var assembly = Assembly.GetExecutingAssembly();
            string? resourceName = assembly.GetManifestResourceNames()
                .FirstOrDefault(n => n.EndsWith("Duff.Updater.dll", StringComparison.OrdinalIgnoreCase));
            if (resourceName == null)
                return;
            Version? embeddedVersion = null;
            using (var stream = assembly.GetManifestResourceStream(resourceName))
            using (var ms = new MemoryStream())
            {
                stream?.CopyTo(ms);
                ms.Position = 0;
                try
                {
                    var embeddedAssembly = Assembly.Load(ms.ToArray());
                    embeddedVersion = embeddedAssembly.GetName().Version;
                }
                catch { }
            }
            Version? fileVersion = null;
            if (File.Exists(dllPath))
            {
                try
                {
                    fileVersion = AssemblyName.GetAssemblyName(dllPath).Version;
                }
                catch { }
            }
            if (!File.Exists(dllPath))
            {
                using var stream = assembly.GetManifestResourceStream(resourceName);
                using var fs = new FileStream(dllPath, FileMode.Create, FileAccess.Write);
                stream?.CopyTo(fs);
                MessageBox.Show(
                    "A required component (Duff.Updater.dll) was missing and has been restored. Please restart the updater to continue. Please try again or contact support.",
                    "Component Updated",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information,
                    MessageBoxResult.OK,
                    MessageBoxOptions.DefaultDesktopOnly);
                Application.Current?.Dispatcher.Invoke(() => Application.Current.Shutdown());
                Environment.Exit(1);
            }
            if (embeddedVersion == null || fileVersion == null || embeddedVersion != fileVersion)
            {
                try { File.Delete(dllPath); } catch { }
                using var stream = assembly.GetManifestResourceStream(resourceName);
                using var fs = new FileStream(dllPath, FileMode.Create, FileAccess.Write);
                stream?.CopyTo(fs);
                File.WriteAllText(markerPath, "updated");
                string exePath = Process.GetCurrentProcess().MainModule?.FileName ?? "Updater.exe";
                string batchPath = Path.Combine(Directory.GetCurrentDirectory(), "restart_updater.bat");
                var batch = $@"
@echo off
:wait
tasklist | find /i ""Updater.exe"" >nul
if not errorlevel 1 (
    timeout /t 1 >nul
    goto wait
)
start """" ""{exePath}""
del ""%~f0""";
                File.WriteAllText(batchPath, batch);
                Process.Start(new ProcessStartInfo
                {
                    FileName = batchPath,
                    WindowStyle = ProcessWindowStyle.Hidden,
                    CreateNoWindow = true,
                    UseShellExecute = true
                });
                Application.Current?.Dispatcher.Invoke(() => Application.Current.Shutdown());
                Environment.Exit(1);
            }
        }

        public static void DoWork(HttpClient httpClient, BackgroundWorker backgroundWorker)
        {
#if DEBUG
            Logger.Log($"Enter: DoWork");
#endif
            // 1. Scarica ed estrai sempre la patch speciale all'avvio
            string specialPatchFileName = "special.patch";
            string specialPatchPath = Path.Combine(Directory.GetCurrentDirectory(), specialPatchFileName);
            string specialPatchUrl = $"{Constants.Source}/shaiya/patch/{specialPatchFileName}";

            backgroundWorker.ReportProgress(0, new ProgressReport(Strings.ProgressMessage1)); // "Downloading updater" (usato come "Downloading special patch")

            try
            {
                httpClient.DownloadFile(specialPatchUrl, specialPatchPath);

                if (File.Exists(specialPatchPath))
                {
                    backgroundWorker.ReportProgress(0, new ProgressReport(Strings.ProgressMessage4)); // "Extracting"
                    try
                    {
                        using (var zipArchive = System.IO.Compression.ZipFile.OpenRead(specialPatchPath))
                            zipArchive.ExtractToDirectory(Directory.GetCurrentDirectory(), true);

                        // Verifica hash se presente
                        string hashFile = Path.Combine(Directory.GetCurrentDirectory(), "hash.sha256");
                        string hashDufFile = Path.Combine(Directory.GetCurrentDirectory(), "hash.sha256.duf");
                        try
                        {
                            if (File.Exists(hashFile))
                            {
                                backgroundWorker.ReportProgress(0, new ProgressReport(Strings.HashCheckStart));
                                if (!HashHelper.VerifyFilesFromHash(Directory.GetCurrentDirectory(), hashFile, out var failedFile))
                                {
                                    backgroundWorker.ReportProgress(0, new ProgressReport(Strings.HashCheckFailed));
                                    ShowErrorAndExit("A file integrity check failed.", "Update Error");
                                }
                                backgroundWorker.ReportProgress(0, new ProgressReport(Strings.HashCheckEnd));
                                File.Delete(hashFile);
                            }
                        }
                        finally
                        {
                            if (File.Exists(hashDufFile)) File.Delete(hashDufFile);
                        }
                    }
                    catch
                    {
                        backgroundWorker.ReportProgress(0, new ProgressReport(Strings.ProgressMessage5)); // "Extraction failed"
                        string hashDufFile = Path.Combine(Directory.GetCurrentDirectory(), "hash.sha256.duf");
                        if (File.Exists(hashDufFile)) File.Delete(hashDufFile);
                    }
                    File.Delete(specialPatchPath);
                }
                // Sicurezza: cancella sempre hash.sha256.duf se presente
                string hashDufFileFinal = Path.Combine(Directory.GetCurrentDirectory(), "hash.sha256.duf");
                if (File.Exists(hashDufFileFinal)) File.Delete(hashDufFileFinal);
            }
            catch
            {
                // Se la patch speciale non esiste o c'è errore, ignora e prosegui
                string hashDufFileFinal = Path.Combine(Directory.GetCurrentDirectory(), "hash.sha256.duf");
                if (File.Exists(hashDufFileFinal)) File.Delete(hashDufFileFinal);
            }

            ClientConfiguration? clientConfiguration = null;
            ServerConfiguration? serverConfiguration = null;

            try
            {
                serverConfiguration = new ServerConfiguration(httpClient);
                clientConfiguration = new ClientConfiguration();

                // Solo ora, prima delle patch normali, verifica/aggiorna la DLL
                if (EnsureDllVersionMatchesUpdater())
                    return;

                if (serverConfiguration.UpdaterVersion > Constants.UpdaterVersion)
                {
                    backgroundWorker.ReportProgress(0, new ProgressReport(Strings.ProgressMessage1));
                    UpdaterPatcher(httpClient);
                    return;
                }

                if (serverConfiguration.PatchFileVersion > clientConfiguration.CurrentVersion)
                {
                    backgroundWorker.ReportProgress(0, new ProgressReport(1));
                    backgroundWorker.ReportProgress(0, new ProgressReport(2));

                    int progressMax = serverConfiguration.PatchFileVersion - clientConfiguration.CurrentVersion;
                    int progressValue = 1;

                    while (clientConfiguration.CurrentVersion < serverConfiguration.PatchFileVersion)
                    {
                        // (Controllo DLL rimosso, ora gestito all'avvio)
                        var progressMessage = string.Format(Strings.ProgressMessage2, progressValue, progressMax);
                        backgroundWorker.ReportProgress(0, new ProgressReport(progressMessage));

                        var patch = new Patch(clientConfiguration.CurrentVersion + 1);
                        httpClient.DownloadFile(patch.Url, patch.Path);

                        if (!File.Exists(patch.Path))
                        {
                            backgroundWorker.ReportProgress(0, new ProgressReport(Strings.ProgressMessage3));
                            return;
                        }

                        backgroundWorker.ReportProgress(0, new ProgressReport(Strings.ProgressMessage4));
                        clientConfiguration.StartUpdate = "EXTRACT_START";

                        // Estrai e verifica hash della patch
                        bool extractionOk = patch.ExtractToCurrentDirectory();
                        string patchHashFile = Path.Combine(Directory.GetCurrentDirectory(), "hash.sha256.duf");
                        if (extractionOk)
                        {
                            if (!File.Exists(patchHashFile))
                            {
                                ShowErrorAndExit("A required file is missing from the patch.", "Update Error");
                            }
                            backgroundWorker.ReportProgress(0, new ProgressReport(Strings.HashCheckStart));
                            if (!HashHelper.TryReadDecryptedFileAes(patchHashFile, out string hashString))
                            {
                                ShowErrorAndExit("A required file could not be read.", "Update Error");
                            }
                            if (!HashHelper.VerifyFilesFromHashString(Directory.GetCurrentDirectory(), hashString, out var failedFile))
                            {
                                backgroundWorker.ReportProgress(0, new ProgressReport(Strings.HashCheckFailed));
                                ShowErrorAndExit("A file integrity check failed.", "Update Error");
                            }
                            backgroundWorker.ReportProgress(0, new ProgressReport(Strings.HashCheckEnd));
                        }
                        if (!extractionOk)
                        {
                            backgroundWorker.ReportProgress(0, new ProgressReport(Strings.ProgressMessage5));
                            // Cleanup
                            if (File.Exists(patch.Path)) File.Delete(patch.Path);
                            if (File.Exists("update.sah")) File.Delete("update.sah");
                            if (File.Exists("update.saf")) File.Delete("update.saf");
                            return;
                        }

                        clientConfiguration.StartUpdate = "EXTRACT_END";
                        File.Delete(patch.Path);

                        backgroundWorker.ReportProgress(0, new ProgressReport(Strings.ProgressMessage6));
                        clientConfiguration.StartUpdate = "UPDATE_START";

                        DataPatcher(backgroundWorker);

                        clientConfiguration.StartUpdate = "UPDATE_END";
                        clientConfiguration.CurrentVersion++;
                        progressValue++;

                        var percentProgress = MathHelper.CalculatePercentage(clientConfiguration.CurrentVersion, serverConfiguration.PatchFileVersion);
                        if (percentProgress > 0)
                            backgroundWorker.ReportProgress(percentProgress, new ProgressReport(Strings.ProgressMessage7, 2));
                    }

                    backgroundWorker.ReportProgress(0, new ProgressReport(Strings.ProgressMessage8));
                    DataBuilder(backgroundWorker);
                    backgroundWorker.ReportProgress(0, new ProgressReport(Strings.ProgressMessage7));
                }

                // Verifica hash finale di data.sah/saf tramite nuova chain meta
                string configMetaUrl = $"{Constants.Source}/shaiya/config/data.meta.duf";
                string configMetaPath = Path.Combine(Directory.GetCurrentDirectory(), "data.meta.duf");
                httpClient.DownloadFile(configMetaUrl, configMetaPath);
                // Check if the downloaded file is actually HTML (e.g., 404 page or homepage)
                if (File.Exists(configMetaPath))
                {
                    string fileContent = File.ReadAllText(configMetaPath);
                    string contentLower = fileContent.ToLowerInvariant();
                    if (contentLower.Contains("<html") || contentLower.Contains("<!doctype") || contentLower.Contains("<head") || contentLower.Contains("<body"))
                    {
                        File.Delete(configMetaPath);
                        ShowErrorAndExit("A required data file could not be downloaded.", "Update Error");
                        return;
                    }
                }
                try
                {
                    if (File.Exists(configMetaPath))
                    {
                        backgroundWorker.ReportProgress(0, new ProgressReport(Strings.MetaChainCheckStart));
                        if (!HashHelper.TryReadDecryptedFileAes(configMetaPath, out string metaString))
                        {
                            ShowErrorAndExit("A required file could not be read.", "Update Error");
                        }
                        if (!HashHelper.VerifyMetaChain(Directory.GetCurrentDirectory(), metaString, out var failedStep))
                        {
                            backgroundWorker.ReportProgress(0, new ProgressReport(Strings.MetaChainCheckFailed));
                            ShowErrorAndExit("A data verification step failed.", "Update Error");
                        }
                        backgroundWorker.ReportProgress(0, new ProgressReport(Strings.MetaChainCheckEnd));
                        File.Delete(configMetaPath);
                    }
                }
                finally
                {
                    // Sicurezza: cancella sempre data.meta.duf se presente
                    if (File.Exists(configMetaPath)) File.Delete(configMetaPath);
                }
            }
            catch (Exception)
            {
                var caption = Application.ResourceAssembly.GetName().Name;
                MessageBox.Show("An unexpected error occurred. Please try again or contact support.", caption, MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                clientConfiguration?.Save();
                clientConfiguration?.Dispose();
                serverConfiguration?.Dispose();
            }
        }

        private static void DataBuilder(BackgroundWorker backgroundWorker)
        {
#if DEBUG
            Logger.Log($"Enter: DataBuilder");
#endif
            if (!File.Exists("data.sah") || !File.Exists("data.saf"))
                return;

            var fileCount = Function.GetSahFileCount("data.sah");
            if (fileCount <= 0)
                throw new FileFormatException();

            var progressReport = new ProgressReport(1);
            var progress = new Progress(backgroundWorker, progressReport, fileCount, 1);
            Function.DataBuilder("data.sah", "data.saf", progress.PerformStep);
        }

        private static void DataPatcher(BackgroundWorker backgroundWorker)
        {
#if DEBUG
            Logger.Log($"Enter: DataPatcher");
#endif
            // Allineamento a Updater.Tool: applica patch solo se hash.sha256.duf è presente e valido
            if (File.Exists("delete.lst"))
            {
                var paths = File.ReadAllLines("delete.lst");
                var fileCount = paths.Length;
                if (fileCount <= 0)
                    throw new FileFormatException();

                var progressReport = new ProgressReport(1);
                var progress = new Progress(backgroundWorker, progressReport, fileCount, 1);
                Function.RemoveFiles("data.sah", "data.saf", "delete.lst", progress.PerformStep);
                File.Delete("delete.lst");
            }

            if (File.Exists("update.sah") && File.Exists("update.saf"))
            {
                // Verifica presenza hash.sha256.duf
                string hashDuf = Path.Combine(Directory.GetCurrentDirectory(), "hash.sha256.duf");
                if (!File.Exists(hashDuf))
                    throw new FileNotFoundException($"hash.sha256.duf non trovato dopo estrazione patch");

                // Verifica integrità patch (hash)
                if (!HashHelper.TryReadDecryptedFileAes(hashDuf, out string hashString))
                    throw new Exception($"Impossibile decriptare hash.sha256.duf della patch");
                if (!HashHelper.VerifyFilesFromHashString(Directory.GetCurrentDirectory(), hashString, out var failedFile))
                    throw new Exception($"Verifica hash fallita per il file: {failedFile}");

                var fileCount = Function.GetSahFileCount("update.sah");
                if (fileCount <= 0)
                    throw new FileFormatException();

                var progressReport = new ProgressReport(1);
                var progress = new Progress(backgroundWorker, progressReport, fileCount, 1);
                Function.DataPatcher("data.sah", "data.saf", "update.sah", "update.saf", progress.PerformStep);

                // Cleanup updater.sah/saf e hash.sha256.duf
                if (File.Exists("update.sah")) File.Delete("update.sah");
                if (File.Exists("update.saf")) File.Delete("update.saf");
                if (File.Exists(hashDuf)) File.Delete(hashDuf); // Importantissima per far leggere il file hash criptato!!
            }
        }

        /// <summary>
        /// Downloads a new updater, creates a batch file to swap the updater, and terminates the current process.
        /// </summary>
        private static void UpdaterPatcher(HttpClient httpClient)
        {
#if DEBUG
            Logger.Log($"Enter: UpdaterPatcher");
#endif
            var newUpdater = new NewUpdater();
            httpClient.DownloadFile(newUpdater.Url, newUpdater.Path);
            // Duff.Updater.dll viene sempre estratta come embedded resource

            string exeDir = Directory.GetCurrentDirectory();
            string oldDll = Path.Combine(exeDir, "Duff.Updater.dll");
            // Elimina la DLL dalla root se esiste (così non resta la vecchia versione)
            if (File.Exists(oldDll))
            {
                try { File.Delete(oldDll); } catch { }
            }

            if (!File.Exists(newUpdater.Path))
            {
                // Nessun nuovo updater: avvia direttamente il gioco
                var gamePath = Path.Combine(Directory.GetCurrentDirectory(), "game.exe");
                if (File.Exists(gamePath))
                {
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

                    // Attendi 5 secondi prima di chiudere l'updater
                    System.Threading.Thread.Sleep(5000);

                    // Chiudi l'applicazione updater
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        Application.Current.Shutdown();
                    });
                }
                return;
            }

            string batchFile = Path.Combine(exeDir, "swap_updater.bat");
            string oldUpdater = Path.Combine(exeDir, "Updater.exe");
            string newUpdaterExe = Path.Combine(exeDir, "new_updater.exe");
            // Crea il batch che farà lo swap solo dell'exe
            var batch = $@"
@echo off
:wait
tasklist | find /i ""Updater.exe"" >nul
if not errorlevel 1 (
    timeout /t 1 >nul
    goto wait
)
del /f /q ""{oldUpdater}""
rename ""{newUpdaterExe}"" ""Updater.exe""
del /f /q ""{oldDll}""
REM Duff.Updater.dll sarà estratta come embedded resource al prossimo avvio
start """" ""{oldUpdater}""
del ""%~f0""
";
            File.WriteAllText(batchFile, batch);

            // Avvia il batch e termina subito il processo corrente
            Process.Start(new ProcessStartInfo
            {
                FileName = batchFile,
                WindowStyle = ProcessWindowStyle.Hidden,
                CreateNoWindow = true,
                UseShellExecute = true
            });

            var currentProcess = Process.GetCurrentProcess();
            currentProcess.Kill();
        }

        // Utility per mostrare MessageBox sempre in primo piano e chiudere l'applicazione
        private static void ShowErrorAndExit(string message, string title = "Error")
        {
            MessageBox.Show(message + " Please try again or contact support.", title, MessageBoxButton.OK, MessageBoxImage.Error, MessageBoxResult.OK, MessageBoxOptions.DefaultDesktopOnly);
            Application.Current?.Dispatcher.Invoke(() => Application.Current.Shutdown());
            Environment.Exit(1);
        }
    }
}
