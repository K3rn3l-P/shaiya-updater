using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Collections.Generic;
using Updater.Helpers;
using Updater.Tool.Services;
using Updater.Interop;

namespace Updater.Tool.Services
{
    public static class PatchApplier
    {
        // Applica una singola patch (helper privato, senza rewrite/meta)
        private static void ApplySinglePatch(string dataFolder, string patchFile)
        {
            string dataSah = Path.Combine(dataFolder, "data.sah");
            string dataSaf = Path.Combine(dataFolder, "data.saf");
            if (!File.Exists(dataSah) || !File.Exists(dataSaf))
                throw new FileNotFoundException("data.sah o data.saf non trovati nella cartella selezionata.");

            using (var zip = ZipFile.OpenRead(patchFile))
                zip.ExtractToDirectory(dataFolder, true);

            string hashDuf = Path.Combine(dataFolder, "hash.sha256.duf");
            if (!File.Exists(hashDuf))
                throw new FileNotFoundException($"hash.sha256.duf non trovato dopo estrazione patch: {patchFile}");

            if (!HashHelper.TryReadDecryptedFileAes(hashDuf, out string hashString))
                throw new Exception($"Impossibile decriptare hash.sha256.duf della patch: {patchFile}");
            if (!HashHelper.VerifyFilesFromHashString(dataFolder, hashString, out var failedFile))
                throw new Exception($"Verifica hash fallita per il file: {failedFile}");

            string updateSah = Path.Combine(dataFolder, "update.sah");
            string updateSaf = Path.Combine(dataFolder, "update.saf");
            if (!File.Exists(updateSah) || !File.Exists(updateSaf))
                throw new FileNotFoundException("update.sah o update.saf non trovati dopo estrazione patch.");
            Function.DataPatcher(dataSah, dataSaf, updateSah, updateSaf, null);

            if (File.Exists(updateSah)) File.Delete(updateSah);
            if (File.Exists(updateSaf)) File.Delete(updateSaf);
            if (File.Exists(hashDuf)) File.Delete(hashDuf);
        }

        // Applica una o più patch, fa sempre rewrite archive/meta UNA SOLA VOLTA alla fine
        public static void ApplyPatch(string? dataFolder = null, params string[] patchFiles)
        {
            // Usa patch directory globale se non specificato
            if (string.IsNullOrEmpty(dataFolder))
            {
                if (string.IsNullOrEmpty(App.PatchDirectory))
                    throw new InvalidOperationException("Patch directory is not set.");
                dataFolder = App.PatchDirectory;
            }
            string dataSah = Path.Combine(dataFolder, "data.sah");
            string dataSaf = Path.Combine(dataFolder, "data.saf");

            // Applica tutte le patch in sequenza SENZA rewrite archive/meta
            foreach (var patchFile in patchFiles)
                ApplySinglePatch(dataFolder, patchFile);

            // SOLO ALLA FINE: Ricostruisci i data.sah/saf per avere hash coerenti con l'updater
            Function.DataBuilder(dataSah, dataSaf, null);

            // Genera hash dei data.sah/saf e crea data.meta.txt/duf in hash-meta
            string sahHash = HashHelper.ComputeFileHash(dataSah);
            string safHash = HashHelper.ComputeFileHash(dataSaf);
            string metaString = $"BASE 0\n{sahHash} {safHash}\n";
            string hashMetaDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "hash-meta");
            if (!Directory.Exists(hashMetaDir))
                Directory.CreateDirectory(hashMetaDir);
            string metaTxt = Path.Combine(hashMetaDir, "data.meta.txt");
            string metaDuf = Path.Combine(hashMetaDir, "data.meta.duf");
            File.WriteAllText(metaTxt, metaString);
            AESHelper.EncryptFileAes(metaTxt, metaDuf);
            Services.Logger.Log($"Patch applicata: {string.Join(", ", patchFiles)}, meta aggiornata.");
        }
    }
}
