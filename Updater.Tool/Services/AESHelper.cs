using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace Updater.Tool.Services
{
    public static class AESHelper
    {
        private const string aesPassword = "7yxVcR8nPFFaS528fFPkH6V89";
        public static void EncryptFileAes(string inputPath, string outputPath)
        {
            byte[] salt = RandomNumberGenerator.GetBytes(16);
            using var derive = new Rfc2898DeriveBytes(aesPassword, salt, 100_000, HashAlgorithmName.SHA256);
            byte[] key = derive.GetBytes(32);
            using var aes = Aes.Create();
            aes.Key = key;
            aes.GenerateIV();
            aes.Padding = PaddingMode.PKCS7;
            using var input = File.OpenRead(inputPath);
            using var output = File.Create(outputPath);
            output.Write(salt, 0, 16);
            output.Write(aes.IV, 0, 16);
            using var cryptoStream = new CryptoStream(output, aes.CreateEncryptor(), CryptoStreamMode.Write);
            input.CopyTo(cryptoStream);
        }

        public static void DecryptFileAes(string inputPath, string outputPath)
        {
            byte[] fileBytes = File.ReadAllBytes(inputPath);
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
            using var output = File.Create(outputPath);
            using var cryptoStream = new CryptoStream(ms, aes.CreateDecryptor(), CryptoStreamMode.Read);
            cryptoStream.CopyTo(output);
        }
    }
}
