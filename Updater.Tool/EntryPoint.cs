using System;
using Updater.Tool.Services;
using System.IO;

namespace Updater.Tool
{
    public static class EntryPoint
    {
#if CONSOLE
        // CLI entry point
        public static void Main(string[] args)
        {
            if (args.Length > 0)
            {
                switch (args[0].ToLowerInvariant())
                {
                    case "create-patch":
                        if (args.Length == 4)
                        {
                            PatchBuilder.CreatePatch(args[1], args[2], args[3]);
                            Console.WriteLine($"Patch creata: {args[3]}");
                        }
                        else
                        {
                            Console.WriteLine("Usage: create-patch <update.sah> <update.saf> <output.patch>");
                        }
                        break;
                    case "apply-patch":
                        if (args.Length == 4)
                        {
                            PatchApplier.ApplyPatch(args[1], args[2], args[3]);
                            Console.WriteLine("Patch applicata.");
                        }
                        else
                        {
                            Console.WriteLine("Usage: apply-patch <data.sah> <data.saf> <patch>");
                        }
                        break;
                    case "create-meta":
                        // create-meta <step1> <data.sah> <data.saf> ... <output.meta.duf>
                        if (args.Length >= 5 && (args.Length - 2) % 3 == 0)
                        {
                            var steps = new System.Collections.Generic.List<(string, string, string)>();
                            for (int i = 1; i < args.Length - 1; i += 3)
                                steps.Add((args[i], args[i + 1], args[i + 2]));
                            MetaChainBuilder.CreateMetaChain(steps, args[^1]);
                            Console.WriteLine($"Meta chain creata: {args[^1]}");
                        }
                        else
                        {
                            Console.WriteLine("Usage: create-meta <step1> <data.sah> <data.saf> ... <output.meta.duf>");
                        }
                        break;
                    case "patch":
                        if (args.Length > 1 && args[1].ToLowerInvariant() == "special")
                        {
                            // Crea patch speciale nella cartella patch/special
                            try
                            {
                                PatchBuilder.CreateSpecialPatch();
                                Console.WriteLine("Special patch creata con successo.");
                                Environment.Exit(0);
                            }
                            catch (Exception ex)
                            {
                                Console.Error.WriteLine($"Errore creazione special patch: {ex.Message}");
                                Environment.Exit(1);
                            }
                        }
                        break;
                    default:
                        Console.WriteLine("Comando non riconosciuto.");
                        break;
                }
            }
            else
            {
                // Avvia la GUI WPF
                var app = new App();
                app.InitializeComponent();
                app.Run();
            }
        }
#endif
    }
}
