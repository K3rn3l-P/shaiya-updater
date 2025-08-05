using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using Updater.Helpers;
using Updater.Interop;

namespace Updater.Tool.Services
{
    public static class PatchBuilder
    {
        public static void CreatePatch(string? psFolderPath = null)
        {
            // Usa patch directory globale se non specificato
            if (string.IsNullOrEmpty(psFolderPath))
            {
                if (string.IsNullOrEmpty(App.PatchDirectory))
                    throw new InvalidOperationException("Patch directory is not set.");
                psFolderPath = App.PatchDirectory;
            }
            // psFolderPath: es. .../patch/ps0002
            if (!Directory.Exists(psFolderPath))
                throw new DirectoryNotFoundException($"Cartella non trovata: {psFolderPath}");

            // Trova update.sah e update.saf
            string sahPath = Path.Combine(psFolderPath, "update.sah");
            string safPath = Path.Combine(psFolderPath, "update.saf");
            if (!File.Exists(sahPath) || !File.Exists(safPath))
                throw new FileNotFoundException("update.sah o update.saf non trovati nella cartella selezionata.");

            // Elimina vecchi hash se presenti
            string hashTxt = Path.Combine(psFolderPath, "hash.sha256.txt");
            string hashDuf = Path.Combine(psFolderPath, "hash.sha256.duf");
            if (File.Exists(hashTxt)) File.Delete(hashTxt);
            if (File.Exists(hashDuf)) File.Delete(hashDuf);

            // Hash di tutti i file e cartelle (NON ricorsivo, solo file validi)
            var files = Directory.GetFiles(psFolderPath)
                .Where(f =>
                    Path.GetFileName(f) != "hash.sha256.txt" &&
                    Path.GetFileName(f) != "hash.sha256.duf" &&
                    !f.EndsWith(".patch", StringComparison.OrdinalIgnoreCase)
                )
                .ToList();
            var hashDict = new Dictionary<string, string>();
            foreach (var file in files)
            {
                hashDict[Path.GetFileName(file)] = HashHelper.ComputeFileHash(file);
            }
            // Scrivi hash.sha256.txt in chiaro
            using (var sw = new StreamWriter(hashTxt))
            {
                foreach (var kvp in hashDict)
                    sw.WriteLine($"{kvp.Value} {kvp.Key}");
            }
            // Cripta hash.sha256.txt -> hash.sha256.duf
            AESHelper.EncryptFileAes(hashTxt, hashDuf);

            // Crea patch/psxxxx.patch nella cartella root "patch"
            string patchDir = Path.GetDirectoryName(psFolderPath);
            if (string.IsNullOrEmpty(patchDir)) patchDir = ".";
            string patchRoot = patchDir;
            if (Path.GetFileName(patchRoot).Equals("patch", StringComparison.OrdinalIgnoreCase))
                patchRoot = patchDir;
            else
                patchRoot = Path.Combine(patchDir, "patch");
            if (!Directory.Exists(patchRoot)) Directory.CreateDirectory(patchRoot);
            string patchFile = Path.Combine(patchRoot, new DirectoryInfo(psFolderPath).Name + ".patch");
            if (File.Exists(patchFile)) File.Delete(patchFile);
            using (var zip = ZipFile.Open(patchFile, ZipArchiveMode.Create))
            {
                // Inserisci tutti i file (tranne hash.txt, .duf, .patch) nella patch
                foreach (var file in files)
                {
                    string entryName = Path.GetFileName(file);
                    zip.CreateEntryFromFile(file, entryName);
                }
                // Inserisci hash.sha256.duf
                zip.CreateEntryFromFile(hashDuf, "hash.sha256.duf");
            }
            Services.Logger.Log($"Patch creata: {patchFile}");
        }

        public static void CreateSpecialPatch(string? specialFolderPath = null)
        {
            // Usa patch directory globale/special se non specificato
            if (string.IsNullOrEmpty(specialFolderPath))
            {
                if (string.IsNullOrEmpty(App.PatchDirectory))
                    throw new InvalidOperationException("Patch directory is not set.");
                specialFolderPath = Path.Combine(App.PatchDirectory, "special");
            }
            if (!Directory.Exists(specialFolderPath))
                throw new DirectoryNotFoundException($"Cartella non trovata: {specialFolderPath}");

            // Solo questi file
            var allowedFiles = new[] { "game.exe", "duff.dll", "x32.exe" };
            var files = allowedFiles
                .Select(f => Path.Combine(specialFolderPath, f))
                .Where(File.Exists)
                .ToList();
            if (files.Count == 0)
                throw new FileNotFoundException($"Nessun file valido trovato in {specialFolderPath}");

            // Elimina vecchi hash se presenti
            string hashTxt = Path.Combine(specialFolderPath, "hash.sha256.txt");
            string hashDuf = Path.Combine(specialFolderPath, "hash.sha256.duf");
            if (File.Exists(hashTxt)) File.Delete(hashTxt);
            if (File.Exists(hashDuf)) File.Delete(hashDuf);

            // Hash solo dei file allowed
            var hashDict = new Dictionary<string, string>();
            foreach (var file in files)
                hashDict[Path.GetFileName(file)] = HashHelper.ComputeFileHash(file);
            using (var sw = new StreamWriter(hashTxt))
                foreach (var kvp in hashDict)
                    sw.WriteLine($"{kvp.Value} {kvp.Key}");
            AESHelper.EncryptFileAes(hashTxt, hashDuf);

            // Crea special.patch nella cartella patch root
            string patchRoot = Path.GetDirectoryName(specialFolderPath) ?? ".";
            string patchFile = Path.Combine(patchRoot, "special.patch");
            if (File.Exists(patchFile)) File.Delete(patchFile);
            using (var zip = ZipFile.Open(patchFile, ZipArchiveMode.Create))
            {
                foreach (var file in files)
                {
                    string entryName = Path.GetFileName(file);
                    zip.CreateEntryFromFile(file, entryName);
                }
                zip.CreateEntryFromFile(hashDuf, "hash.sha256.duf");
            }
            Services.Logger.Log($"Special patch creata: {patchFile}");
        }
    }
}
