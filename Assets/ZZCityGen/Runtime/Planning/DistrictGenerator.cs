using System;
using System.Collections.Generic;
using UnityEngine;
using ZZCityGen.Data;
using ZZCityGen.Utilities;

namespace ZZCityGen.Planning
{
    public sealed class DistrictGenerator
    {
        private readonly WorldGenerationSettings settings;
        private readonly System.Random random;
        private readonly SeededNameGenerator names;

        public DistrictGenerator(WorldGenerationSettings settings, System.Random random, SeededNameGenerator names)
        {
            this.settings = settings ?? new WorldGenerationSettings();
            this.random = random ?? new System.Random(this.settings.worldSeed);
            this.names = names ?? new SeededNameGenerator(this.settings.worldSeed);
        }

        public List<DistrictPlan> BuildDistricts(CityPlan city)
        {
            var districts = new List<DistrictPlan>();
            var recipe = GetDistrictRecipe(city.archetype);
            for (var i = 0; i < recipe.Count; i++)
            {
                var type = recipe[i];
                var sliceAngle = Mathf.PI * 2f * i / recipe.Count;
                var districtCenter = city.position + new Vector2(Mathf.Cos(sliceAngle), Mathf.Sin(sliceAngle)) * city.radiusMeters * 0.32f;
                var districtSize = city.radiusMeters * Range(0.42f, 0.72f);
                var districtPopulation = type == DistrictType.Park || type == DistrictType.Industrial || type == DistrictType.Downtown ? 0 : Mathf.RoundToInt(city.populationTarget / (float)recipe.Count);
                var district = new DistrictPlan
                {
                    name = names.NextDistrictName(type, i),
                    type = type,
                    bounds = new Rect(districtCenter.x - districtSize * 0.5f, districtCenter.y - districtSize * 0.5f, districtSize, districtSize),
                    populationTarget = districtPopulation,
                    jobsTarget = EstimateDistrictJobs(type, districtPopulation, city.development),
                    density = GetDistrictDensity(type),
                    development = city.development,
                    electricityMegawatts = EstimateElectricity(type, districtPopulation, city.development),
                    waterMegalitersPerDay = EstimateWater(type, districtPopulation)
                };

                district.lots = new LotGenerator(settings, random, names).BuildLots(district);
                districts.Add(district);
            }

            return districts;
        }

        private List<DistrictType> GetDistrictRecipe(CityArchetype archetype)
        {
            switch (archetype)
            {
                case CityArchetype.CapitalMegacity:
                    return new List<DistrictType>
                    {
                        DistrictType.Downtown,
                        DistrictType.Commercial,
                        DistrictType.Government,
                        DistrictType.University,
                        DistrictType.Luxury,
                        DistrictType.Park,
                        DistrictType.Residential,
                        DistrictType.Industrial
                    };
                case CityArchetype.IndustrialCity:
                    return new List<DistrictType>
                    {
                        DistrictType.Industrial,
                        DistrictType.Commercial,
                        DistrictType.Residential,
                        DistrictType.Park
                    };
                case CityArchetype.CoastalCity:
                    return new List<DistrictType>
                    {
                        DistrictType.Residential,
                        DistrictType.Commercial,
                        DistrictType.Luxury,
                        DistrictType.Park
                    };
                case CityArchetype.UniversityCity:
                    return new List<DistrictType>
                    {
                        DistrictType.University,
                        DistrictType.Residential,
                        DistrictType.Commercial,
                        DistrictType.Park
                    };
                case CityArchetype.TourismCity:
                    return new List<DistrictType>
                    {
                        DistrictType.Luxury,
                        DistrictType.Commercial,
                        DistrictType.Park,
                        DistrictType.Residential
                    };
                case CityArchetype.RuralTown:
                    return new List<DistrictType>
                    {
                        DistrictType.Residential,
                        DistrictType.Industrial,
                        DistrictType.Park
                    };
                case CityArchetype.FamilySuburb:
                    return new List<DistrictType>
                    {
                        DistrictType.Residential,
                        DistrictType.Commercial,
                        DistrictType.Park
                    };
                case CityArchetype.Village:
                    return new List<DistrictType>
                    {
                        DistrictType.Residential,
                        DistrictType.Park
                    };
                default:
                    return new List<DistrictType>
                    {
                        DistrictType.Residential,
                        DistrictType.Commercial,
                        DistrictType.Park
                    };
            }
        }

        private float GetDistrictDensity(DistrictType type)
        {
            switch (type)
            {
                case DistrictType.Downtown:
                    return Range(0.78f, 1f);
                case DistrictType.Commercial:
                    return Range(0.68f, 0.92f);
                case DistrictType.Industrial:
                    return Range(0.42f, 0.7f);
                case DistrictType.Government:
                    return Range(0.45f, 0.72f);
                case DistrictType.University:
                    return Range(0.5f, 0.75f);
                case DistrictType.Luxury:
                    return Range(0.32f, 0.6f);
                case DistrictType.Residential:
                    return Range(0.52f, 0.84f);
                case DistrictType.Park:
                    return Range(0.0f, 0.26f);
                default:
                    return Mathf.Clamp01(settings.urbanDensity + Range(-0.25f, 0.25f));
            }
        }

        private int EstimateDistrictJobs(DistrictType type, int population, float development)
        {
            switch (type)
            {
                case DistrictType.Downtown:
                case DistrictType.Commercial:
                    return Mathf.RoundToInt(population * Mathf.Lerp(1.6f, 3.0f, development));
                case DistrictType.Industrial:
                    return Mathf.RoundToInt(Mathf.Lerp(1200f, 7600f, development));
                case DistrictType.Government:
                case DistrictType.University:
                    return Mathf.RoundToInt(Mathf.Max(150, population) * Mathf.Lerp(0.4f, 1.1f, development));
                case DistrictType.Luxury:
                    return Mathf.RoundToInt(population * Mathf.Lerp(0.18f, 0.54f, development));
                default:
                    return Mathf.RoundToInt(population * Mathf.Lerp(0.12f, 0.38f, development));
            }
        }

        private float EstimateElectricity(DistrictType type, int population, float development)
        {
            var multiplier = type == DistrictType.Downtown || type == DistrictType.Commercial || type == DistrictType.Industrial ? 0.0048f : 0.0016f;
            return Mathf.Max(0.05f, population * multiplier * Mathf.Lerp(0.75f, 1.8f, development));
        }

        private float EstimateWater(DistrictType type, int population)
        {
            var multiplier = type == DistrictType.Park ? 0.00045f : 0.00022f;
            return Mathf.Max(0.02f, population * multiplier);
        }

        private float Range(float min, float max)
        {
            return Mathf.Lerp(min, max, (float)random.NextDouble());
        }
    }
}
