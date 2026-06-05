using System.IO;
using UnityEngine;
using ZZCityGen.Core;
using ZZCityGen.Data;
using ZZCityGen.Planning;
using ZZCityGen.Plugins;
using ZZCityGen.Simulation;
using ZZCityGen.Streaming;

namespace ZZCityGen.Generation
{
    [ExecuteAlways]
    public sealed class WorldGenerator : MonoBehaviour
    {
        [SerializeField] private WorldGenerationSettings settings = new WorldGenerationSettings();
        [SerializeField] private AssetCatalog assetCatalog;
        [SerializeField] private PrefabDatabase prefabDatabase;
        [SerializeField] private Transform generatedRoot;

        private MasterPlan currentPlan;
        private ChunkStreamingController streamingController;
        private EconomySimulator economySimulator;
        private TrafficSimulator trafficSimulator;
        private TrafficSystem trafficSystem;
        private PluginRegistry pluginRegistry;

        public WorldGenerationSettings Settings => settings;
        public MasterPlan CurrentPlan => currentPlan;

        public void GenerateMasterPlan()
        {
            currentPlan = new MasterPlanBuilder(settings).Build();
            EnsureRuntimeSystems();
            pluginRegistry.ApplyMasterPlanExtensions(currentPlan, settings);
            currentPlan.worldPlan = WorldPlan.FromMasterPlan(currentPlan);
            var path = GetWorldPlanPath();
            WorldSaveUtility.SaveWorldPlan(currentPlan.worldPlan, path);
            Debug.Log($"World plan saved to {path}");
        }

        private static string GetWorldPlanPath()
        {
            return Path.Combine(Application.dataPath, "..", "world_plan.json");
        }

        private static string GetTerrainPlanPath()
        {
            return Path.Combine(Application.dataPath, "..", "terrain_data.json");
        }

        private static string GetRoadNetworkPath()
        {
            return Path.Combine(Application.dataPath, "..", "road_network.json");
        }

        private static string GetCityDataPath()
        {
            return Path.Combine(Application.dataPath, "..", "city_data.json");
        }

        public void GenerateTerrain()
        {
            EnsurePlan();
            EnsureRoot();
            ClearChildren("Terrain");
            var terrainRoot = CreateStageRoot("Terrain");

            var terrainPath = GetTerrainPlanPath();
            WorldSaveUtility.SaveTerrainPlan(currentPlan.terrainPlan, terrainPath);
            Debug.Log($"Terrain data saved to {terrainPath}");

            foreach (var feature in currentPlan.naturalFeatures)
            {
                var marker = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                marker.name = $"{feature.name} ({feature.startElevation:0.00}->{feature.endElevation:0.00})";
                marker.transform.SetParent(terrainRoot, false);
                marker.transform.position = ToWorld(feature.start, 0f);
                var radius = Mathf.Max(2f, feature.widthOrRadius * 0.5f);
                marker.transform.localScale = new Vector3(radius, 2f + feature.startElevation * 24f, radius);
            }

            if (currentPlan.terrainAnalysis.Count <= 512)
            {
                var analysisRoot = new GameObject("Terrain Suitability Cells");
                analysisRoot.transform.SetParent(terrainRoot, false);
                foreach (var cell in currentPlan.terrainAnalysis)
                {
                    var marker = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    marker.name = $"Suitability City {cell.citySuitabilityScore:0.00} Port {cell.portSuitabilityScore:0.00} Airport {cell.airportSuitabilityScore:0.00}";
                    marker.transform.SetParent(analysisRoot.transform, false);
                    marker.transform.position = ToWorld(cell.center, -0.15f);
                    marker.transform.localScale = new Vector3(cell.bounds.width * 0.72f, 0.3f + cell.buildabilityScore, cell.bounds.height * 0.72f);
                }
            }
        }

