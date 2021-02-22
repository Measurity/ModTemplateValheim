using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.Win32;

namespace BuildTool
{
    public static class Steam
    {
        private static readonly Regex steamJsonRegex = new Regex("\"(.*)\"\t*\"(.*)\"", RegexOptions.Compiled);

        /// <summary>
        ///     Finds game install directory by iterating through all the steam game libraries configured and finding the appid
        ///     that matches.
        /// </summary>
        /// <param name="steamAppId"></param>
        /// <returns></returns>
        /// <exception cref="Exception">If steam is not installed or game could not be found.</exception>
        public static SteamGameData FindGame(uint steamAppId)
        {
            var steamPath = (string) ReadRegistrySafe("Software\\Valve\\Steam", "SteamPath");
            if (string.IsNullOrEmpty(steamPath))
            {
                try
                {
                    steamPath = (string) ReadRegistrySafe(@"SOFTWARE\Valve\Steam",
                        "InstallPath",
                        RegistryHive.LocalMachine);
                }
                finally
                {
                    if (string.IsNullOrEmpty(steamPath))
                    {
                        throw new Exception("Steam could not be found. Check if it is installed.");
                    }
                }
            }

            var appsPath = Path.Combine(steamPath, "steamapps");

            // Test main steamapps.
            var game = GameDataFromAppManifest(Path.Combine(appsPath, $"appmanifest_{steamAppId}.acf"));
            if (game == null)
            {
                // Test steamapps on other drives (as defined by Steam).
                game = SearchAllInstallations(Path.Combine(appsPath, "libraryfolders.vdf"), steamAppId);
                if (game == null)
                {
                    throw new Exception($"Steam game with id {steamAppId} is not installed.");
                }
            }

            return game;
        }

        private static SteamGameData SearchAllInstallations(
            string libraryfoldersFile, uint appId)
        {
            if (!File.Exists(libraryfoldersFile)) return null;
            // Turn contents of file into dictionary lookup.
            var steamLibraryData = JsonAsDictionary(File.ReadAllText(libraryfoldersFile));

            var steamLibraryIndex = 0;
            while (true)
            {
                steamLibraryIndex++;
                if (!steamLibraryData.TryGetValue(steamLibraryIndex.ToString(), out var steamLibraryPath)) return null;
                var manifestFile = Path.Combine(steamLibraryPath, $"steamapps/appmanifest_{appId}.acf");
                if (!File.Exists(manifestFile)) continue;

                // Validate manifest is correct.
                var game = GameDataFromAppManifest(manifestFile);
                if (game.Id != appId) continue;

                return game;
            }
        }

        private static SteamGameData GameDataFromAppManifest(string manifestFile)
        {
            var gameData = JsonAsDictionary(File.ReadAllText(manifestFile));

            // Validate steam game data exists.
            if (!gameData.TryGetValue("name", out var gameName)) return null;
            if (!gameData.TryGetValue("appid", out var appidStr)) return null;
            if (!uint.TryParse(appidStr, out var appId)) return null;
            if (!gameData.TryGetValue("installdir", out var gameInstallFolderName)) return null;
            var gameDir =
                Path.GetFullPath(Path.Combine(Path.GetDirectoryName(manifestFile), "common", gameInstallFolderName));
            if (!Directory.Exists(gameDir)) return null;

            return new SteamGameData(appId, gameName, gameInstallFolderName, gameDir);
        }

        private static Dictionary<string, string> JsonAsDictionary(string json)
        {
            return steamJsonRegex.Matches(json)
                .Cast<Match>()
                .ToDictionary(m => m.Groups[1].Value.ToLowerInvariant(), m => m.Groups[2].Value);
        }

        private static object ReadRegistrySafe(string path, string key, RegistryHive hive = RegistryHive.CurrentUser)
        {
            using var subkey = RegistryKey.OpenBaseKey(hive, RegistryView.Registry32).OpenSubKey(path);
            return subkey?.GetValue(key);
        }
    }
}
