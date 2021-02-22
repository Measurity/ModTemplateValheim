using System;
using System.IO;
using System.Text;
using System.Xml;

namespace BuildTool
{
    public class SteamGameData
    {
        private SteamGameData()
        {
        }

        public SteamGameData(uint id, string name, string installFolderName, string installDir)
        {
            Id = id;
            Name = name;
            InstallFolderName = installFolderName;
            InstallDir = installDir;
        }

        public uint Id { get; private set; }
        public string Name { get; private set; }
        public string InstallFolderName { get; private set; }
        public string InstallDir { get; private set; }

        public string ManagedDllsDir => Path.Combine(InstallDir, "valheim_Data", "Managed");

        public static SteamGameData TryFrom(string path)
        {
            try
            {
                var game = new SteamGameData();
                var xDoc = new XmlDocument();
                xDoc.Load(path);
                XmlNamespaceManager nsManager = new XmlNamespaceManager(xDoc.NameTable);
                nsManager.AddNamespace("d", xDoc.DocumentElement.NamespaceURI);
                foreach (XmlElement elem in xDoc.DocumentElement.SelectNodes("//d:PropertyGroup/*", nsManager))
                {
                    switch (elem.Name)
                    {
                        case "GameAppId":
                            game.Id = uint.Parse(elem.LastChild.Value);
                            break;
                        case "GameName":
                            game.Name = elem.LastChild.Value;
                            break;
                        case "GameFolderName":
                            game.InstallFolderName = elem.LastChild.Value;
                            break;
                        case "GameDir":
                            game.InstallDir = elem.LastChild.Value;
                            break;
                    }
                }
                return game;
            }
            catch (Exception)
            {
                return null;
            }
        }

        public void TrySave(string path)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                using XmlWriter writer = new XmlTextWriter(path, Encoding.UTF8);
                writer.WriteStartElement("Project");
                writer.WriteAttributeString("xmlns", "http://schemas.microsoft.com/developer/msbuild/2003");
                writer.WriteStartElement("PropertyGroup");
                writer.WriteElementString("GameAppId", Id.ToString());
                writer.WriteElementString("GameName", Name);
                writer.WriteElementString("GameFolderName", Utils.PostfixBackslash(InstallFolderName));
                writer.WriteElementString("GameDir", Utils.PostfixBackslash(InstallDir));
                writer.WriteElementString("GameManagedDir", Utils.PostfixBackslash(ManagedDllsDir));
                writer.WriteEndElement();
                writer.WriteEndElement();
            }
            catch
            {
                // ignored
            }
        }
    }
}