        public void GenerateCities()
        {
            EnsurePlan();
            EnsureRoot();
            ClearChildren("Cities");
            var cityRoot = CreateStageRoot("Cities");

            var cityDataPath = GetCityDataPath();
            WorldSaveUtility.SaveCityData(new CityDataPackage { cities = currentPlan.cities }, cityDataPath);
            Debug.Log($"City data saved to {cityDataPath}");

            foreach (var city in currentPlan.cities)
            {
                var cityObject = new GameObject(city.name);
                cityObject.transform.SetParent(cityRoot, false);
                cityObject.transform.position = ToWorld(city.position, 0f);

                foreach (var district in city.districts)
                {
                    var districtObject = new GameObject(district.name);
                    districtObject.transform.SetParent(cityObject.transform, false);
                    districtObject.transform.position = ToWorld(district.bounds.center, 0f);
                }
            }
        }

        public void GenerateBuildings()
        {
            EnsurePlan();
            EnsureRoot();
            ClearChildren("Buildings");
            var buildingRoot = CreateStageRoot("Buildings");
            var buildingSystem = new BuildingPlacementSystem(prefabDatabase, assetCatalog);

            foreach (var city in currentPlan.cities)
            {
                var cityObject = new GameObject(city.name);
                cityObject.transform.SetParent(buildingRoot, false);
                cityObject.transform.position = ToWorld(city.position, 0f);

                foreach (var district in city.districts)
                {
                    var districtObject = new GameObject(district.name);
                    districtObject.transform.SetParent(cityObject.transform, false);
                    districtObject.transform.position = ToWorld(district.bounds.center, 0f);

                    if (district.lots == null)
                    {
                        continue;
                    }

                    foreach (var lot in district.lots)
                    {
                        var worldPosition = ToWorld(lot.center, 0f);
                        buildingSystem.PlaceBuilding(districtObject.transform, district, lot, worldPosition);
                    }
                }
            }
        }

        public void GenerateTransport()
        {
            EnsurePlan();
            EnsureRoot();
            ClearChildren("Transport");
            var transportRoot = CreateStageRoot("Transport");

            var roadNetworkPath = GetRoadNetworkPath();
            WorldSaveUtility.SaveRoadNetwork(currentPlan.roadNetwork, roadNetworkPath);
            Debug.Log($"Road network saved to {roadNetworkPath}");

            foreach (var link in currentPlan.transportLinks)
            {
                var road = GameObject.CreatePrimitive(PrimitiveType.Cube);
                road.name = link.name;
                road.transform.SetParent(transportRoot, false);
                var from = ToWorld(link.from, 0.25f);
                var to = ToWorld(link.to, 0.25f);
                road.transform.position = (from + to) * 0.5f;
                road.transform.LookAt(to);
                road.transform.localScale = new Vector3(GetTransportWidth(link.type), 0.5f, Vector3.Distance(from, to));
            }

            foreach (var intersection in currentPlan.roadNetwork.Intersections)
            {
                var marker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                marker.name = intersection.name;
                marker.transform.SetParent(transportRoot, false);
                marker.transform.position = ToWorld(intersection.position, 0.35f);
                marker.transform.localScale = Vector3.one * 12f;
            }

            foreach (var roundabout in currentPlan.roadNetwork.Roundabouts)
            {
                var marker = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                marker.name = roundabout.name;
                marker.transform.SetParent(transportRoot, false);
                marker.transform.position = ToWorld(roundabout.center, 0.12f);
                marker.transform.localScale = new Vector3(roundabout.radiusMeters * 0.10f, 0.2f, roundabout.radiusMeters * 0.10f);
            }
        }

