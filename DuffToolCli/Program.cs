using System;
using Updater.Tool.Services;

namespace DuffToolCli
{
    internal static class Program
    {
        static int Main(string[] args)
        {
            // Setta la PatchDirectory per uso CLI (cartella corrente)
            Updater.Tool.App.PatchDirectory = Environment.CurrentDirectory;

            if (args.Length == 2 && args[0].ToLowerInvariant() == "patch" && args[1].ToLowerInvariant() == "special")
            {
                try
                {
                    // Forza la cartella patch/special come argomento esplicito
                    var specialDir = System.IO.Path.Combine(Environment.CurrentDirectory, "patch", "special");
                    PatchBuilder.CreateSpecialPatch(specialDir);
                    Console.WriteLine("Special patch creata con successo.");
                    return 0;
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"Errore creazione special patch: {ex.Message}");
                    return 1;
                }
            }
            Console.WriteLine("Usage: DuffToolCli.exe patch special");
            return 1;
        }
    }
}
