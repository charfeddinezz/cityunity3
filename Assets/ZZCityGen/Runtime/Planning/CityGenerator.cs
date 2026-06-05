using System;
using System.Collections.Generic;
using UnityEngine;
using ZZCityGen.Data;
using ZZCityGen.Utilities;

namespace ZZCityGen.Planning
{
    public sealed class CityGenerator
    {
        private readonly WorldGenerationSettings settings;
        private readonly System.Random random;
        private readonly SeededNameGenerator names;

        public CityGenerator(WorldGenerationSettings settings, System.Random random, SeededNameGenerator names)
        {
            this.settings = settings ?? new WorldGenerationSettings();
            this.random = random ?? new System.Random(this.settings.worldSeed);
            this.names = names ?? new SeededNameGenerator(this.settings.worldSeed);
        }

        public List<CityPlan> BuildCities(MasterPlan plan)
        {
            var cities = new List<CityPlan>();
            var center = plan.worldSizeMeters * 0.5f;
            var capitalPopulation = Mathf.RoundToInt(settings.targetPopulation * 0.36f);
            var capitalRadius = settings.WorldSizeMeters * 0.08f;
            cities.Add(CreateCity(0, CityArchetype.CapitalMegacity, center, capitalPopulation, capitalRadius));

            var archetypes = new[]
            {
                CityArchetype.FamilySuburb,
                CityArchetype.RuralTown,
                CityArchetype.IndustrialCity,
                CityArchetype.CoastalCity,
                CityArchetype.TourismCity,
                CityArchetype.UniversityCity
            };

            for (var i = 1; i < settings.cityCount; i++)
            {
                var archetype = archetypes[i % archetypes.Length];
                var angle = (Mathf.PI * 2f * i / Mathf.Max(1, settings.cityCount - 1)) + Range(-0.3f, 0.3f);
                var distance = settings.WorldSizeMeters * Range(0.18f, 0.46f);

                if (archetype == CityArchetype.CoastalCity)
                {
                    distance = settings.WorldSizeMeters * Range(0.42f, 0.48f);
                    angle = GetCoastalAngle(center, settings.WorldSizeMeters);
                }

                var position = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * distance;
                var population = Mathf.Max(1000, Mathf.RoundToInt(settings.targetPopulation * Range(0.015f, 0.07f)));
                var radius = Mathf.Lerp(650f, 4500f, settings.urbanDensity) * Range(0.75f, 1.35f);
                position = ClampToWorld(position, plan.worldSizeMeters);
                cities.Add(CreateCity(i, archetype, position, population, radius));
            }

            return cities;
        }

        private CityPlan CreateCity(int index, CityArchetype archetype, Vector2 position, int population, float radius)
        {
            var currentPopulation = Mathf.RoundToInt(population * Range(0.72f, 0.96f));
            var city = new CityPlan
            {
                name = names.NextCityName(archetype, index),
                archetype = archetype,
                position = position,
                bounds = new Rect(position.x - radius, position.y - radius, radius * 2f, radius * 2f),
                radiusMeters = radius,
                populationTarget = population,
                populationCurrent = currentPopulation,
                development = Mathf.Clamp01(settings.economicDevelopment + Range(-0.2f, 0.25f)),
                economy = CreateEconomy(population, currentPopulation, archetype)
            };

            city.districts = new DistrictGenerator(settings, random, names).BuildDistricts(city);
            return city;
        }

        private CityEconomyPlan CreateEconomy(int populationTarget, int populationCurrent, CityArchetype archetype)
        {
            var baseIncome = Mathf.Lerp(18f, 55f, settings.economicDevelopment);
            var jobsTotal = Mathf.RoundToInt(populationCurrent * Mathf.Lerp(0.35f, 0.78f, settings.economicDevelopment));
            var gdpMillions = populationCurrent * baseIncome * Mathf.Lerp(0.00012f, 0.00024f, settings.economicDevelopment);
            var employmentRate = Mathf.Clamp01(jobsTotal / (float)Mathf.Max(1, populationCurrent));
            var productivity = Mathf.Clamp01(0.52f + settings.economicDevelopment * 0.32f + Range(-0.1f, 0.1f));

            switch (archetype)
            {
                case CityArchetype.CapitalMegacity:
                    baseIncome *= 1.45f;
                    gdpMillions *= 1.72f;
                    break;
                case CityArchetype.IndustrialCity:
                    baseIncome *= 0.98f;
                    gdpMillions *= 1.18f;
                    break;
                case CityArchetype.TourismCity:
                    baseIncome *= 0.88f;
                    gdpMillions *= 1.12f;
                    break;
                case CityArchetype.UniversityCity:
                    baseIncome *= 1.05f;
                    gdpMillions *= 1.08f;
                    break;
                case CityArchetype.CoastalCity:
                    baseIncome *= 1.02f;
                    gdpMillions *= 1.11f;
                    break;
                case CityArchetype.RuralTown:
                    baseIncome *= 0.74f;
                    gdpMillions *= 0.82f;
                    break;
                case CityArchetype.FamilySuburb:
                    baseIncome *= 0.92f;
                    gdpMillions *= 0.95f;
                    break;
            }

            return new CityEconomyPlan
            {
                residentPopulation = populationCurrent,
                jobsTotal = jobsTotal,
                averageIncome = Mathf.Round(baseIncome * 100f) / 100f,
                gdpMillions = Mathf.Round(gdpMillions * 100f) / 100f,
                employmentRate = Mathf.Clamp01(employmentRate),
                productivityIndex = productivity
            };
        }

        private float GetCoastalAngle(Vector2 center, float worldSize)
        {
            var edge = random.Next(4);
            switch (edge)
            {
                case 0: return Range(-0.5f, 0.5f);
                case 1: return Range(1.05f, 2.05f);
                case 2: return Range(2.55f, 3.55f);
                default: return Range(3.6f, 4.2f);
            }
        }

        private Vector2 ClampToWorld(Vector2 value, Vector2 worldSize)
        {
            return new Vector2(Mathf.Clamp(value.x, 0f, worldSize.x), Mathf.Clamp(value.y, 0f, worldSize.y));
        }

        private float Range(float min, float max)
        {
            return Mathf.Lerp(min, max, (float)random.NextDouble());
        }

    }
}