        public void GenerateInfrastructure()
        {
            EnsurePlan();
            EnsureRoot();
            ClearChildren("Infrastructure");
            var infrastructureRoot = CreateStageRoot("Infrastructure");

            foreach (var infrastructure in currentPlan.infrastructure)
            {
                GameObject marker;
                var height = GetInfrastructureHeight(infrastructure.type);
                var footprint = Mathf.Clamp(infrastructure.serviceRadiusMeters * 0.08f, 18f, 160f);

                if (infrastructure.type == InfrastructureType.StreetLight)
                {
                    marker = new GameObject(infrastructure.name);
                    marker.transform.SetParent(infrastructureRoot, false);
                    marker.transform.position = ToWorld(infrastructure.position, 0f);

                    var pole = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                    pole.transform.SetParent(marker.transform, false);
                    pole.transform.localScale = new Vector3(0.2f, 2.5f, 0.2f);
                    pole.transform.localPosition = new Vector3(0f, 2.5f, 0f);
                    pole.name = infrastructure.name + " Pole";

                    var light = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    light.transform.SetParent(marker.transform, false);
                    light.transform.localScale = Vector3.one * 0.6f;
                    light.transform.localPosition = new Vector3(0.5f, 4.8f, 0f);
                    light.name = infrastructure.name + " Lamp";
                    continue;
                }

                if (infrastructure.type == InfrastructureType.TrafficSignal)
                {
                    marker = new GameObject(infrastructure.name);
                    marker.transform.SetParent(infrastructureRoot, false);
                    marker.transform.position = ToWorld(infrastructure.position, 0f);

                    var post = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                    post.transform.SetParent(marker.transform, false);
                    post.transform.localScale = new Vector3(0.15f, 1.4f, 0.15f);
                    post.transform.localPosition = new Vector3(0f, 1.4f, 0f);
                    post.name = infrastructure.name + " Post";

                    var signal = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    signal.transform.SetParent(marker.transform, false);
                    signal.transform.localScale = new Vector3(0.5f, 0.7f, 0.25f);
                    signal.transform.localPosition = new Vector3(0f, 2.2f, 0f);
                    signal.name = infrastructure.name + " Signal";
                    continue;
                }

                marker = GameObject.CreatePrimitive(GetInfrastructurePrimitive(infrastructure.type));
                marker.name = infrastructure.name;
                marker.transform.SetParent(infrastructureRoot, false);
                marker.transform.position = ToWorld(infrastructure.position, height * 0.5f);
                marker.transform.localScale = new Vector3(footprint, height, footprint);
            }

            if (currentPlan.utilityLines != null && currentPlan.utilityLines.Count > 0)
            {
                var utilityRoot = new GameObject("Utility Networks");
                utilityRoot.transform.SetParent(infrastructureRoot, false);
                foreach (var line in currentPlan.utilityLines)
                {
                    var segment = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    segment.name = line.name;
                    segment.transform.SetParent(utilityRoot.transform, false);
                    var from = ToWorld(line.from, 0.05f);
                    var to = ToWorld(line.to, 0.05f);
                    segment.transform.position = (from + to) * 0.5f;
                    segment.transform.LookAt(to);
                    segment.transform.localScale = new Vector3(0.18f, 0.08f, Vector3.Distance(from, to));
                    ApplyColor(segment, GetUtilityLineColor(line.type));
                }
            }

            if (currentPlan.siteReservations.Count > 0)
            {
                var reservationRoot = new GameObject("Reserved Planning Sites");
                reservationRoot.transform.SetParent(infrastructureRoot, false);
                foreach (var reservation in currentPlan.siteReservations)
                {
                    var marker = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                    marker.name = $"{reservation.purpose} Reserved: {reservation.ownerName} Score {reservation.score:0.00}";
                    marker.transform.SetParent(reservationRoot.transform, false);
                    marker.transform.position = ToWorld(reservation.position, 0.1f);
                    var radius = Mathf.Clamp(reservation.radiusMeters * 0.08f, 8f, 140f);
                    marker.transform.localScale = new Vector3(radius, 0.2f, radius);
                }
            }

            if (currentPlan.planningRecommendations.Count > 0)
            {
                var recommendationRoot = new GameObject("AI Planning Recommendations");
                recommendationRoot.transform.SetParent(infrastructureRoot, false);
                foreach (var recommendation in currentPlan.planningRecommendations)
                {
                    var marker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    marker.name = $"{recommendation.name} Score {recommendation.score:0.00}";
                    marker.transform.SetParent(recommendationRoot.transform, false);
                    marker.transform.position = ToWorld(recommendation.position, 24f);
                    marker.transform.localScale = Vector3.one * Mathf.Lerp(18f, 72f, recommendation.score);
                }
            }

            foreach (var landmark in currentPlan.landmarks)
            {
                var marker = GameObject.CreatePrimitive(PrimitiveType.Cube);
                marker.name = landmark.name;
                marker.transform.SetParent(infrastructureRoot, false);
                marker.transform.position = ToWorld(landmark.position, landmark.heightMeters * 0.5f);
                marker.transform.localScale = new Vector3(landmark.footprintMeters.x, landmark.heightMeters, landmark.footprintMeters.y);
            }
        }

