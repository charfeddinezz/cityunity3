using System.Collections.Generic;
using UnityEngine;
using ZZCityGen.Data;

namespace ZZCityGen.Simulation
{
    public sealed class TrafficSystem : MonoBehaviour
    {
        [SerializeField] private int carUnits;
        [SerializeField] private int busUnits;
        [SerializeField] private int trainUnits;
        [SerializeField] private int metroUnits;
        [SerializeField] private float trafficFlowIndex;

        private readonly List<TrafficRoutePlan> routes = new List<TrafficRoutePlan>();
        private readonly List<VehicleInstance> vehicles = new List<VehicleInstance>();
        private WorldGenerationSettings settings;

        public void Configure(MasterPlan plan, WorldGenerationSettings generationSettings)
        {
            settings = generationSettings;
            routes.Clear();
            vehicles.Clear();

            if (plan?.trafficRoutes != null)
            {
                routes.AddRange(plan.trafficRoutes);
            }

            carUnits = routes.FindAll(route => route.type == TransportType.Car).Count * 2;
            busUnits = routes.FindAll(route => route.type == TransportType.Bus).Count * 2;
            trainUnits = routes.FindAll(route => route.type == TransportType.Rail).Count * 1;
            metroUnits = routes.FindAll(route => route.type == TransportType.Metro).Count * 1;
            trafficFlowIndex = CalculateTrafficFlow();

            if (Application.isPlaying && settings != null && settings.enableTrafficSimulation)
            {
                SpawnRouteVehicles();
            }
        }

        private void Update()
        {
            if (settings == null || !settings.enableTrafficSimulation || !Application.isPlaying)
            {
                return;
            }

            foreach (var vehicle in vehicles)
            {
                if (vehicle == null || vehicle.Route == null || vehicle.Route.pathPoints.Count < 2)
                {
                    continue;
                }

                vehicle.Progress += Time.deltaTime * vehicle.Speed;
                if (vehicle.Progress >= 1f)
                {
                    vehicle.Progress = 0f;
                }

                var position = Vector3.Lerp(vehicle.PathStart, vehicle.PathEnd, vehicle.Progress);
                vehicle.Instance.transform.position = position;
            }
        }

        private float CalculateTrafficFlow()
        {
            var routeCount = Mathf.Max(1, routes.Count);
            return Mathf.Clamp01((carUnits * 0.12f + busUnits * 0.24f + trainUnits * 0.5f + metroUnits * 0.55f) / (routeCount * 6f));
        }

        private void SpawnRouteVehicles()
        {
            var spawnedCount = 0;
            foreach (var route in routes)
            {
                if (route.pathPoints.Count < 2)
                {
                    continue;
                }

                var start = new Vector3(route.pathPoints[0].x, 0.4f, route.pathPoints[0].y);
                var end = new Vector3(route.pathPoints[route.pathPoints.Count - 1].x, 0.4f, route.pathPoints[route.pathPoints.Count - 1].y);
                var routeSpeed = Mathf.Clamp(12f + route.frequencyPerHour * 0.5f, 10f, 34f);
                var units = route.type == TransportType.Car ? 1 : route.type == TransportType.Bus ? 1 : 1;
                for (var index = 0; index < units; index++)
                {
                    var instance = GameObject.CreatePrimitive(GetVehiclePrimitive(route.type));
                    instance.name = $"{route.name} Vehicle {index + 1}";
                    instance.transform.SetParent(transform, false);
                    instance.transform.position = start;
                    instance.transform.localScale = Vector3.one * 0.7f;
                    ApplyColor(instance, GetTrafficRouteColor(route.type));

                    vehicles.Add(new VehicleInstance
                    {
                        Instance = instance,
                        Route = route,
                        Progress = index / (float)Mathf.Max(1, units),
                        Speed = routeSpeed * 0.014f,
                        PathStart = start,
                        PathEnd = end
                    });

                    spawnedCount++;
                    if (spawnedCount >= 20)
                    {
                        return;
                    }
                }
            }
        }

        private PrimitiveType GetVehiclePrimitive(TransportType type)
        {
            switch (type)
            {
                case TransportType.Car:
                    return PrimitiveType.Sphere;
                case TransportType.Bus:
                    return PrimitiveType.Cube;
                case TransportType.Rail:
                    return PrimitiveType.Cylinder;
                case TransportType.Metro:
                    return PrimitiveType.Capsule;
                default:
                    return PrimitiveType.Sphere;
            }
        }

        private Color GetTrafficRouteColor(TransportType type)
        {
            switch (type)
            {
                case TransportType.Car:
                    return Color.gray;
                case TransportType.Bus:
                    return Color.red;
                case TransportType.Rail:
                    return Color.blue;
                case TransportType.Metro:
                    return new Color(0.2f, 0.8f, 1f, 1f);
                default:
                    return Color.white;
            }
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

        private sealed class VehicleInstance
        {
            public GameObject Instance;
            public TrafficRoutePlan Route;
            public float Progress;
            public float Speed;
            public Vector3 PathStart;
            public Vector3 PathEnd;
        }
    }
}
