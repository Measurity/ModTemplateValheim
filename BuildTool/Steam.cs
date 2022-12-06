﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.Win32;

namespace BuildTool
{
    public static class Steam
    {
        /// <summary>
        ///     Finds game install directory by iterating through all the steam game libraries configured and finding the appid
        ///     that matches.
        /// </summary>
        /// <param name="steamAppId"></param>
        /// <returns></returns>
        /// <exception cref="Exception">If steam is not installed or game could not be found.</exception>
        public static SteamGameData FindGame(uint steamAppId)
        {
            var steamPath = (string)ReadRegistrySafe("Software\\Valve\\Steam", "SteamPath");
            if (string.IsNullOrEmpty(steamPath))
            {
                try
                {
                    steamPath = (string)ReadRegistrySafe(@"SOFTWARE\Valve\Steam",
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
            SteamGameData game = GameDataFromAppManifest(Path.Combine(appsPath, $"appmanifest_{steamAppId}.acf"));
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
            string libraryfoldersFile,
            uint appId)
        {
            if (!File.Exists(libraryfoldersFile)) return null;

            var steamLibraryPaths = GetLibraryPaths(File.ReadAllText(libraryfoldersFile));

            foreach (var steamLibraryPath in steamLibraryPaths)
            {
                var manifestFile = Path.Combine(steamLibraryPath, $"steamapps/appmanifest_{appId}.acf");
                if (!File.Exists(manifestFile)) continue;

                // Validate manifest is correct.
                SteamGameData game = GameDataFromAppManifest(manifestFile);
                if (game.Id != appId) continue;

                return game;
            }

            return null;
        }

        private static SteamGameData GameDataFromAppManifest(string manifestFile)
        {
            Dictionary<string, string> gameData;
            try
            {
                gameData = JsonAsDictionary(File.ReadAllText(manifestFile));
            }
            catch (FileNotFoundException)
            {
                return null;
            }

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
            Regex regex = new("\"(.*)\"\t*\"(.*)\"");
            Dictionary<string, string> result = new();
            foreach (Match match in regex.Matches(json).Cast<Match>())
            {
                result[match.Groups[1].Value.ToLowerInvariant()] = match.Groups[2].Value;
            }
            return result;
        }

        private static string[] GetLibraryPaths(string json)
        {
            Regex regex = new("\"path\"\t*\"(.*)\"", RegexOptions.Compiled);
            return regex.Matches(json)
                .OfType<Match>()
                .Select(m => m.Groups[1].Value)
                .ToArray();
        }

        private static object ReadRegistrySafe(string path, string key, RegistryHive hive = RegistryHive.CurrentUser)
        {
            using RegistryKey subkey = RegistryKey.OpenBaseKey(hive, RegistryView.Registry32).OpenSubKey(path);
            return subkey?.GetValue(key);
        }
    }
}