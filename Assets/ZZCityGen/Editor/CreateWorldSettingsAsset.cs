using UnityEditor;
using UnityEngine;
using ZZCityGen.WorldGenerator.Core.Settings;

public static class CreateWorldSettingsAsset
{
    [MenuItem("Assets/Create/ZZCityGen/World Settings", priority = 110)]
    public static void CreateAsset()
    {
        var asset = ScriptableObject.CreateInstance<WorldSettings>();
        var path = "Assets/ZZCityGen/WorldGenerator/Resources";
        if (!AssetDatabase.IsValidFolder(path)) AssetDatabase.CreateFolder("Assets/ZZCityGen/WorldGenerator", "Resources");
        var assetPath = AssetDatabase.GenerateUniqueAssetPath(path + "/WorldSettings.asset");
        AssetDatabase.CreateAsset(asset, assetPath);
        AssetDatabase.SaveAssets();
        EditorUtility.FocusProjectWindow();
        Selection.activeObject = asset;
    }
}