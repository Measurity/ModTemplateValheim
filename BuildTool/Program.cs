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
        private const string BepInExDownloadUrl =
            "https://github.com/BepInEx/BepInEx/releases/download/v5.4.5/BepInEx_x64_5.4.5.0.zip";

        private const string NugetExeDownloadUrl = "https://dist.nuget.org/win-x86-commandline/v5.7.0/nuget.exe";
        private const string PublicizerGitRepo = "https://github.com/MrPurple6411/AssemblyPublicizer";

        private static uint SteamAppId =>
            !uint.TryParse(Environment.GetEnvironmentVariable("SteamAppId"), out var id) ? 0 : id;

        public static async Task Main(string[] args)
        {
            if (SteamAppId <= 0) throw new Exception("SteamAppId environment variable must be set and be valid");

            var game = await Task.Run(() => EnsureSteamGame(SteamAppId));
            var publicizerTask = EnsurePublicizerAsync();
            await Task.WhenAll(EnsureBepInExAsync(game.InstallDir), publicizerTask);
            EnsurePublicizedAssemblies(game, publicizerTask.Result);
        }

        private static void EnsurePublicizedAssemblies(SteamGameData game, string publicizerExe)
        {
            var publicizerArgs = string.Join(" ",
                Directory.GetFiles(game.ManagedDllsDir, "assembly_*.dll").Select(dll => @$"""{dll}"""));
            Utils.ExecuteShell($@"""{publicizerExe}"" {publicizerArgs}", game.ManagedDllsDir);
            // Copy publicized dlls to this solution
            var targetDir = Path.Combine(Utils.GeneratedOutputDir, "publicized_assemblies");
            Directory.CreateDirectory(targetDir);
            foreach (var dll in Directory.GetFiles(Path.Combine(game.ManagedDllsDir, "publicized_assemblies")))
            {
                File.Copy(dll, Path.Combine(targetDir, Path.GetFileName(dll)), true);
            }
        }

        private static SteamGameData EnsureSteamGame(uint steamAppId)
        {
            static string ValidateUnityGame(SteamGameData game)
            {
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
            if (game == null || ValidateUnityGame(game) != null)
            {
                game = Steam.FindGame(steamAppId);
            }

            var error = ValidateUnityGame(game);
            if (error != null)
            {
                throw new Exception(error);
            }

            game.TrySave(cacheFile);
            return game;
        }

        private static async Task<string> EnsurePublicizerAsync()
        {
            var repoDir = Path.Combine(Utils.GeneratedOutputDir, "AssemblyPublicizer");
            var exe = Path.Combine(repoDir, "AssemblyPublicizer", "bin", "Release", "AssemblyPublicizer.exe");
            if (File.Exists(exe))
            {
                return exe;
            }

            var nugetTask = EnsureNugetAsync();
            // Download or update local publicizer repo through git.
            if (!Directory.Exists(repoDir) || !Directory.GetFiles(repoDir).Any())
            {
                Utils.ExecuteShell($@"git clone ""{PublicizerGitRepo}""");
            }
            else
            {
                Utils.ExecuteShell(@"git pull --rebase origin master", repoDir);
            }
            // Build project: restore nuget + msbuild
            var nuget = await nugetTask;
            Utils.ExecuteShell($"\"{nuget}\" restore && \"{Utils.MsBuildExe}\" /t:Build /p:Configuration=Release",
                repoDir);
            if (!File.Exists(exe))
            {
                throw new FileNotFoundException("Publicizer was not found after MSBuild task.", exe);
            }
            return exe;
        }

        private static async Task<string> EnsureNugetAsync()
        {
            var nuget = Path.Combine(Utils.GeneratedOutputDir, "nuget.exe");
            if (File.Exists(nuget)) return nuget;
            using (var client = new WebClient())
            {
                await client.DownloadFileTaskAsync(NugetExeDownloadUrl, nuget);
            }
            return nuget;
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
                using (var client = new WebClient())
                {
                    await client.DownloadFileTaskAsync(BepInExDownloadUrl, zip);
                }
            }
            // Extract BepInEx zip over game files.
            using (var zipReader = ZipFile.OpenRead(zip))
            {
                foreach (var entry in zipReader.Entries)
                {
                    var targetFile = Path.Combine(gameDir, entry.FullName);
                    Directory.CreateDirectory(Path.GetDirectoryName(targetFile));
                    entry.ExtractToFile(targetFile, true);
                }
            }
        }
    }
}
