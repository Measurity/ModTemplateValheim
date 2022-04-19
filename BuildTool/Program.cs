using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace BuildTool
{
    internal static class Program
    {
        private static readonly string[] skipZipExtractContains = { "example", "readme", "changelog" };

        private const string BepInExDownloadUrl =
            "https://valheim.thunderstore.io/package/download/denikson/BepInExPack_Valheim/5.4.1900/";


        private static uint SteamAppId =>
            !uint.TryParse(Environment.GetEnvironmentVariable("SteamAppId"), out var id) ? 892970 : id;

        private static bool SkipPublicizer =>
            bool.TryParse(Environment.GetEnvironmentVariable("SkipPublicizer"), out var skip) && skip;

        public static async Task Main(string[] args)
        {
            if (SteamAppId <= 0) throw new Exception("SteamAppId environment variable must be set and be valid");

            Console.WriteLine($"Building mod for Steam game with id {SteamAppId}");
            SteamGameData game = await Task.Factory.StartNew(() => EnsureSteamGame(SteamAppId)).ConfigureAwait(false);
            Console.WriteLine($"Found game at {game.InstallDir}");
            await Task.WhenAll(
                    Task.Factory.StartNew(() => EnsureBepInExAsync(game.InstallDir)).Unwrap(),
                    Task.Factory.StartNew(() => EnsurePublicizedAssemblies(game)))
                .ConfigureAwait(false);
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
            SteamGameData game = SteamGameData.TryFrom(cacheFile);
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
            if (File.Exists(Path.Combine(Utils.GeneratedOutputDir, "bepinex.zip")) && File.Exists(Path.Combine(gameDir, "BepInEx", "core", "BepInEx.dll")))
            {
                Console.WriteLine("BepInEx is already installed.");
                return;
            }

            var zip = Path.Combine(Utils.GeneratedOutputDir, "bepinex.zip");
            if (!File.Exists(zip))
            {
                using WebClient client = new();
                await client.DownloadFileTaskAsync(BepInExDownloadUrl, zip);
            }
            // Extract BepInEx zip over game files.
            using ZipArchive zipReader = ZipFile.OpenRead(zip);
            foreach (ZipArchiveEntry entry in zipReader.Entries)
            {
                if (skipZipExtractContains.Any(
                        name => entry.FullName.IndexOf(name, StringComparison.InvariantCultureIgnoreCase) >= 0)) continue;
                var isFolder = entry.FullName.EndsWith("/");
                var targetFile = Path.Combine(gameDir, entry.FullName.Replace("BepInExPack_Valheim/", ""));

                if (isFolder)
                {
                    Directory.CreateDirectory(targetFile);
                }
                else
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(targetFile));
                    entry.ExtractToFile(targetFile, true);
                }
            }
        }
    }
}