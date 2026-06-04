using System.IO;
using UnityEngine;
using ZZCityGen.Data;

namespace ZZCityGen.Core
{
    public static class WorldSaveUtility
    {
        public static void SaveMasterPlan(MasterPlan plan, string filePath)
        {
            var json = JsonUtility.ToJson(plan, true);
            var directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }
            File.WriteAllText(filePath, json);
        }

        public static MasterPlan LoadMasterPlan(string filePath)
        {
            return JsonUtility.FromJson<MasterPlan>(File.ReadAllText(filePath));
        }
    }
}
