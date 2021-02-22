using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace BuildTool
{
    internal static class Program
    {
        private const string BepInExDownloadUrl =
            "https://github.com/BepInEx/BepInEx/releases/download/v5.4.5/BepInEx_x64_5.4.5.0.zip";

        private const string UnityDoorstopDownloadUrl =
            "https://github.com/NeighTools/UnityDoorstop/releases/download/v3.2.0.0/Doorstop_x64_3.2.0.0.zip";

        private const string UnityUnstrippedDllsRepo =
            "https://codeload.github.com/Measurity/OriginalUEngineSources/zip/{VERSION}";

        private static readonly Regex iniRegex = new Regex(@"^(\w+)=(\\N+)?", RegexOptions.Compiled);
        private static readonly string[] skipZipExtractContains = {"example", "readme", "changelog"};
        private const string UnstrippedDllsFolderName = "unstripped_corlib";

        private static uint SteamAppId =>
            !uint.TryParse(Environment.GetEnvironmentVariable("SteamAppId"), out var id) ? 892970 : id;

        private static bool SkipPublicizer =>
            bool.TryParse(Environment.GetEnvironmentVariable("SkipPublicizer"), out var skip) && skip;

        public static async Task Main(string[] args)
        {
            if (SteamAppId <= 0) throw new Exception("SteamAppId environment variable must be set and be valid");

            Console.WriteLine($"Building mod for Steam game with id {SteamAppId}");
            var game = await Task.Factory.StartNew(() => EnsureSteamGame(SteamAppId)).ConfigureAwait(false);
            Console.WriteLine($"Found game at {game.InstallDir}");
            await EnsureBepInExAsync(game.InstallDir);
            await EnsureUnityDoorstopAsync(game);
            await Task.WhenAll(Task.Factory.StartNew(() => EnsurePublicizedAssemblies(game)),
                    EnsureUnstrippedMonoAssembliesAsync(game),
                    Task.Factory.StartNew(() => EnsureUnityDoorstopConfig(game)))
                .ConfigureAwait(false);
        }

        private static void EnsureUnityDoorstopConfig(SteamGameData game)
        {
            // Change UnityDoorstop configuration to make it override game dlls with unstripped dlls.
            var unityDoorstopConfig = Path.Combine(game.InstallDir, "doorstop_config.ini");
            if (!File.Exists(unityDoorstopConfig))
            {
                throw new FileNotFoundException("UnityDoorstop config file not found", unityDoorstopConfig);
            }
            var configContent = File.ReadAllLines(unityDoorstopConfig).ToList();
            var requiredValues = new Dictionary<string, string>
            {
                {"dllSearchPathOverride", UnstrippedDllsFolderName},
                {"targetAssembly", @"BepInEx\core\BepInEx.Preloader.dll"}
            };
            for (var i = 0; i < configContent.Count; i++)
            {
                var match = iniRegex.Match(configContent[i]);
                if (!match.Success) continue;
                if (!requiredValues.TryGetValue(match.Groups[1].Value, out var value)) continue;
            
                configContent[i] = $"{match.Groups[1].Value}={value}";
                requiredValues.Remove(match.Groups[1].Value);
            }
            foreach (var pair in requiredValues)
            {
                configContent.Add($"{pair.Key}={pair.Value}");
            }
            File.WriteAllLines(unityDoorstopConfig, configContent);
        }

        private static async Task EnsureUnityDoorstopAsync(SteamGameData game)
        {
            var zip = Path.Combine(Utils.GeneratedOutputDir, "unitydoorstop.zip");
            if (!File.Exists(zip))
            {
                using var client = new WebClient();
                await client.DownloadFileTaskAsync(UnityDoorstopDownloadUrl, zip);
            }

            // Extract UnityDoorstop zip over game files.
            using var zipReader = ZipFile.OpenRead(zip);

            string[] skipIfExists = {"doorstop_config.ini"};
            foreach (var entry in zipReader.Entries)
            {
                if (skipZipExtractContains.Any(
                    name => entry.FullName.IndexOf(name, StringComparison.InvariantCultureIgnoreCase) >= 0)) continue;
                var targetFile = Path.Combine(game.InstallDir, entry.FullName);
                if (File.Exists(targetFile) &&
                    skipIfExists.Any(f =>
                        f.Equals(Path.GetFileName(targetFile), StringComparison.InvariantCultureIgnoreCase))) continue;

                Directory.CreateDirectory(Path.GetDirectoryName(targetFile));
                entry.ExtractToFile(targetFile, true);
            }
        }

        private static async Task EnsureUnstrippedMonoAssembliesAsync(SteamGameData game)
        {
            if (Directory.Exists(Path.Combine(game.InstallDir, UnstrippedDllsFolderName)))
            {
                Console.WriteLine("Unstripped Mono Assemblies are already installed.");
                return;
            }
            // Detect Unity Engine version the game is using.
            var unityVersionUsedByGame = new Version(FileVersionInfo
                .GetVersionInfo(Path.Combine(game.InstallDir, "UnityPlayer.dll"))
                .FileVersion).ToString(3);

            // Create Unstripped dlls directory in game directory
            var targetDir = Path.Combine(game.InstallDir, UnstrippedDllsFolderName);
            Directory.CreateDirectory(targetDir);

            // Download unstripped dlls
            var dllsZip = Path.Combine(Utils.GeneratedOutputDir, $@"{unityVersionUsedByGame}.zip");
            if (!File.Exists(dllsZip))
            {
                using var client = new WebClient();
                await client.DownloadFileTaskAsync(
                    UnityUnstrippedDllsRepo.Replace("{VERSION}", unityVersionUsedByGame),
                    dllsZip);
            }

            // Extract unstripped dlls to unstripped dlls directory in game directory
            using var zipReader = ZipFile.OpenRead(dllsZip);
            foreach (var entry in zipReader.Entries)
            {
                var fileName = Path.GetFileName(entry.FullName);
                if (string.IsNullOrWhiteSpace(fileName)) continue;
                
                var targetFile = Path.Combine(targetDir, fileName);
                entry.ExtractToFile(targetFile, true);
            }
        }

        private static void EnsurePublicizedAssemblies(SteamGameData game)
        {
            if (SkipPublicizer)
            {
                Console.WriteLine("Skipping Assembly Publicizer, execute this manually.");
                return;
            }
            if (Directory.Exists(Path.Combine(Utils.GeneratedOutputDir, "publicized_assemblies")))
            {
                Console.WriteLine("Assemblies are already publicized.");
                return;
            }

            var dllsToPublicize = Directory.GetFiles(game.ManagedDllsDir, "assembly_*.dll");
            foreach (var publicizedDll in Publicizer.Execute(dllsToPublicize,
                "_publicized",
                Path.Combine(Utils.GeneratedOutputDir, "publicized_assemblies")))
            {
                Console.WriteLine($"Wrote publicized dll: {publicizedDll}");
            }
        }

        private static SteamGameData EnsureSteamGame(uint steamAppId)
        {
            static string ValidateUnityGame(SteamGameData game, uint steamAppId)
            {
                if (game.Id != steamAppId)
                {
                    return $"Steam id in game.props {game.Id} does not match {steamAppId}";
                }
                if (!File.Exists(Path.Combine(game.InstallDir, "UnityPlayer.dll")))
                {
                    return "Steam game is not a Unity game.";
                }
                if (!Directory.Exists(game.ManagedDllsDir))
                {
                    throw new Exception("Game is missing Unity managed dlls directory.");
                }
                return null;
            }

            var cacheFile = Path.Combine(Utils.GeneratedOutputDir, "game.props");
            var game = SteamGameData.TryFrom(cacheFile);
            if (game == null || ValidateUnityGame(game, steamAppId) != null)
            {
                game = Steam.FindGame(steamAppId);
            }

            var error = ValidateUnityGame(game, steamAppId);
            if (error != null)
            {
                throw new Exception(error);
            }

            game.TrySave(cacheFile);
            return game;
        }

        private static async Task EnsureBepInExAsync(string gameDir)
        {
            if (File.Exists(Path.Combine(gameDir, "BepInEx", "core", "BepInEx.dll")))
            {
                Console.WriteLine("BepInEx is already installed.");
                return;
            }

            var zip = Path.Combine(Utils.GeneratedOutputDir, "bepinex.zip");
            if (!File.Exists(zip))
            {
                using var client = new WebClient();
                await client.DownloadFileTaskAsync(BepInExDownloadUrl, zip);
            }
            // Extract BepInEx zip over game files.
            using var zipReader = ZipFile.OpenRead(zip);
            foreach (var entry in zipReader.Entries)
            {
                if (skipZipExtractContains.Any(
                    name => entry.FullName.IndexOf(name, StringComparison.InvariantCultureIgnoreCase) >= 0)) continue;

                var targetFile = Path.Combine(gameDir, entry.FullName);
                Directory.CreateDirectory(Path.GetDirectoryName(targetFile));
                entry.ExtractToFile(targetFile, true);
            }
        }
    }
}