        public void ConfigureSimulation()
        {
            EnsurePlan();
            EnsureRuntimeSystems();
            economySimulator.Configure(currentPlan, settings);
            trafficSimulator.Configure(currentPlan, settings);
            streamingController.Configure(settings, currentPlan);
        }

        public void OptimizeWorld()
        {
            EnsureRoot();
            foreach (Transform child in generatedRoot)
            {
                child.gameObject.isStatic = true;
            }
        }

        public void GenerateAll()
        {
            GenerateMasterPlan();
            GenerateTerrain();
            GenerateCities();
            GenerateBuildings();
            GenerateTransport();
            GenerateParks();
            GenerateInfrastructure();
            GenerateTrafficSystem();
            ConfigureSimulation();
            OptimizeWorld();
        }

        public void GenerateParks()
        {
            EnsurePlan();
            EnsureRoot();
            ClearChildren("Parks");
            var parksRoot = CreateStageRoot("Parks");

            foreach (var city in currentPlan.cities)
            {
                foreach (var district in city.districts)
                {
                    if (district.type != DistrictType.PublicPark && district.type != DistrictType.Park)
                    {
                        continue;
                    }

                    var districtObject = new GameObject(district.name);
                    districtObject.transform.SetParent(parksRoot, false);
                    districtObject.transform.position = ToWorld(district.bounds.center, 0f);

                    foreach (var pond in district.ponds)
                    {
                        var pondObject = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                        pondObject.name = pond.name;
                        pondObject.transform.SetParent(districtObject.transform, false);
                        pondObject.transform.position = ToWorld(pond.center, 0.05f);
                        pondObject.transform.localScale = new Vector3(pond.radiusMeters * 0.8f, 0.1f, pond.radiusMeters * 0.8f);
                    }

                    foreach (var tree in district.trees)
                    {
                        var treeObject = new GameObject(tree.name);
                        treeObject.transform.SetParent(districtObject.transform, false);
                        treeObject.transform.position = ToWorld(tree.position, 0f);

                        var trunk = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                        trunk.transform.SetParent(treeObject.transform, false);
                        trunk.transform.localScale = new Vector3(0.35f, Mathf.Max(0.5f, tree.heightMeters * 0.35f), 0.35f);
                        trunk.transform.localPosition = new Vector3(0f, trunk.transform.localScale.y, 0f);
                        trunk.name = tree.name + " Trunk";

                        var foliage = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                        foliage.transform.SetParent(treeObject.transform, false);
                        foliage.transform.localScale = Vector3.one * Mathf.Max(0.5f, tree.heightMeters * 0.75f);
                        foliage.transform.localPosition = new Vector3(0f, tree.heightMeters * 0.4f, 0f);
                        foliage.name = tree.name + " Foliage";
                    }

                    foreach (var path in district.paths)
                    {
                        for (var i = 0; i < path.pathPoints.Count - 1; i++)
                        {
                            var start = ToWorld(path.pathPoints[i], 0.04f);
                            var end = ToWorld(path.pathPoints[i + 1], 0.04f);
                            var segment = GameObject.CreatePrimitive(PrimitiveType.Cube);
                            segment.name = path.name + $" Segment {i + 1}";
                            segment.transform.SetParent(districtObject.transform, false);
                            segment.transform.position = (start + end) * 0.5f;
                            segment.transform.LookAt(end);
                            segment.transform.localScale = new Vector3(path.widthMeters * 0.45f, 0.08f, Vector3.Distance(start, end));
                        }
                    }
                }
            }
        }

        private void GenerateDistrict(Transform cityRoot, DistrictPlan district)
        {
            var districtObject = new GameObject(district.name);
            districtObject.transform.SetParent(cityRoot, false);
            districtObject.transform.position = ToWorld(district.bounds.center, 0f);

            if (district.lots != null && district.lots.Count > 0)
            {
                foreach (var lot in district.lots)
                {
                    GenerateLot(districtObject.transform, district, lot);
                }
                return;
            }

            var lotsPerAxis = Mathf.Clamp(Mathf.RoundToInt(Mathf.Lerp(2f, 9f, district.density)), 1, 16);
            var lotSize = new Vector2(district.bounds.width / lotsPerAxis, district.bounds.height / lotsPerAxis);
            for (var x = 0; x < lotsPerAxis; x++)
            {
                for (var y = 0; y < lotsPerAxis; y++)
                {
                    if ((district.type == DistrictType.PublicPark || district.type == DistrictType.Park) && (x + y) % 3 != 0)
                    {
                        continue;
                    }

                    var localPosition = new Vector2(
                        district.bounds.xMin + lotSize.x * (x + 0.5f),
                        district.bounds.yMin + lotSize.y * (y + 0.5f));
                    PlaceLot(districtObject.transform, district, null, localPosition, lotSize);
                }
            }
        }

