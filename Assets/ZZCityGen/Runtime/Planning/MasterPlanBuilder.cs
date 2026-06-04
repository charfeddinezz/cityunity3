using System;
using System.Collections.Generic;
using UnityEngine;
using ZZCityGen.Data;
using ZZCityGen.Utilities;

namespace ZZCityGen.Planning
{
    public sealed class MasterPlanBuilder
    {
        private readonly WorldGenerationSettings settings;
        private readonly System.Random random;
        private readonly SeededNameGenerator names;

        public MasterPlanBuilder(WorldGenerationSettings settings)
        {
            this.settings = settings ?? new WorldGenerationSettings();
            random = new System.Random(this.settings.worldSeed);
            names = new SeededNameGenerator(this.settings.worldSeed);
        }

        public MasterPlan Build()
        {
            var plan = new MasterPlan
            {
                seed = settings.worldSeed,
                worldName = names.NextWorldName(),
                worldSizeMeters = new Vector2(settings.WorldSizeMeters, settings.WorldSizeMeters)
            };

            BuildRegions(plan);
            BuildCities(plan);
            BuildNaturalFeatures(plan);
            BuildTransport(plan);
            BuildEconomy(plan);
            return plan;
        }

        private void BuildRegions(MasterPlan plan)
        {
            const int grid = 3;
            var regionSize = settings.WorldSizeMeters / (float)grid;
            for (var y = 0; y < grid; y++)
            {
                for (var x = 0; x < grid; x++)
                {
                    var index = y * grid + x;
                    plan.regions.Add(new RegionPlan
                    {
                        name = names.NextRegionName(index),
                        bounds = new Rect(x * regionSize, y * regionSize, regionSize, regionSize),
                        climate = settings.climate == ClimateProfile.Mixed ? (ClimateProfile)random.Next(0, 4) : settings.climate,
                        populationTarget = Mathf.RoundToInt(settings.targetPopulation / 9f),
                        development = Mathf.Clamp01(settings.economicDevelopment + Range(-0.18f, 0.18f))
                    });
                }
            }
        }

        private void BuildCities(MasterPlan plan)
        {
            var center = plan.worldSizeMeters * 0.5f;
            var capitalPopulation = Mathf.RoundToInt(settings.targetPopulation * 0.36f);
            plan.cities.Add(CreateCity(0, CityArchetype.CapitalMegacity, center, capitalPopulation, settings.WorldSizeMeters * 0.08f));

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
                var angle = (Mathf.PI * 2f * i / Mathf.Max(1, settings.cityCount - 1)) + Range(-0.3f, 0.3f);
                var distance = settings.WorldSizeMeters * Range(0.18f, 0.46f);
                var position = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * distance;
                position = ClampToWorld(position, plan.worldSizeMeters);
                var archetype = archetypes[i % archetypes.Length];
                var population = Mathf.Max(1000, Mathf.RoundToInt(settings.targetPopulation * Range(0.015f, 0.07f)));
                var radius = Mathf.Lerp(650f, 4500f, settings.urbanDensity) * Range(0.75f, 1.35f);
                plan.cities.Add(CreateCity(i, archetype, position, population, radius));
            }
        }

        private CityPlan CreateCity(int index, CityArchetype archetype, Vector2 position, int population, float radius)
        {
            var city = new CityPlan
            {
                name = names.NextCityName(archetype, index),
                archetype = archetype,
                position = position,
                radiusMeters = radius,
                populationTarget = population,
                development = Mathf.Clamp01(settings.economicDevelopment + Range(-0.2f, 0.25f))
            };

            var districtTypes = GetDistrictRecipe(archetype);
            for (var i = 0; i < districtTypes.Count; i++)
            {
                var type = districtTypes[i];
                var sliceAngle = Mathf.PI * 2f * i / districtTypes.Count;
                var districtCenter = position + new Vector2(Mathf.Cos(sliceAngle), Mathf.Sin(sliceAngle)) * radius * 0.32f;
                var districtSize = radius * Range(0.42f, 0.72f);
                var districtPopulation = type == DistrictType.PublicPark || type == DistrictType.Industrial ? 0 : Mathf.RoundToInt(population / (float)districtTypes.Count);
                city.districts.Add(new DistrictPlan
                {
                    name = names.NextDistrictName(type, i),
                    type = type,
                    bounds = new Rect(districtCenter.x - districtSize * 0.5f, districtCenter.y - districtSize * 0.5f, districtSize, districtSize),
                    populationTarget = districtPopulation,
                    density = Mathf.Clamp01(settings.urbanDensity + Range(-0.25f, 0.25f)),
                    development = city.development
                });
            }

            return city;
        }

