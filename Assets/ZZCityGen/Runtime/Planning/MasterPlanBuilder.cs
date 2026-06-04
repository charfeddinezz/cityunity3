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
            BuildVillages(plan);
            BuildNaturalFeatures(plan);
            BuildTransport(plan);
            BuildEconomy(plan);
            BuildInfrastructure(plan);
            BuildLandmarks(plan);
            BuildMapLayers(plan);
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

        private void BuildVillages(MasterPlan plan)
        {
            if (settings.villageCount <= 0)
            {
                return;
            }

            for (var i = 0; i < settings.villageCount; i++)
            {
                var region = plan.regions[random.Next(plan.regions.Count)];
                var position = new Vector2(
                    Range(region.bounds.xMin, region.bounds.xMax),
                    Range(region.bounds.yMin, region.bounds.yMax));

                var nearestCity = FindNearestCity(plan.cities, position);
                if (nearestCity != null)
                {
                    var away = (position - nearestCity.position).normalized;
                    if (away.sqrMagnitude < 0.01f)
                    {
                        away = new Vector2(Range(-1f, 1f), Range(-1f, 1f)).normalized;
                    }

                    position = ClampToWorld(nearestCity.position + away * Range(nearestCity.radiusMeters * 1.7f, nearestCity.radiusMeters * 3.4f), plan.worldSizeMeters);
                }

                var population = Mathf.Max(120, Mathf.RoundToInt(settings.targetPopulation * Range(0.00015f, 0.0014f)));
                var radius = Mathf.Lerp(140f, 620f, settings.urbanDensity) * Range(0.65f, 1.25f);
                plan.cities.Add(CreateCity(settings.cityCount + i, CityArchetype.Village, position, population, radius));
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
                    jobsTarget = EstimateDistrictJobs(type, districtPopulation, city.development),
                    density = Mathf.Clamp01(settings.urbanDensity + Range(-0.25f, 0.25f)),
                    development = city.development,
                    electricityMegawatts = EstimateElectricity(type, districtPopulation, city.development),
                    waterMegalitersPerDay = EstimateWater(type, districtPopulation)
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

            for (var i = 1; i < plan.cities.Count; i++)
            {
                var from = plan.cities[i];
                var to = plan.cities[i == plan.cities.Count - 1 ? 1 : i + 1];
                if (from.archetype == CityArchetype.Village && random.NextDouble() > 0.35f)
                {
                    continue;
                }

                AddTransport(plan, TransportType.SecondaryRoad, from.position, to.position, $"Secondary Road {from.name} - {to.name}");
            }

            foreach (var city in plan.cities)
            {
                if (city.archetype != CityArchetype.CapitalMegacity && city.archetype != CityArchetype.UniversityCity && city.archetype != CityArchetype.TourismCity)
                {
                    continue;
                }

                for (var i = 0; i < city.districts.Count; i++)
                {
                    var next = city.districts[(i + 1) % city.districts.Count];
                    AddTransport(plan, TransportType.Tram, city.districts[i].bounds.center, next.bounds.center, $"Tram {city.name} Loop {i + 1}");
                }
            }
        }

        private void BuildInfrastructure(MasterPlan plan)
        {
            foreach (var city in plan.cities)
            {
                foreach (var district in city.districts)
                {
                    if (district.type == DistrictType.Airport)
                    {
                        AddInfrastructure(plan, InfrastructureType.Airport, city, district.bounds.center, Mathf.RoundToInt(city.populationTarget * 0.9f));
                    }
                    else if (district.type == DistrictType.Port)
                    {
                        AddInfrastructure(plan, InfrastructureType.Port, city, district.bounds.center, Mathf.RoundToInt(plan.economy.freightTonsPerDay));
                    }
                    else if (settings.generateFreightTerminals && district.type == DistrictType.Industrial)
                    {
                        AddInfrastructure(plan, InfrastructureType.FreightTerminal, city, district.bounds.center, Mathf.Max(200, district.jobsTarget));
                    }
                }

                if (city.archetype == CityArchetype.CapitalMegacity || city.archetype == CityArchetype.IndustrialCity)
                {
                    var offset = new Vector2(city.radiusMeters * 0.85f, -city.radiusMeters * 0.65f);
                    AddInfrastructure(plan, InfrastructureType.PowerPlant, city, ClampToWorld(city.position + offset, plan.worldSizeMeters), Mathf.RoundToInt(city.populationTarget * 0.75f));
                    AddInfrastructure(plan, InfrastructureType.WaterTreatment, city, ClampToWorld(city.position - offset, plan.worldSizeMeters), Mathf.RoundToInt(city.populationTarget * 0.65f));
                }
            }
        }

        private void BuildLandmarks(MasterPlan plan)
        {
            var index = 0;
            foreach (var city in plan.cities)
            {
                if (city.archetype == CityArchetype.Village || random.NextDouble() > settings.landmarkFrequency + city.development * 0.35f)
                {
                    continue;
                }

                foreach (var district in city.districts)
                {
                    if (district.type != DistrictType.Business && district.type != DistrictType.Government && district.type != DistrictType.Tourism && district.type != DistrictType.Education && district.type != DistrictType.PublicPark)
                    {
                        continue;
                    }

                    plan.landmarks.Add(new LandmarkPlan
                    {
                        name = names.NextLandmarkName(index),
                        districtType = district.type,
                        position = district.bounds.center,
                        footprintMeters = new Vector2(district.bounds.width * 0.22f, district.bounds.height * 0.22f),
                        heightMeters = district.type == DistrictType.Business ? Mathf.Lerp(90f, 420f, district.development) : Mathf.Lerp(18f, 90f, district.development),
                        uniqueness = Mathf.Clamp01(city.development + Range(0.05f, 0.35f))
                    });
                    index++;
                    break;
                }
            }
        }

        private void BuildMapLayers(MasterPlan plan)
        {
            plan.mapLayers.Add(new MapLayerPlan { name = "World Overview", layerType = "World", elementCount = plan.regions.Count + plan.naturalFeatures.Count });
            plan.mapLayers.Add(new MapLayerPlan { name = "Road & Rail Map", layerType = "Transport", elementCount = plan.transportLinks.Count });
            plan.mapLayers.Add(new MapLayerPlan { name = "Settlements Map", layerType = "Cities", elementCount = plan.cities.Count });
            plan.mapLayers.Add(new MapLayerPlan { name = "Infrastructure Map", layerType = "Infrastructure", elementCount = plan.infrastructure.Count });
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
            plan.economy.publicServiceJobs = Mathf.RoundToInt(plan.economy.estimatedJobs * 0.18f);
            plan.economy.industrialJobs = Mathf.RoundToInt(plan.economy.estimatedJobs * 0.22f);
            plan.economy.tourismJobs = Mathf.RoundToInt(plan.economy.estimatedJobs * 0.08f);
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
            else if (archetype == CityArchetype.Village)
            {
                recipe.Clear();
                recipe.AddRange(new[] { DistrictType.PopularResidential, DistrictType.MiddleResidential, DistrictType.PublicPark });
            }

            return recipe;
        }

        private void AddInfrastructure(MasterPlan plan, InfrastructureType type, CityPlan city, Vector2 position, int capacity)
        {
            plan.infrastructure.Add(new InfrastructurePlan
            {
                name = names.NextInfrastructureName(type.ToString(), city.name),
                type = type,
                position = position,
                serviceRadiusMeters = Mathf.Max(city.radiusMeters * 2f, 1200f),
                capacity = Mathf.Max(1, capacity),
                ownerCityName = city.name
            });
        }

        private CityPlan FindNearestCity(List<CityPlan> cities, Vector2 position)
        {
            CityPlan nearest = null;
            var bestDistance = float.MaxValue;
            foreach (var city in cities)
            {
                var distance = Vector2.SqrMagnitude(city.position - position);
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    nearest = city;
                }
            }

            return nearest;
        }

        private int EstimateDistrictJobs(DistrictType type, int population, float development)
        {
            switch (type)
            {
                case DistrictType.Business:
                    return Mathf.RoundToInt(population * Mathf.Lerp(1.8f, 3.4f, development));
                case DistrictType.Industrial:
                case DistrictType.Port:
                case DistrictType.Airport:
                case DistrictType.FreightTerminal:
                    return Mathf.RoundToInt(Mathf.Lerp(450f, 8500f, development));
                case DistrictType.Education:
                case DistrictType.Government:
                case DistrictType.Tourism:
                    return Mathf.RoundToInt(Mathf.Max(150, population) * Mathf.Lerp(0.35f, 1.2f, development));
                default:
                    return Mathf.RoundToInt(population * Mathf.Lerp(0.12f, 0.38f, development));
            }
        }

        private float EstimateElectricity(DistrictType type, int population, float development)
        {
            var multiplier = type == DistrictType.Business || type == DistrictType.Industrial ? 0.0048f : 0.0016f;
            return Mathf.Max(0.05f, population * multiplier * Mathf.Lerp(0.75f, 1.8f, development));
        }

        private float EstimateWater(DistrictType type, int population)
        {
            var multiplier = type == DistrictType.PublicPark ? 0.00045f : 0.00022f;
            return Mathf.Max(0.02f, population * multiplier);
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
