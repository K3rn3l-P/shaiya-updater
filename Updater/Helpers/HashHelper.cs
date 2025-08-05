using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Linq;

namespace Updater.Helpers
{
    public static class HashHelper
    {
        // Legge e decripta un file AES compatibile Duff-Tool (salt+IV nei primi 32 byte)
        public static string ReadDecryptedFileAes(string filePath)
        {
            // Password AES usata anche in Duff-Tool
            const string aesPassword = "7yxVcR8nPFFaS528fFPkH6V89";
            byte[] fileBytes = File.ReadAllBytes(filePath);
            if (fileBytes.Length < 32)
                throw new InvalidDataException("File is too short to be valid.");
            byte[] salt = new byte[16];
            byte[] iv = new byte[16];
            Array.Copy(fileBytes, 0, salt, 0, 16);
            Array.Copy(fileBytes, 16, iv, 0, 16);
            using var derive = new Rfc2898DeriveBytes(aesPassword, salt, 100_000, HashAlgorithmName.SHA256);
            byte[] key = derive.GetBytes(32);
            using var aes = Aes.Create();
            aes.Key = key;
            aes.IV = iv;
            aes.Padding = PaddingMode.PKCS7;
            using var ms = new MemoryStream(fileBytes, 32, fileBytes.Length - 32);
            using var cryptoStream = new CryptoStream(ms, aes.CreateDecryptor(), CryptoStreamMode.Read);
            using var sr = new StreamReader(cryptoStream, Encoding.UTF8);
            return sr.ReadToEnd();
        }

        // Legge e decripta un file AES, restituisce true se ok, false se errore
        public static bool TryReadDecryptedFileAes(string filePath, out string result)
        {
            result = null;
            try
            {
                result = ReadDecryptedFileAes(filePath);
                if (!string.IsNullOrWhiteSpace(result) && result.Any(c => !char.IsControl(c) || c == '\n' || c == '\r'))
                    return true;
            }
            catch (Exception)
            {
                // Log eventuali errori se necessario, ma non mostrare dettagli all'utente
            }
            result = null;
            return false;
        }

        // Calcola l'hash SHA256 di un file
        public static string ComputeFileHash(string filePath)
        {
            using var sha256 = SHA256.Create();
            using var stream = File.OpenRead(filePath);
            var hash = sha256.ComputeHash(stream);
            return BitConverter.ToString(hash).Replace("-", string.Empty).ToLowerInvariant();
        }

        // Legge un file hash.sha256 e restituisce un dizionario filename -> hash
        public static Dictionary<string, string> ReadHashFile(string hashFilePath)
        {
            var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var line in File.ReadAllLines(hashFilePath))
            {
                var parts = line.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length >= 2)
                    dict[parts[1].Trim()] = parts[0].Trim();
            }
            return dict;
        }

        // Legge un file hash.sha256 (già decriptato) e restituisce un dizionario filename -> hash
        public static Dictionary<string, string> ReadHashString(string hashString)
        {
            var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            using var reader = new StringReader(hashString);
            string? line;
            while ((line = reader.ReadLine()) != null)
            {
                // Gestione filename con spazi: hash e filename separati dal primo spazio
                int firstSpace = line.IndexOf(' ');
                if (firstSpace > 0 && firstSpace < line.Length - 1)
                {
                    string hash = line.Substring(0, firstSpace).Trim();
                    string file = line.Substring(firstSpace + 1).Trim();
                    if (!string.IsNullOrEmpty(hash) && !string.IsNullOrEmpty(file))
                        dict[file] = hash;
                }
            }
            return dict;
        }

        // Verifica che tutti i file corrispondano agli hash
        public static bool VerifyFilesFromHash(string baseDirectory, string hashFilePath, out string? failedFile)
        {
            failedFile = null;
            var hashDict = ReadHashFile(hashFilePath);
            foreach (var kvp in hashDict)
            {
                var filePath = Path.Combine(baseDirectory, kvp.Key);
                if (!File.Exists(filePath) || ComputeFileHash(filePath) != kvp.Value)
                {
                    failedFile = kvp.Key;
                    return false;
                }
            }
            return true;
        }

        // Verifica che tutti i file corrispondano agli hash (usando stringa decriptata)
        public static bool VerifyFilesFromHashString(string baseDirectory, string hashString, out string? failedFile)
        {
            failedFile = null;
            var hashDict = ReadHashString(hashString);
#if DEBUG
            string logPath = Path.Combine(baseDirectory, "updater_hash_debug.log");
            using var log = new StreamWriter(logPath, append: true);
            log.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] Verifica hash avviata");
