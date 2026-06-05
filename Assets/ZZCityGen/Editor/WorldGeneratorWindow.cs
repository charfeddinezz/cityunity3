using UnityEditor;
using UnityEngine;
using ZZCityGen.Generation;

namespace ZZCityGen.Editor
{
    public sealed class WorldGeneratorWindow : EditorWindow
    {
        private WorldGenerator generator;
        private float progressValue;
        private string progressMessage;
        private readonly List<string> errorLog = new List<string>();
        private Vector2 logScroll;

        [MenuItem("Tools/ZZ CityGen/World Generator")]
        public static void Open()
        {
            GetWindow<WorldGeneratorWindow>("ZZ CityGen");
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Smart World & City Generator", EditorStyles.boldLabel);
            generator = (WorldGenerator)EditorGUILayout.ObjectField("Generator", generator, typeof(WorldGenerator), true);

            if (generator == null && GUILayout.Button("Create Generator In Scene"))
            {
                var host = new GameObject("ZZ CityGen Controller");
                generator = host.AddComponent<WorldGenerator>();
                Selection.activeGameObject = host;
            }

            if (generator != null)
            {
                EditorGUILayout.Space(8f);
                DrawProgressBar();
                EditorGUILayout.Space(8f);
                DrawDashboardButtons();
                EditorGUILayout.Space(8f);
                DrawErrorLog();
            }
        }

        private void DrawProgressBar()
        {
            if (progressValue > 0f)
            {
                var rect = GUILayoutUtility.GetRect(18f, 18f, GUILayout.ExpandWidth(true));
                EditorGUI.ProgressBar(rect, progressValue, progressMessage);
            }
            else
            {
                EditorGUILayout.LabelField("Progress", EditorStyles.boldLabel);
                EditorGUILayout.LabelField("Ready to generate.");
            }
        }

        private void DrawDashboardButtons()
        {
            using (new EditorGUILayout.VerticalScope(GUI.skin.box))
            {
                EditorGUILayout.LabelField("Build Sequence", EditorStyles.boldLabel);
                DrawStageButton("Master Plan", () => RunStage(() => generator.GenerateMasterPlan(), 0.08f, "Master Plan"));
                DrawStageButton("Terrain", () => RunStage(() => generator.GenerateTerrain(), 0.16f, "Terrain"));
                DrawStageButton("Roads", () => RunStage(() => generator.GenerateRoads(), 0.24f, "Roads"));
                DrawStageButton("Cities", () => RunStage(() => generator.GenerateCities(), 0.32f, "Cities"));
                DrawStageButton("Districts", () => RunStage(() => generator.GenerateDistricts(), 0.40f, "Districts"));
                DrawStageButton("Lots", () => RunStage(() => generator.GenerateLots(), 0.48f, "Lots"));
                DrawStageButton("Parks", () => RunStage(() => generator.GenerateParks(), 0.56f, "Parks"));
                DrawStageButton("Buildings", () => RunStage(() => generator.GenerateBuildings(), 0.64f, "Buildings"));
                DrawStageButton("Infrastructure", () => RunStage(() => generator.GenerateInfrastructure(), 0.72f, "Infrastructure"));
                DrawStageButton("Traffic", () => RunStage(() => generator.GenerateTrafficSystem(), 0.80f, "Traffic"));
                DrawStageButton("Optimization", () => RunStage(() => generator.OptimizeWorld(), 0.90f, "Optimization"));
                DrawStageButton("Save", () => RunStage(() => generator.SaveWorld(), 1f, "Save"));
                EditorGUILayout.Space(4f);
                if (GUILayout.Button("Generate Entire World", GUILayout.Height(32f)))
                {
                    RunEntireWorld();
                }
            }
        }

        private void DrawErrorLog()
        {
            EditorGUILayout.LabelField("Error Log", EditorStyles.boldLabel);
            var scrollView = new EditorGUILayout.ScrollViewScope(logScroll, GUILayout.Height(150f));
            logScroll = scrollView.scrollPosition;
            foreach (var error in errorLog)
            {
                EditorGUILayout.HelpBox(error, MessageType.Error);
            }
            scrollView.Dispose();
        }

        private void RunStage(System.Action action, float progress, string stageName)
        {
            try
            {
                progressValue = progress;
                progressMessage = stageName;
                action.Invoke();
                progressValue = 0f;
                progressMessage = string.Empty;
            }
            catch (System.Exception ex)
            {
                errorLog.Add($"{stageName}: {ex.Message}");
            }
        }

        private void RunEntireWorld()
        {
            errorLog.Clear();
            var stages = new (System.Action action, float progress, string name)[]
            {
                (() => generator.GenerateMasterPlan(), 0.08f, "Master Plan"),
                (() => generator.GenerateTerrain(), 0.16f, "Terrain"),
                (() => generator.GenerateRoads(), 0.24f, "Roads"),
                (() => generator.GenerateCities(), 0.32f, "Cities"),
                (() => generator.GenerateDistricts(), 0.40f, "Districts"),
                (() => generator.GenerateLots(), 0.48f, "Lots"),
                (() => generator.GenerateParks(), 0.56f, "Parks"),
                (() => generator.GenerateBuildings(), 0.64f, "Buildings"),
                (() => generator.GenerateInfrastructure(), 0.72f, "Infrastructure"),
                (() => generator.GenerateTrafficSystem(), 0.80f, "Traffic"),
                (() => generator.OptimizeWorld(), 0.90f, "Optimization"),
                (() => generator.SaveWorld(), 1f, "Save")
            };

            foreach (var stage in stages)
            {
                try
                {
                    progressValue = stage.progress;
                    progressMessage = stage.name;
                    stage.action.Invoke();
                }
                catch (System.Exception ex)
                {
                    errorLog.Add($"{stage.name}: {ex.Message}");
                    break;
                }
            }

            progressValue = 0f;
            progressMessage = string.Empty;
        }

        private static void DrawStageButton(string label, System.Action action)
        {
            if (GUILayout.Button(label))
            {
                action.Invoke();
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
