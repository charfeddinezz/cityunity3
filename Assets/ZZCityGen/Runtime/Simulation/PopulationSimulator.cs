using System.Collections.Generic;
using UnityEngine;
using ZZCityGen.Data;

namespace ZZCityGen.Simulation
{
    public sealed class PopulationSimulator : MonoBehaviour
    {
        [SerializeField] private int currentPopulation;
        [SerializeField] private int activeJobs;
        [SerializeField] private int unemployed;
        [SerializeField] private int pedestrianUnits;
        [SerializeField] private float pedestrianFlowIndex;

        private readonly List<PopulationClusterPlan> clusters = new List<PopulationClusterPlan>();
        private readonly List<PedestrianRoutePlan> pedestrianRoutes = new List<PedestrianRoutePlan>();
        private readonly List<PedestrianInstance> pedestrians = new List<PedestrianInstance>();
        private WorldGenerationSettings settings;
        private float simulatedYears;

        public void Configure(MasterPlan plan, WorldGenerationSettings generationSettings)
        {
            settings = generationSettings;
            clusters.Clear();
            pedestrianRoutes.Clear();
            pedestrians.Clear();

            if (plan == null)
            {
                return;
            }

            clusters.AddRange(plan.populationClusters);
            pedestrianRoutes.AddRange(plan.pedestrianRoutes);
            currentPopulation = plan.economy.totalPopulation;
            activeJobs = plan.economy.estimatedJobs;
            unemployed = Mathf.Max(0, currentPopulation - activeJobs);
            pedestrianUnits = Mathf.Min(60, pedestrianRoutes.Count * 3 + clusters.Count / 4);
            pedestrianFlowIndex = CalculatePedestrianFlow();

            if (Application.isPlaying && settings != null && settings.enablePedestrianSimulation)
            {
                SpawnPedestrians();
            }
        }

        private void Update()
        {
            if (settings == null || !settings.enablePopulationSimulation || !Application.isPlaying)
            {
                return;
            }

            var yearsDelta = Time.deltaTime / 60f;
            simulatedYears += yearsDelta;
            if (settings.enableDynamicGrowth)
            {
                var growth = Mathf.RoundToInt(currentPopulation * (0.006f + settings.economicDevelopment * 0.004f) * yearsDelta);
                currentPopulation += growth;
                activeJobs += Mathf.RoundToInt(growth * 0.5f);
                unemployed = Mathf.Max(0, currentPopulation - activeJobs);
            }

            if (pedestrianRoutes.Count == 0)
            {
                return;
            }

            foreach (var pedestrian in pedestrians)
            {
                if (pedestrian == null || pedestrian.Instance == null || pedestrian.Route == null || pedestrian.Route.pathPoints.Count < 2)
                {
                    continue;
                }

                pedestrian.Progress += Time.deltaTime * pedestrian.Speed;
                if (pedestrian.Progress >= 1f)
                {
                    pedestrian.Progress = 0f;
                    pedestrian.Route = pedestrianRoutes[Random.Range(0, pedestrianRoutes.Count)];
                    pedestrian.PathStart = ToWorld(pedestrian.Route.pathPoints[0], 0.2f);
                    pedestrian.PathEnd = ToWorld(pedestrian.Route.pathPoints[pedestrian.Route.pathPoints.Count - 1], 0.2f);
                }

                pedestrian.Instance.transform.position = Vector3.Lerp(pedestrian.PathStart, pedestrian.PathEnd, pedestrian.Progress);
            }
        }

        private float CalculatePedestrianFlow()
        {
            if (pedestrianRoutes.Count == 0)
            {
                return 0f;
            }

            var totalIndex = 0f;
            foreach (var route in pedestrianRoutes)
            {
                totalIndex += route.footTrafficIndex;
            }

            return Mathf.Clamp01(totalIndex / Mathf.Max(1, pedestrianRoutes.Count));
        }

        private void SpawnPedestrians()
        {
            if (pedestrianRoutes.Count == 0)
            {
                return;
            }

            var spawnCount = Mathf.Clamp(pedestrianUnits, 6, 40);
            for (var index = 0; index < spawnCount; index++)
            {
                var route = pedestrianRoutes[index % pedestrianRoutes.Count];
                if (route.pathPoints.Count < 2)
                {
                    continue;
                }

                var start = ToWorld(route.pathPoints[0], 0.2f);
                var end = ToWorld(route.pathPoints[route.pathPoints.Count - 1], 0.2f);
                var instance = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                instance.name = $"Pedestrian {index + 1} - {route.name}";
                instance.transform.SetParent(transform, false);
                instance.transform.position = start;
                instance.transform.localScale = Vector3.one * 0.35f;
                ApplyColor(instance, new Color(1f, 0.85f, 0.55f, 1f));

                pedestrians.Add(new PedestrianInstance
                {
                    Instance = instance,
                    Route = route,
                    Progress = Random.Range(0f, 1f),
                    Speed = Mathf.Clamp01(0.35f + route.footTrafficIndex * 0.4f),
                    PathStart = start,
                    PathEnd = end
                });
            }
        }

        private Vector3 ToWorld(Vector2 planPosition, float y)
        {
            return new Vector3(planPosition.x, y, planPosition.y);
        }

        private void ApplyColor(GameObject gameObject, Color color)
        {
            var renderer = gameObject.GetComponent<Renderer>();
            if (renderer == null)
            {
                return;
            }

            renderer.sharedMaterial = new Material(Shader.Find("Standard")) { color = color };
        }

        private sealed class PedestrianInstance
        {
            public GameObject Instance;
            public PedestrianRoutePlan Route;
            public float Progress;
            public float Speed;
            public Vector3 PathStart;
            public Vector3 PathEnd;
        }
    }
}
