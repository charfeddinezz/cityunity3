using System;
using System.Collections.Generic;
using UnityEngine;

namespace ZZCityGen.Data
{
    public enum DistrictType
    {
        Business,
        LuxuryResidential,
        MiddleResidential,
        PopularResidential,
        Industrial,
        Education,
        Government,
        Tourism,
        PublicPark,
        Port,
        Airport,
        FreightTerminal,
        Utility,
        Downtown,
        Residential,
        Commercial,
        University,
        Luxury,
        Park
    }

    public enum TransportType
    {
        Highway,
        SecondaryRoad,
        MainStreet,
        SecondaryStreet,
        Car,
        Bus,
        Rail,
        Metro,
        Tram,
        Bridge,
        Tunnel
    }

    public enum InfrastructureType
    {
        Airport,
        Port,
        FreightTerminal,
        PowerPlant,
        WaterTreatment,
        SewageTreatment,
        Substation,
        StreetLight,
        TrafficSignal,
        TransitHub
    }

    public enum SitePurpose
    {
        Capital,
        City,
        Village,
        Airport,
        Port,
        Industrial,
        Park,
        Electricity,
        Water,
        Sewage,
        StreetLighting,
        TrafficControl
    }

    [Serializable]
    public sealed class MasterPlan
    {
        public int seed;
        public string worldName;
        public Vector2 worldSizeMeters;
        public List<RegionPlan> regions = new List<RegionPlan>();
        public List<CityPlan> cities = new List<CityPlan>();
        public TerrainPlan terrainPlan = new TerrainPlan();
        public List<NaturalFeaturePlan> naturalFeatures = new List<NaturalFeaturePlan>();
        public List<TerrainAnalysisCellPlan> terrainAnalysis = new List<TerrainAnalysisCellPlan>();
        public List<SiteReservationPlan> siteReservations = new List<SiteReservationPlan>();
        public List<UrbanPlanningRecommendationPlan> planningRecommendations = new List<UrbanPlanningRecommendationPlan>();
        public List<TransportLinkPlan> transportLinks = new List<TransportLinkPlan>();
        public List<TrafficRoutePlan> trafficRoutes = new List<TrafficRoutePlan>();
        public List<PopulationClusterPlan> populationClusters = new List<PopulationClusterPlan>();
        public List<PedestrianRoutePlan> pedestrianRoutes = new List<PedestrianRoutePlan>();
        public List<InfrastructurePlan> infrastructure = new List<InfrastructurePlan>();
        public List<UtilityLinePlan> utilityLines = new List<UtilityLinePlan>();
        public List<LandmarkPlan> landmarks = new List<LandmarkPlan>();
        public List<MapLayerPlan> mapLayers = new List<MapLayerPlan>();
        public List<GrowthPhasePlan> growthPhases = new List<GrowthPhasePlan>();
        public EconomyPlan economy = new EconomyPlan();
        public RoadNetworkPlan roadNetwork = new RoadNetworkPlan();
        public WorldPlan worldPlan = new WorldPlan();
    }

    [Serializable]
    public sealed class RegionPlan
    {
        public string name;
        public Rect bounds;
        public ClimateProfile climate;
        public int populationTarget;
        public float development;
    }

    [Serializable]
    public sealed class CityPlan
    {
        public string name;
        public CityArchetype archetype;
        public Vector2 position;
        public Rect bounds;
        public float radiusMeters;
        public int populationTarget;
        public int populationCurrent;
        public float development;
        public CityEconomyPlan economy = new CityEconomyPlan();
        public List<DistrictPlan> districts = new List<DistrictPlan>();
    }

    [Serializable]
    public sealed class CityEconomyPlan
    {
        public int residentPopulation;
        public int jobsTotal;
        public float averageIncome;
        public float gdpMillions;
        public float employmentRate;
        public float productivityIndex;
    }

    [Serializable]
    public sealed class CityDataPackage
    {
        public List<CityPlan> cities = new List<CityPlan>();
    }

    [Serializable]
    public sealed class DistrictPlan
    {
        public string name;
        public DistrictType type;
        public Rect bounds;
        public int populationTarget;
        public int jobsTarget;
        public float density;
        public float development;
        public float electricityMegawatts;
        public float waterMegalitersPerDay;
        public List<LotPlan> lots = new List<LotPlan>();
        public List<ParkTreePlan> trees = new List<ParkTreePlan>();
        public List<ParkPondPlan> ponds = new List<ParkPondPlan>();
        public List<ParkPathPlan> paths = new List<ParkPathPlan>();
    }