#endif
            foreach (var kvp in hashDict)
            {
                var filePath = Path.Combine(baseDirectory, kvp.Key);
                if (!File.Exists(filePath))
                {
#if DEBUG
                    log.WriteLine($"[MISSING] {filePath}");
#endif
                    failedFile = kvp.Key;
#if DEBUG
                    log.WriteLine($"[FAILED] File mancante: {filePath}");
#endif
                    return false;
                }
                var actualHash = ComputeFileHash(filePath);
                if (actualHash != kvp.Value)
                {
#if DEBUG
                    log.WriteLine($"[MISMATCH] {filePath}\n  Atteso:   {kvp.Value}\n  Calcolato: {actualHash}");
#endif
                    failedFile = kvp.Key;
#if DEBUG
                    log.WriteLine($"[FAILED] Hash mismatch per {filePath}");
#endif
                    return false;
                }
                else
                {
#if DEBUG
                    log.WriteLine($"[OK] {filePath}");
#endif
                }
            }
#if DEBUG
            log.WriteLine($"[SUCCESS] Tutti i file corrispondono agli hash\n");
#endif
            return true;
        }

        // Legge una chain meta (step-by-step, compatibile Duff-Tool)
        public static List<(string type, string? patch, string? sahHash, string? safHash)> ReadMetaChain(string metaString)
        {
            var list = new List<(string, string?, string?, string?)>();
            using var reader = new StringReader(metaString);
            string? line;
            string? currentStep = null;
            while ((line = reader.ReadLine()) != null)
            {
                line = line.Trim();
                if (string.IsNullOrEmpty(line) || line.StartsWith("#"))
                    continue;
                var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length == 2 && parts[0] == "STEP")
                {
                    currentStep = parts[1];
                    list.Add(("STEP", currentStep, null, null));
                }
                else if (parts.Length == 3 && parts[0] == "BASE")
                {
                    list.Add(("BASE", currentStep, parts[1], parts[2]));
                }
                else if (parts.Length == 4 && parts[0] == "PATCH")
                {
                    list.Add(("PATCH", parts[1], parts[2], parts[3]));
                }
                else if (parts.Length == 3 && parts[0] == "NEWBASE")
                {
                    list.Add(("NEWBASE", currentStep, parts[1], parts[2]));
                }
            }
            return list;
        }

        // Verifica SOLO la forma BASE 0\nhash1 hash2\n (come Updater.Tool). Non accetta chain meta.
        public static bool VerifyMetaChain(string baseDirectory, string metaString, out string? failedStep)
        {
            failedStep = null;
            var lines = metaString.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
            // Cerca la PRIMA riga che inizia con BASE e usala (ignora eventuali altre righe)
            var baseLine = lines.FirstOrDefault(l => l.StartsWith("BASE "));
            if (baseLine != null)
            {
                var baseIndex = Array.IndexOf(lines, baseLine);
                if (baseIndex >= 0 && baseIndex + 1 < lines.Length)
                {
                    var parts = lines[baseIndex].Split(' ');
                    var hashParts = lines[baseIndex + 1].Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length == 2 && hashParts.Length == 2)
                    {
                        string sahHash = hashParts[0];
                        string safHash = hashParts[1];
                        string sahPath = Path.Combine(baseDirectory, "data.sah");
                        string safPath = Path.Combine(baseDirectory, "data.saf");
                        if (!File.Exists(sahPath) || !File.Exists(safPath))
                        {
                            failedStep = "BASE_MISSING";
                            return false;
                        }
                        if (ComputeFileHash(sahPath) != sahHash || ComputeFileHash(safPath) != safHash)
                        {
                            failedStep = "BASE_HASH";
                            return false;
                        }
                        return true;
                    }
                }
            }
            failedStep = "INVALID_META_FORMAT";
            return false;
        }
    }
}
