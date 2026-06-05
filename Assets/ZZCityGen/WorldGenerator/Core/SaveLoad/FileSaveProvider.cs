using System.IO;
using UnityEngine;

namespace ZZCityGen.WorldGenerator.Core.SaveLoad
{
    public class FileSaveProvider : ISaveLoadProvider
    {
        private readonly string baseFolder;

        public FileSaveProvider(string relativeFolder = "Assets/ZZCityGen/WorldGenerator/GeneratedData/Saves")
        {
            baseFolder = Path.Combine(Application.dataPath, relativeFolder.Replace("Assets/", ""));
            Directory.CreateDirectory(baseFolder);
        }

        public string Save(string key, string json)
        {
            var path = Path.Combine(baseFolder, key + ".json");
            File.WriteAllText(path, json);
            return path;
        }

        public string Load(string key)
        {
            var path = Path.Combine(baseFolder, key + ".json");
            if (!File.Exists(path)) return null;
            return File.ReadAllText(path);
        }

        public bool Exists(string key)
        {
            var path = Path.Combine(baseFolder, key + ".json");
            return File.Exists(path);
        }
    }
}