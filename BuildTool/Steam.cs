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
                throw new Exception("Steam could not be found. Check if it is installed.");
            }
            var appsPath = Path.Combine(steamPath, "steamapps");
            var result = SearchAllInstallations(Path.Combine(appsPath, "libraryfolders.vdf"), steamAppId);
            if (result == null)
            {
                throw new Exception($"Steam game with id {steamAppId} is not installed.");
            }
            return result;
        }

        private static SteamGameData SearchAllInstallations(
            string libraryfoldersFile, uint appid)
        {
            if (!File.Exists(libraryfoldersFile)) return null;
            // Turn contents of file into dictionary lookup.
            var steamLibraryData = JsonAsDictionary(File.ReadAllText(libraryfoldersFile));

            var steamLibraryIndex = 0;
            while (true)
            {
                steamLibraryIndex++;
                if (!steamLibraryData.TryGetValue(steamLibraryIndex.ToString(), out var steamLibraryPath)) return null;
                var steamAppDataFile = Path.Combine(steamLibraryPath, $"steamapps/appmanifest_{appid}.acf");
                if (!File.Exists(steamAppDataFile)) continue;

                var gameData = JsonAsDictionary(File.ReadAllText(steamAppDataFile));

                // Validate steam game data exists.
                if (!gameData.TryGetValue("name", out var gameName)) continue;
                if (!gameData.TryGetValue("appid", out var appidStr)) continue;
                if (!gameData.TryGetValue("installdir", out var gameInstallFolderName)) continue;
                // Validate Steam ID matches.
                if (!uint.TryParse(appidStr, out var appIdFromData)) continue;
                if (appIdFromData != appid) continue;
                // Validate game Path exists.
                var gameDir =
                    Path.GetFullPath(Path.Combine(steamLibraryPath, "steamapps/common", gameInstallFolderName));
                if (!Directory.Exists(gameDir)) continue;

                return new SteamGameData(appid, gameName, gameInstallFolderName, gameDir);
            }
        }

        private static Dictionary<string, string> JsonAsDictionary(string json)
        {
            return steamJsonRegex.Matches(json)
                .Cast<Match>()
                .ToDictionary(m => m.Groups[1].Value.ToLowerInvariant(), m => m.Groups[2].Value);
        }

        private static object ReadRegistrySafe(string path, string key)
        {
            using (var subkey = Registry.CurrentUser.OpenSubKey(path))
            {
                if (subkey != null)
                {
                    return subkey.GetValue(key);
                }
            }

            return null;
        }
    }
}
