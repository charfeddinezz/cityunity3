using System;
using ZZCityGen.Data;

namespace ZZCityGen.Utilities
{
    public sealed class SeededNameGenerator
    {
        private static readonly string[] Prefixes = { "Astra", "Nova", "Zara", "Mira", "Qamar", "Sahil", "Rayan", "Atlas", "Nahr", "Cedra" };
        private static readonly string[] Suffixes = { "port", "vale", "grad", "bay", "ridge", "field", "heights", "gate", "haven", "district" };
        private readonly Random random;

        public SeededNameGenerator(int seed)
        {
            random = new Random(seed);
        }

        public string NextWorldName()
        {
            return $"{Pick(Prefixes)} {Pick(new[] { "World", "Republic", "Isles", "Continent", "Union" })}";
        }

        public string NextRegionName(int index)
        {
            return $"{Pick(Prefixes)} {Pick(new[] { "Province", "Governorate", "Territory", "Region" })} {index + 1}";
        }

        public string NextCityName(CityArchetype archetype, int index)
        {
            var suffix = archetype == CityArchetype.CoastalCity ? "bay" : Pick(Suffixes);
            return $"{Pick(Prefixes)}{suffix} {index + 1}";
        }

        public string NextDistrictName(DistrictType type, int index)
        {
            return $"{type} {Pick(Suffixes)} {index + 1}";
        }

        public string NextFeatureName(string featureType, int index)
        {
            return $"{Pick(Prefixes)} {featureType} {index + 1}";
        }

        public string NextInfrastructureName(string infrastructureType, string cityName)
        {
            return $"{cityName} {infrastructureType}";
        }

        public string NextLandmarkName(int index)
        {
            return $"{Pick(Prefixes)} {Pick(new[] { "Tower", "Civic Center", "Grand Park", "Innovation Campus", "Harbor Gate" })} {index + 1}";
        }

        private string Pick(string[] values)
        {
            return values[random.Next(values.Length)];
        }
    }
}
