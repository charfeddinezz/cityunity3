using System;
using UnityEngine;

namespace ZZCityGen.Data
{
    [Serializable]
    public sealed class WorldSettings
    {
        [Header("General Settings")]
        [Min(1000)] public int worldSize = 16384;
        public int seed = 12345;
        [Range(1, 128)] public int numberOfCities = 12;

        [Header("Terrain Settings")]
        public TerrainSettings terrainSettings = new TerrainSettings();

        [Header("Road Settings")]
        public RoadSettings roadSettings = new RoadSettings();

        [Header("Building Settings")]
        public BuildingSettings buildingSettings = new BuildingSettings();
    }

    [Serializable]
    public sealed class TerrainSettings
    {
        [Range(0f, 1f)] public float mountainAmount = 0.25f;
        [Range(0f, 1f)] public float riverAmount = 0.2f;
        [Range(0f, 1f)] public float parkAmount = 0.18f;
    }

    [Serializable]
    public sealed class RoadSettings
    {
        public bool connectAllCities = true;
        [Min(1f)] public float mainRoadWidth = 24f;
        [Min(1f)] public float secondaryRoadWidth = 12f;
    }

    [Serializable]
    public sealed class BuildingSettings
    {
        [Range(0.05f, 1f)] public float density = 0.55f;
        [Range(1, 64)] public int districtsPerCity = 6;
        [Min(1f)] public float averageBuildingHeight = 24f;
    }
}
