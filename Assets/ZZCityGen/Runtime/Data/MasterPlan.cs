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
        Utility
    }

    public enum TransportType
    {
        Highway,
        SecondaryRoad,
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
        TransitHub
    }

    [Serializable]
    public sealed class MasterPlan
    {
        public int seed;
        public string worldName;
        public Vector2 worldSizeMeters;
        public List<RegionPlan> regions = new List<RegionPlan>();
        public List<CityPlan> cities = new List<CityPlan>();
        public List<NaturalFeaturePlan> naturalFeatures = new List<NaturalFeaturePlan>();
        public List<TransportLinkPlan> transportLinks = new List<TransportLinkPlan>();
        public List<InfrastructurePlan> infrastructure = new List<InfrastructurePlan>();
        public List<LandmarkPlan> landmarks = new List<LandmarkPlan>();
        public List<MapLayerPlan> mapLayers = new List<MapLayerPlan>();
        public EconomyPlan economy = new EconomyPlan();
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
        public float radiusMeters;
        public int populationTarget;
        public float development;
        public List<DistrictPlan> districts = new List<DistrictPlan>();
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
    }

    [Serializable]
    public sealed class NaturalFeaturePlan
    {
        public string name;
        public string featureType;
        public Vector2 start;
        public Vector2 end;
        public float widthOrRadius;
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
