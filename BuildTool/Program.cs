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

        private const string NugetExeDownloadUrl = "https://dist.nuget.org/win-x86-commandline/v5.7.0/nuget.exe";
        private const string PublicizerGitRepo = "https://github.com/MrPurple6411/AssemblyPublicizer";
        private const string UnityUnstrippedDllsRepo = "https://github.com/Measurity/OriginalUEngineSources";
        private static readonly Regex iniRegex = new Regex(@"^(\w+)=(\\N+)?", RegexOptions.Compiled);
        private static readonly string[] skipZipExtractContains = {"example", "readme", "changelog"};

        private static uint SteamAppId =>
            !uint.TryParse(Environment.GetEnvironmentVariable("SteamAppId"), out var id) ? 892970 : id;

        private static bool SkipPublicizer => bool.TryParse(Environment.GetEnvironmentVariable("SkipPublicizer"), out bool skip) && skip;

        public static async Task Main(string[] args)
        {
            if (SteamAppId <= 0) throw new Exception("SteamAppId environment variable must be set and be valid");

            var game = await Task.Factory.StartNew(() => EnsureSteamGame(SteamAppId)).ConfigureAwait(false);
            var publicizerTask = EnsurePublicizerAsync()
                .ContinueWith(t => Task.Factory.StartNew(() => EnsurePublicizedAssemblies(game, t.Result)));
            await Task.WhenAll(EnsureBepInExAsync(game.InstallDir), publicizerTask, EnsureUnityDoorstopAsync(game))
                .ConfigureAwait(false);
            await Task.Factory.StartNew(() => EnsureUnstrippedMonoAssemblies(game));
        }

        private static async Task EnsureUnityDoorstopAsync(SteamGameData game)
        {
            if (File.Exists(Path.Combine(game.InstallDir, "doorstop_config.ini")))
            {
                Console.WriteLine("Unity Doorstop is already installed.");
                return;
            }

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

        private static void EnsureUnstrippedMonoAssemblies(SteamGameData game)
        {
            const string unstrippedDllsFolderName = "unstripped_dlls";
            if (Directory.Exists(Path.Combine(game.InstallDir, unstrippedDllsFolderName)))
            {
                Console.WriteLine("Unstripped Mono Assemblies are already installed.");
                return;
            }

            // Download or update local Unity Engine dlls repo through git.
            var repoDir = Path.Combine(Utils.GeneratedOutputDir, "OriginalUEngineSources");
            if (!Directory.Exists(repoDir) || !Directory.GetFiles(repoDir).Any())
            {
                Utils.ExecuteShell($@"git clone ""{UnityUnstrippedDllsRepo}""");
            }
            else
            {
                Utils.ExecuteShell(@"git fetch --all", repoDir);
            }
            // Go to git branch with the clean dlls.
            var unityVersionUsedByGame = new Version(FileVersionInfo
                .GetVersionInfo(Path.Combine(game.InstallDir, "UnityPlayer.dll"))
                .FileVersion);
            if (Utils.ExecuteShell($@"git checkout ""{unityVersionUsedByGame.ToString(3)}""", repoDir) != 0)
            {
                throw new Exception(
                    $"Failed to checkout Unity dll branch '{unityVersionUsedByGame.ToString(3)}' on repo {UnityUnstrippedDllsRepo}");
            }
            // Copy clean dlls to game.
            var targetDir = Path.Combine(game.InstallDir, unstrippedDllsFolderName);
            Directory.CreateDirectory(targetDir);
            foreach (var sourceFile in Directory.GetFiles(repoDir, "*.dll"))
            {
                var targetFile = Path.Combine(targetDir, Path.GetFileName(sourceFile));
                File.Copy(sourceFile, targetFile, true);
            }
            // Change UnityDoorstop configuration to use clean dlls.
            var unityDoorstopConfig = Path.Combine(game.InstallDir, "doorstop_config.ini");
            if (!File.Exists(unityDoorstopConfig))
            {
                throw new FileNotFoundException("UnityDoorstop config file not found", unityDoorstopConfig);
            }
            var configContent = File.ReadAllLines(unityDoorstopConfig).ToList();
            var requiredValues = new Dictionary<string, string>
            {
                {"dllSearchPathOverride", unstrippedDllsFolderName},
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

        private static void EnsurePublicizedAssemblies(SteamGameData game, string publicizerExe)
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