        private void GenerateLot(Transform districtRoot, DistrictPlan district, LotPlan lot)
        {
            var lotSize = new Vector2(lot.widthMeters, lot.lengthMeters);
            PlaceLot(districtRoot, district, lot, lot.center, lotSize);
        }

        private void PlaceLot(Transform districtRoot, DistrictPlan district, LotPlan lot, Vector2 position, Vector2 lotSize)
        {
            var asset = FindBestAsset(district.type, lotSize);
            GameObject instance;
            if (asset?.prefab != null)
            {
                instance = Instantiate(asset.prefab, districtRoot);
                instance.name = asset.id;
                if (lot != null)
                {
                    lot.matchedPrefabId = asset.id;
                    lot.matchedPrefabCategory = asset.category;
                    lot.matchedFootprintMeters = asset.footprintMeters;
                    lot.matchedHeightMeters = asset.heightMeters;
                    lot.matchedPrefabPlainText = asset.plainText;
                }
            }
            else
            {
                var isPark = district.type == DistrictType.PublicPark || district.type == DistrictType.Park;
                instance = GameObject.CreatePrimitive(isPark ? PrimitiveType.Sphere : PrimitiveType.Cube);
                instance.transform.SetParent(districtRoot, false);
                instance.name = lot != null ? lot.name : $"{district.type} Lot";
                var height = GetDistrictHeight(district);
                instance.transform.localScale = new Vector3(lotSize.x * 0.65f, height, lotSize.y * 0.65f);
                if (lot != null)
                {
                    lot.matchedPrefabId = "None";
                    lot.matchedPrefabCategory = PrefabCategory.Generic;
                    lot.matchedFootprintMeters = lotSize;
                    lot.matchedHeightMeters = height;
                    lot.matchedPrefabPlainText = $"Placeholder | {district.type} | {lotSize.x:0.##}m x {lotSize.y:0.##}m x {height:0.##}m";
                }
            }

            instance.transform.position = ToWorld(position, instance.transform.localScale.y * 0.5f);
        }

        private PrefabEntry FindBestAsset(DistrictType districtType, Vector2 lotSize)
        {
            if (prefabDatabase != null)
            {
                return prefabDatabase.FindBestMatch(districtType, lotSize);
            }

            var asset = assetCatalog != null ? assetCatalog.FindBestFit(districtType, lotSize) : null;
            if (asset == null)
            {
                return null;
            }

            return new PrefabEntry
            {
                id = asset.id,
                prefab = asset.prefab,
                footprintMeters = asset.footprintMeters,
                heightMeters = asset.heightMeters,
                category = PrefabCategory.Generic,
                priority = asset.priority,
                allowedDistricts = asset.allowedDistricts,
                plainText = $"{asset.id} | Generic | {asset.footprintMeters.x:0.##}m x {asset.footprintMeters.y:0.##}m x {asset.heightMeters:0.##}m"
            };
        }

        private float GetDistrictHeight(DistrictPlan district)
        {
            switch (district.type)
            {
                case DistrictType.Business:
                    return Mathf.Lerp(18f, 180f, district.development);
                case DistrictType.Airport:
                case DistrictType.Port:
                case DistrictType.FreightTerminal:
                case DistrictType.Utility:
                    return Mathf.Lerp(6f, 28f, district.development);
                case DistrictType.PublicPark:
                case DistrictType.Park:
                    return Mathf.Lerp(3f, 18f, district.development);
                default:
                    return Mathf.Lerp(4f, 42f, district.development);
            }
        }

