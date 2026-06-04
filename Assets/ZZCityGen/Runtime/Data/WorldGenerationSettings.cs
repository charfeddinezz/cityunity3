using System;
using UnityEngine;

namespace ZZCityGen.Data
{
    public enum WorldShape
    {
        Continent,
        IslandChain,
        Nation
    }

    public enum ClimateProfile
    {
        Temperate,
        Tropical,
        Desert,
        Alpine,
        Mixed
    }

    public enum CityArchetype
    {
        CapitalMegacity,
        FamilySuburb,
        RuralTown,
        IndustrialCity,
        CoastalCity,
        TourismCity,
        UniversityCity
    }

    [Serializable]
    public sealed class WorldGenerationSettings
    {
        [Header("World")]
        public int worldSeed = 12345;
        public WorldShape worldShape = WorldShape.Continent;
        public ClimateProfile climate = ClimateProfile.Mixed;
        [Range(8, 512)] public int worldSizeInChunks = 64;
        [Range(64, 1024)] public int chunkSizeMeters = 256;

        [Header("Settlements")]
        [Range(1, 128)] public int cityCount = 24;
        [Range(0, 512)] public int villageCount = 96;
        [Range(1000, 50000000)] public int targetPopulation = 2500000;
        [Range(0.05f, 1f)] public float urbanDensity = 0.55f;
        [Range(0.05f, 1f)] public float economicDevelopment = 0.65f;

        [Header("Terrain")]
        [Range(0f, 1f)] public float mountainAmount = 0.28f;
        [Range(0f, 1f)] public float forestAmount = 0.35f;
        [Range(0f, 1f)] public float waterAmount = 0.22f;
        [Range(0f, 1f)] public float desertAmount = 0.08f;

        [Header("Transport")]
        public bool generateHighways = true;
        public bool generateRail = true;
        public bool generateMetro = true;
        public bool generateAirports = true;
        public bool generatePorts = true;
        public bool generateBridgesAndTunnels = true;

        [Header("Runtime")]
        public bool enableEconomySimulation = true;
        public bool enableTrafficSimulation = true;
        public bool enableDynamicGrowth = true;
        public int activeChunkRadius = 3;
        public int lodLevels = 4;

        public int WorldSizeMeters => Mathf.Max(1, worldSizeInChunks) * Mathf.Max(1, chunkSizeMeters);
    }
}