    [Serializable]
    public sealed class LotPlan
    {
        public string name;
        public string districtName;
        public Vector2 center;
        public float widthMeters;
        public float lengthMeters;
        public float areaSquareMeters;
        public DistrictType zoneType;
        public string plainText;

        public string matchedPrefabId;
        public PrefabCategory matchedPrefabCategory;
        public Vector2 matchedFootprintMeters;
        public float matchedHeightMeters;
        public string matchedPrefabPlainText;
    }

    [Serializable]
    public sealed class ParkTreePlan
    {
        public string name;
        public Vector2 position;
        public float heightMeters;
    }

    [Serializable]
    public sealed class ParkPondPlan
    {
        public string name;
        public Vector2 center;
        public float radiusMeters;
    }

    [Serializable]
    public sealed class ParkPathPlan
    {
        public string name;
        public List<Vector2> pathPoints = new List<Vector2>();
        public float widthMeters;
    }

    [Serializable]
    public sealed class NaturalFeaturePlan
    {
        public string name;
        public string featureType;
        public Vector2 start;
        public Vector2 end;
        public float widthOrRadius;
        public float startElevation;
        public float endElevation;
    }

    [Serializable]
    public sealed class TerrainAnalysisCellPlan
    {
        public Rect bounds;
        public Vector2 center;
        public float elevation;
        public float slope;
        public float waterAccess;
        public float resourceRichness;
        public float buildabilityScore;
        public float citySuitabilityScore;
        public float portSuitabilityScore;
        public float airportSuitabilityScore;
    }

    [Serializable]
    public sealed class SiteReservationPlan
    {
        public string ownerName;
        public SitePurpose purpose;
        public Vector2 position;
        public float radiusMeters;
        public float score;
    }

    [Serializable]
    public sealed class UrbanPlanningRecommendationPlan
    {
        public string name;
        public SitePurpose purpose;
        public Vector2 position;
        public float score;
        public string rationale;
    }

    [Serializable]
    public sealed class TransportLinkPlan
    {
        public string name;
        public TransportType type;
        public Vector2 from;
        public Vector2 to;
        public bool requiresBridge;
        public bool requiresTunnel;
    }

    [Serializable]
    public sealed class InfrastructurePlan
    {
        public string name;
        public InfrastructureType type;
        public Vector2 position;
        public float serviceRadiusMeters;
        public int capacity;
        public string ownerCityName;
    }

    [Serializable]
    public sealed class TrafficRoutePlan
    {
        public string name;
        public TransportType type;
        public List<Vector2> pathPoints = new List<Vector2>();
        public int frequencyPerHour;
        public int vehicleCount;
    }

    public enum PopulationClusterRole
    {
        Residence,
        Employment,
        Service,
        Transit
    }

    [Serializable]
    public sealed class PopulationClusterPlan
    {
        public string name;
        public string cityName;
        public string districtName;
        public PopulationClusterRole role;
        public Vector2 center;
        public int residentPopulation;
        public int jobCapacity;
        public float footTrafficIndex;
    }

    [Serializable]
    public sealed class PedestrianRoutePlan
    {
        public string name;
        public string cityName;
        public string originClusterName;
        public string destinationClusterName;
        public List<Vector2> pathPoints = new List<Vector2>();
        public float footTrafficIndex;
    }

    public enum UtilityLineType
    {
        Power,
        Water,
        Sewage
    }

    [Serializable]
    public sealed class UtilityLinePlan
    {
        public string name;
        public UtilityLineType type;
        public Vector2 from;
        public Vector2 to;
        public int capacity;
    }

    [Serializable]
    public sealed class LandmarkPlan
    {
        public string name;
        public DistrictType districtType;
        public Vector2 position;
        public Vector2 footprintMeters;
        public float heightMeters;
        public float uniqueness;
    }

    [Serializable]
    public sealed class MapLayerPlan
    {
        public string name;
        public string layerType;
        public int elementCount;
    }

    [Serializable]
    public sealed class GrowthPhasePlan
    {
        public string cityName;
        public int year;
        public int populationTarget;
        public float radiusMeters;
        public float infrastructureBudgetIndex;
        public List<DistrictType> priorityDistricts = new List<DistrictType>();
    }

    [Serializable]
    public sealed class EconomyPlan
    {
        public int totalPopulation;
        public int estimatedJobs;
        public float electricityMegawatts;
        public float waterMegalitersPerDay;
        public float freightTonsPerDay;
        public int publicServiceJobs;
        public int industrialJobs;
        public int tourismJobs;
    }
}
