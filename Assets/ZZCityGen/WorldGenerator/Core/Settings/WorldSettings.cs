using UnityEngine;

namespace ZZCityGen.WorldGenerator.Core.Settings
{
    [CreateAssetMenu(menuName = "ZZCityGen/WorldSettings", fileName = "WorldSettings")]
    public sealed class WorldSettings : ScriptableObject
    {
        [Header("World")]
        public int worldSeed = 12345;
        public int worldSizeMeters = 8192;
        public ClimateProfile climate = ClimateProfile.Temperate;

        [Header("Settlements")]
        public int cityCount = 12;
        public float roadDensity = 0.6f;
        public float populationDensity = 0.5f;

        public enum ClimateProfile
        {
            Temperate,
            Tropical,
            Desert,
            Alpine,
            Mixed
        }
    }
}