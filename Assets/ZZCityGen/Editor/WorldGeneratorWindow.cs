using UnityEditor;
using UnityEngine;
using ZZCityGen.Generation;

namespace ZZCityGen.Editor
{
    public sealed class WorldGeneratorWindow : EditorWindow
    {
        private WorldGenerator generator;

        [MenuItem("Tools/World Generator Window")]
        [MenuItem("Tools/ZZ CityGen/World Generator")]
        public static void Open()
        {
            GetWindow<WorldGeneratorWindow>("World Generator Window");
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("World Generator Window", EditorStyles.boldLabel);
            generator = (WorldGenerator)EditorGUILayout.ObjectField("Generator", generator, typeof(WorldGenerator), true);

            if (generator == null && GUILayout.Button("Create Generator In Scene"))
            {
                var host = new GameObject("ZZ CityGen Controller");
                generator = host.AddComponent<WorldGenerator>();
                Selection.activeGameObject = host;
            }

            using (new EditorGUI.DisabledScope(generator == null))
            {
                DrawStageButton("Generate Master Plan", () => generator.GenerateMasterPlan());
                DrawStageButton("2. Generate Terrain", () => generator.GenerateTerrain());
                DrawStageButton("3. Generate Cities & Districts", () => generator.GenerateCities());
                DrawStageButton("4. Generate Transport", () => generator.GenerateTransport());
                DrawStageButton("5. Generate Infrastructure & Landmarks", () => generator.GenerateInfrastructure());
                DrawStageButton("6. Configure Economy, Traffic & Streaming", () => generator.ConfigureSimulation());
                DrawStageButton("7. Optimize World", () => generator.OptimizeWorld());

                EditorGUILayout.Space(8f);
                if (GUILayout.Button("Generate Complete World", GUILayout.Height(34f)))
                {
                    generator.GenerateAll();
                    EditorUtility.SetDirty(generator);
                }
            }
        }

        private static void DrawStageButton(string label, System.Action action)
        {
            if (GUILayout.Button(label))
            {
                action.Invoke();
            }
        }
    }
}
