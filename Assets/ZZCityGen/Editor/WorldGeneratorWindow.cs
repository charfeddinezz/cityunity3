using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using ZZCityGen.Generation;
using ZZCityGen.WorldGenerator.Core.Settings;
using ZZCityGen.WorldGenerator.Core.Logging;
using ZZCityGen.WorldGenerator.Core.Validation;

namespace ZZCityGen.Editor
{
    public sealed class WorldGeneratorWindow : EditorWindow
    {
        private WorldGenerator generator;
        private WorldSettings editorWorldSettings;
        private LogLevel selectedLogLevel = LogLevel.Info;
        private bool enableValidation = true;
        private float progressValue;
        private string progressMessage;
        private readonly List<string> errorLog = new List<string>();
        private Vector2 logScroll;

        // UI helpers
        private readonly List<string> validationMessages = new List<string>();
        private string logPreview = string.Empty;
        private Vector2 validationScroll;
        private Vector2 logScrollPos;

        [MenuItem("Tools/ZZ CityGen/World Generator")]
        public static void Open()
        {
            GetWindow<WorldGeneratorWindow>("ZZ CityGen");
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Smart World & City Generator", EditorStyles.boldLabel);
            generator = (WorldGenerator)EditorGUILayout.ObjectField("Generator", generator, typeof(WorldGenerator), true);

            // WorldSettings selector and editor-level controls
            editorWorldSettings = (WorldSettings)EditorGUILayout.ObjectField("World Settings", editorWorldSettings, typeof(WorldSettings), false);
            if (editorWorldSettings != null && generator != null)
            {
                var so = new SerializedObject(generator);
                var prop = so.FindProperty("worldSettings");
                if (prop != null)
                {
                    prop.objectReferenceValue = editorWorldSettings;
                    so.ApplyModifiedProperties();
                }
            }

            // Logging and validation controls
            selectedLogLevel = (LogLevel)EditorGUILayout.EnumPopup("Log Level", selectedLogLevel);
            if (GeneratorLogger.Level != selectedLogLevel)
            {
                GeneratorLogger.Level = selectedLogLevel;
            }

            enableValidation = EditorGUILayout.Toggle("Enable Validation", enableValidation);

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
                EditorGUILayout.Space(6f);
                DrawSettingsAndValidation();
                EditorGUILayout.Space(6f);
                DrawLogViewer();
            }
        }

        private void DrawSettingsAndValidation()
        {
            using (new EditorGUILayout.VerticalScope(GUI.skin.box))
            {
                EditorGUILayout.LabelField("World Settings Preview", EditorStyles.boldLabel);
                if (editorWorldSettings != null)
                {
                    EditorGUILayout.LabelField("Seed", editorWorldSettings.worldSeed.ToString());
                    EditorGUILayout.LabelField("World Size (m)", editorWorldSettings.worldSizeMeters.ToString());
                    EditorGUILayout.LabelField("Cities", editorWorldSettings.cityCount.ToString());
                    EditorGUILayout.LabelField("Road Density", editorWorldSettings.roadDensity.ToString("0.00"));
                    EditorGUILayout.LabelField("Population Density", editorWorldSettings.populationDensity.ToString("0.00"));
                    if (GUILayout.Button("Open WorldSettings"))
                    {
                        EditorGUIUtility.PingObject(editorWorldSettings);
                        Selection.activeObject = editorWorldSettings;
                    }
                }
                else
                {
                    EditorGUILayout.LabelField("No WorldSettings selected.");
                }

                EditorGUILayout.Space(6f);
                EditorGUILayout.LabelField("Validation", EditorStyles.boldLabel);
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("Run Validation Now"))
                    {
                        RunValidationNow();
                    }
                    if (GUILayout.Button("Clear Messages"))
                    {
                        validationMessages.Clear();
                    }
                }

                validationScroll = EditorGUILayout.BeginScrollView(validationScroll, GUILayout.Height(100f));
                foreach (var m in validationMessages)
                {
                    EditorGUILayout.HelpBox(m, MessageType.Warning);
                }
                EditorGUILayout.EndScrollView();
            }
        }

        private void RunValidationNow()
        {
            validationMessages.Clear();
            if (generator == null)
            {
                validationMessages.Add("No generator assigned.");
                return;
            }

            if (generator.CurrentPlan == null)
            {
                validationMessages.Add("No master plan available. Run Master Plan first.");
                return;
            }

            var result = Validator.Validate(generator.CurrentPlan);
            if (result.IsValid)
            {
                validationMessages.Add("Validation passed.");
            }
            else
            {
                validationMessages.AddRange(result.Messages);
            }
        }

        private void DrawLogViewer()
        {
            using (new EditorGUILayout.VerticalScope(GUI.skin.box))
            {
                EditorGUILayout.LabelField("Generator Log", EditorStyles.boldLabel);
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("Refresh Logs")) LoadLogPreview();
                    if (GUILayout.Button("Open Log Folder"))
                    {
                        var folder = Path.Combine(Application.dataPath, "ZZCityGen/WorldGenerator/GeneratedData/Logs");
                        if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);
                        EditorUtility.RevealInFinder(folder);
                    }
                }

                logScrollPos = EditorGUILayout.BeginScrollView(logScrollPos, GUILayout.Height(140f));
                EditorGUILayout.SelectableLabel(logPreview, GUILayout.ExpandHeight(false));
                EditorGUILayout.EndScrollView();
            }
        }

        private void LoadLogPreview()
        {
            try
            {
                var path = Path.Combine(Application.dataPath, "ZZCityGen/WorldGenerator/GeneratedData/Logs/generator.log");
                if (!File.Exists(path))
                {
                    logPreview = "(no log file found)";
                    return;
                }

                var lines = File.ReadAllLines(path);
                var tail = lines.Length > 200 ? lines.Skip(lines.Length - 200).ToArray() : lines;
                logPreview = string.Join("\n", tail);
            }
            catch (System.Exception ex)
            {
                logPreview = "Error loading log: " + ex.Message;
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
                // Optional pre-stage validation
                if (enableValidation && generator != null && generator.CurrentPlan != null)
                {
                    var result = Validator.Validate(generator.CurrentPlan);
                    if (!result.IsValid)
                    {
                        foreach (var m in result.Messages) errorLog.Add($"Validation: {m}");
                        errorLog.Add($"{stageName}: Aborted due to validation failures.");
                        return;
                    }
                }

                progressValue = progress;
                progressMessage = stageName;
                action.Invoke();
                LoadLogPreview();
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
                    // Optional validation before each stage in the full run
                    if (enableValidation && generator != null && generator.CurrentPlan != null)
                    {
                        var result = Validator.Validate(generator.CurrentPlan);
                        if (!result.IsValid)
                        {
                            foreach (var m in result.Messages) errorLog.Add($"Validation: {m}");
                            errorLog.Add($"{stage.name}: Aborted due to validation failures.");
                            break;
                        }
                    }

                    progressValue = stage.progress;
                    progressMessage = stage.name;
                    stage.action.Invoke();
                    LoadLogPreview();
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
}
