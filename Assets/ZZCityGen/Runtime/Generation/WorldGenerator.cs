using UnityEngine;
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
        [SerializeField] private Transform generatedRoot;

        private MasterPlan currentPlan;
        private ChunkStreamingController streamingController;
        private EconomySimulator economySimulator;
        private TrafficSimulator trafficSimulator;
        private PluginRegistry pluginRegistry;

        public WorldGenerationSettings Settings => settings;
        public MasterPlan CurrentPlan => currentPlan;

        public void GenerateMasterPlan()
        {
            currentPlan = new MasterPlanBuilder(settings).Build();
            EnsureRuntimeSystems();
            pluginRegistry.ApplyMasterPlanExtensions(currentPlan, settings);
        }

        public void GenerateTerrain()
        {
            EnsurePlan();
            EnsureRoot();
            ClearChildren("Terrain");
            var terrainRoot = CreateStageRoot("Terrain");

            foreach (var feature in currentPlan.naturalFeatures)
            {
                var marker = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                marker.name = feature.name;
                marker.transform.SetParent(terrainRoot, false);
                marker.transform.position = ToWorld(feature.start, 0f);
                var radius = Mathf.Max(2f, feature.widthOrRadius * 0.5f);
                marker.transform.localScale = new Vector3(radius, 2f, radius);
            }
        }

        public void GenerateCities()
        {
            EnsurePlan();
            EnsureRoot();
            ClearChildren("Cities");
            var cityRoot = CreateStageRoot("Cities");

            foreach (var city in currentPlan.cities)
            {
                var cityObject = new GameObject(city.name);
                cityObject.transform.SetParent(cityRoot, false);
                cityObject.transform.position = ToWorld(city.position, 0f);

                foreach (var district in city.districts)
                {
                    GenerateDistrict(cityObject.transform, district);
                }
            }
        }

        public void GenerateTransport()
        {
            EnsurePlan();
            EnsureRoot();
            ClearChildren("Transport");
            var transportRoot = CreateStageRoot("Transport");

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
        }

        public void GenerateInfrastructure()
        {
            EnsurePlan();
            EnsureRoot();
            ClearChildren("Infrastructure");
            var infrastructureRoot = CreateStageRoot("Infrastructure");

            foreach (var infrastructure in currentPlan.infrastructure)
            {
                var marker = GameObject.CreatePrimitive(GetInfrastructurePrimitive(infrastructure.type));
                marker.name = infrastructure.name;
                marker.transform.SetParent(infrastructureRoot, false);
                var footprint = Mathf.Clamp(infrastructure.serviceRadiusMeters * 0.08f, 18f, 160f);
                var height = GetInfrastructureHeight(infrastructure.type);
                marker.transform.position = ToWorld(infrastructure.position, height * 0.5f);
                marker.transform.localScale = new Vector3(footprint, height, footprint);
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
            GenerateTransport();
            GenerateInfrastructure();
            ConfigureSimulation();
            OptimizeWorld();
        }

        private void GenerateDistrict(Transform cityRoot, DistrictPlan district)
        {
            var districtObject = new GameObject(district.name);
            districtObject.transform.SetParent(cityRoot, false);
            districtObject.transform.position = ToWorld(district.bounds.center, 0f);

            var lotsPerAxis = Mathf.Clamp(Mathf.RoundToInt(Mathf.Lerp(2f, 9f, district.density)), 1, 16);
            var lotSize = new Vector2(district.bounds.width / lotsPerAxis, district.bounds.height / lotsPerAxis);
            for (var x = 0; x < lotsPerAxis; x++)
            {
                for (var y = 0; y < lotsPerAxis; y++)
                {
                    if (district.type == DistrictType.PublicPark && (x + y) % 3 != 0)
                    {
                        continue;
                    }

                    var localPosition = new Vector2(
                        district.bounds.xMin + lotSize.x * (x + 0.5f),
                        district.bounds.yMin + lotSize.y * (y + 0.5f));
                    PlaceLot(districtObject.transform, district, localPosition, lotSize);
                }
            }
        }

        private void PlaceLot(Transform districtRoot, DistrictPlan district, Vector2 position, Vector2 lotSize)
        {
            var asset = assetCatalog != null ? assetCatalog.FindBestFit(district.type, lotSize) : null;
            GameObject instance;
            if (asset?.prefab != null)
            {
                instance = Instantiate(asset.prefab, districtRoot);
                instance.name = asset.id;
            }
            else
            {
                instance = GameObject.CreatePrimitive(district.type == DistrictType.PublicPark ? PrimitiveType.Sphere : PrimitiveType.Cube);
                instance.transform.SetParent(districtRoot, false);
                instance.name = $"{district.type} Lot";
                var height = GetDistrictHeight(district);
                instance.transform.localScale = new Vector3(lotSize.x * 0.65f, height, lotSize.y * 0.65f);
            }

            instance.transform.position = ToWorld(position, instance.transform.localScale.y * 0.5f);
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
                case InfrastructureType.Airport:
                    return 12f;
                default:
                    return 24f;
            }
        }

        private float GetTransportWidth(TransportType type)
        {
            switch (type)
            {
                case TransportType.Highway:
                    return 18f;
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