        private PrimitiveType GetInfrastructurePrimitive(InfrastructureType type)
        {
            switch (type)
            {
                case InfrastructureType.Airport:
                case InfrastructureType.Port:
                case InfrastructureType.FreightTerminal:
                case InfrastructureType.SewageTreatment:
                case InfrastructureType.Substation:
                    return PrimitiveType.Cube;
                case InfrastructureType.PowerPlant:
                case InfrastructureType.WaterTreatment:
                    return PrimitiveType.Cylinder;
                default:
                    return PrimitiveType.Sphere;
            }
        }

        private float GetInfrastructureHeight(InfrastructureType type)
        {
            switch (type)
            {
                case InfrastructureType.PowerPlant:
                    return 42f;
                case InfrastructureType.WaterTreatment:
                    return 16f;
                case InfrastructureType.SewageTreatment:
                    return 18f;
                case InfrastructureType.Substation:
                    return 12f;
                case InfrastructureType.Airport:
                    return 12f;
                case InfrastructureType.Port:
                case InfrastructureType.FreightTerminal:
                    return 16f;
                default:
                    return 24f;
            }
        }

        private Color GetUtilityLineColor(UtilityLineType type)
        {
            switch (type)
            {
                case UtilityLineType.Power:
                    return Color.yellow;
                case UtilityLineType.Water:
                    return Color.cyan;
                case UtilityLineType.Sewage:
                    return new Color(0.4f, 0.7f, 0.25f, 1f);
                default:
                    return Color.white;
            }
        }

        private Color GetInfrastructureColor(InfrastructureType type)
        {
            switch (type)
            {
                case InfrastructureType.PowerPlant:
                    return new Color(0.9f, 0.5f, 0.1f, 1f);
                case InfrastructureType.WaterTreatment:
                    return new Color(0.2f, 0.45f, 0.85f, 1f);
                case InfrastructureType.SewageTreatment:
                    return new Color(0.35f, 0.75f, 0.35f, 1f);
                case InfrastructureType.Substation:
                    return new Color(0.85f, 0.85f, 0.35f, 1f);
                case InfrastructureType.Airport:
                    return new Color(0.4f, 0.75f, 0.9f, 1f);
                case InfrastructureType.Port:
                    return new Color(0.2f, 0.85f, 0.9f, 1f);
                case InfrastructureType.FreightTerminal:
                    return new Color(0.85f, 0.4f, 0.15f, 1f);
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

        private float GetTransportWidth(TransportType type)
        {
            switch (type)
            {
                case TransportType.Highway:
                    return 18f;
                case TransportType.MainStreet:
                    return 12f;
                case TransportType.SecondaryStreet:
                    return 7f;
                case TransportType.Rail:
                case TransportType.Metro:
                case TransportType.Tram:
                    return 7f;
                default:
                    return 10f;
            }
        }

        private Vector3 ToWorld(Vector2 planPosition, float y)
        {
            return new Vector3(planPosition.x, y, planPosition.y);
        }

        private void EnsurePlan()
        {
            if (currentPlan == null)
            {
                GenerateMasterPlan();
            }
        }

        private void EnsureRoot()
        {
            if (generatedRoot != null)
            {
                return;
            }

            var root = GameObject.Find("ZZ CityGen Generated World") ?? new GameObject("ZZ CityGen Generated World");
            generatedRoot = root.transform;
        }

        private void EnsureRuntimeSystems()
        {
            streamingController = GetComponent<ChunkStreamingController>() ?? gameObject.AddComponent<ChunkStreamingController>();
            economySimulator = GetComponent<EconomySimulator>() ?? gameObject.AddComponent<EconomySimulator>();
            trafficSimulator = GetComponent<TrafficSimulator>() ?? gameObject.AddComponent<TrafficSimulator>();
            pluginRegistry = GetComponent<PluginRegistry>() ?? gameObject.AddComponent<PluginRegistry>();
        }

        private Transform CreateStageRoot(string stageName)
        {
            var stage = new GameObject(stageName);
            stage.transform.SetParent(generatedRoot, false);
            return stage.transform;
        }

        private void ClearChildren(string stageName)
        {
            EnsureRoot();
            var stage = generatedRoot.Find(stageName);
            if (stage == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(stage.gameObject);
            }
            else
            {
                DestroyImmediate(stage.gameObject);
            }
        }
    }
}