        private void BuildNaturalFeatures(MasterPlan plan)
        {
            var featureCount = Mathf.Max(4, settings.worldSizeInChunks / 8);
            for (var i = 0; i < featureCount; i++)
            {
                var river = random.NextDouble() < settings.waterAmount;
                var type = river ? "River" : PickFeatureType();
                var start = RandomWorldPoint(plan.worldSizeMeters);
                var end = river ? DownhillEndpoint(start, plan.worldSizeMeters) : start;
                plan.naturalFeatures.Add(new NaturalFeaturePlan
                {
                    name = names.NextFeatureName(type, i),
                    featureType = type,
                    start = start,
                    end = end,
                    widthOrRadius = river ? Range(18f, 90f) : Range(300f, 2600f)
                });
            }
        }

        private void BuildTransport(MasterPlan plan)
        {
            if (plan.cities.Count == 0)
            {
                return;
            }

            var capital = plan.cities[0];
            for (var i = 1; i < plan.cities.Count; i++)
            {
                var city = plan.cities[i];
                if (settings.generateHighways)
                {
                    AddTransport(plan, TransportType.Highway, capital.position, city.position, $"Highway {capital.name} - {city.name}");
                }

                if (settings.generateRail && i % 2 == 0)
                {
                    AddTransport(plan, TransportType.Rail, capital.position, city.position, $"Rail {capital.name} - {city.name}");
                }
            }

            if (settings.generateMetro)
            {
                foreach (var district in capital.districts)
                {
                    if (district.type == DistrictType.Business || district.type == DistrictType.Government || district.type == DistrictType.Education)
                    {
                        AddTransport(plan, TransportType.Metro, capital.position, district.bounds.center, $"Metro {capital.name} - {district.name}");
                    }
                }
            }
        }

        private void AddTransport(MasterPlan plan, TransportType type, Vector2 from, Vector2 to, string name)
        {
            var crossesWater = settings.generateBridgesAndTunnels && random.NextDouble() < settings.waterAmount;
            var crossesMountain = settings.generateBridgesAndTunnels && random.NextDouble() < settings.mountainAmount * 0.5f;
            plan.transportLinks.Add(new TransportLinkPlan
            {
                name = name,
                type = type,
                from = from,
                to = to,
                requiresBridge = crossesWater,
                requiresTunnel = crossesMountain
            });
        }

        private void BuildEconomy(MasterPlan plan)
        {
            plan.economy.totalPopulation = settings.targetPopulation;
            plan.economy.estimatedJobs = Mathf.RoundToInt(settings.targetPopulation * Mathf.Lerp(0.32f, 0.55f, settings.economicDevelopment));
            plan.economy.electricityMegawatts = settings.targetPopulation * Mathf.Lerp(0.0012f, 0.0024f, settings.economicDevelopment);
            plan.economy.waterMegalitersPerDay = settings.targetPopulation * 0.00022f;
            plan.economy.freightTonsPerDay = plan.economy.estimatedJobs * Mathf.Lerp(0.018f, 0.05f, settings.economicDevelopment);
        }

        private List<DistrictType> GetDistrictRecipe(CityArchetype archetype)
        {
            var recipe = new List<DistrictType> { DistrictType.MiddleResidential, DistrictType.PopularResidential, DistrictType.PublicPark };
            if (archetype == CityArchetype.CapitalMegacity)
            {
                recipe.AddRange(new[] { DistrictType.Business, DistrictType.Government, DistrictType.LuxuryResidential, DistrictType.Education, DistrictType.Tourism });
                if (settings.generateAirports)
                {
                    recipe.Add(DistrictType.Airport);
                }
            }
            else if (archetype == CityArchetype.IndustrialCity)
            {
                recipe.AddRange(new[] { DistrictType.Industrial, DistrictType.Business });
            }
            else if (archetype == CityArchetype.CoastalCity)
            {
                if (settings.generatePorts)
                {
                    recipe.Add(DistrictType.Port);
                }
                recipe.Add(DistrictType.Tourism);
            }
            else if (archetype == CityArchetype.UniversityCity)
            {
                recipe.AddRange(new[] { DistrictType.Education, DistrictType.Business });
            }
            else if (archetype == CityArchetype.TourismCity)
            {
                recipe.AddRange(new[] { DistrictType.Tourism, DistrictType.LuxuryResidential });
                if (settings.generateAirports)
                {
                    recipe.Add(DistrictType.Airport);
                }
            }

            return recipe;
        }

        private string PickFeatureType()
        {
            var roll = (float)random.NextDouble();
            if (roll < settings.mountainAmount) return "MountainRange";
            if (roll < settings.mountainAmount + settings.forestAmount) return "Forest";
            if (roll < settings.mountainAmount + settings.forestAmount + settings.desertAmount) return "Desert";
            return "Lake";
        }

        private Vector2 RandomWorldPoint(Vector2 worldSize)
        {
            return new Vector2(Range(0f, worldSize.x), Range(0f, worldSize.y));
        }

        private Vector2 DownhillEndpoint(Vector2 start, Vector2 worldSize)
        {
            var targetY = Mathf.Max(0f, start.y - Range(worldSize.y * 0.12f, worldSize.y * 0.44f));
            return ClampToWorld(new Vector2(start.x + Range(-worldSize.x * 0.15f, worldSize.x * 0.15f), targetY), worldSize);
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
