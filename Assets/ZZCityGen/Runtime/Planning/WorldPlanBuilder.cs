using System;
using UnityEngine;
using ZZCityGen.Data;

namespace ZZCityGen.Planning
{
    public sealed class WorldPlanBuilder
    {
        private const float GoldenAngle = 2.39996323f;
        private readonly WorldSettings settings;
        private readonly System.Random random;

        public WorldPlanBuilder(WorldSettings settings)
        {
            this.settings = settings ?? new WorldSettings();
            this.settings.terrainSettings = this.settings.terrainSettings ?? new TerrainSettings();
            this.settings.roadSettings = this.settings.roadSettings ?? new RoadSettings();
            this.settings.buildingSettings = this.settings.buildingSettings ?? new BuildingSettings();
            random = new System.Random(this.settings.seed);
        }

        public WorldPlanBuilder(WorldGenerationSettings settings)
            : this(ToWorldSettings(settings))
        {
        }

        public WorldPlan Build()
        {
            var size = Mathf.Max(1000, settings.worldSize);
            var plan = new WorldPlan
            {
                Seed = settings.seed,
                WorldSize = size
            };

            GenerateCityLocations(plan);
            GenerateRoads(plan);
            GenerateRivers(plan);
            GenerateMountains(plan);
            GenerateParksAndDistricts(plan);
            return plan;
        }

        private static WorldSettings ToWorldSettings(WorldGenerationSettings source)
        {
            source = source ?? new WorldGenerationSettings();
            return new WorldSettings
            {
                worldSize = source.WorldSizeMeters,
                seed = source.worldSeed,
                numberOfCities = source.cityCount,
                terrainSettings = new TerrainSettings
                {
                    mountainAmount = source.mountainAmount,
                    riverAmount = source.waterAmount,
                    parkAmount = source.forestAmount
                },
                roadSettings = new RoadSettings
                {
                    connectAllCities = source.generateHighways,
                    mainRoadWidth = source.generateHighways ? 28f : 18f,
                    secondaryRoadWidth = source.generateRail ? 14f : 10f
                },
                buildingSettings = new BuildingSettings
                {
                    density = source.urbanDensity,
                    districtsPerCity = 6,
                    averageBuildingHeight = Mathf.Lerp(12f, 48f, source.urbanDensity)
                }
            };
        }

        private void GenerateCityLocations(WorldPlan plan)
        {
            var cityCount = Mathf.Clamp(settings.numberOfCities, 1, 128);
            var center = new Vector2(plan.WorldSize * 0.5f, plan.WorldSize * 0.5f);
            var margin = plan.WorldSize * 0.06f;
            var minSpacing = plan.WorldSize / Mathf.Sqrt(cityCount) * 0.42f;

            plan.Cities.Add(new WorldCityPlan
            {
                Name = "Capital City",
                Position = center,
                Radius = Mathf.Max(500f, plan.WorldSize * 0.055f),
                PopulationTarget = Mathf.RoundToInt(350000f * settings.buildingSettings.density)
            });

            for (var i = 1; i < cityCount; i++)
            {
                var normalizedIndex = i / (float)Mathf.Max(1, cityCount - 1);
                var baseRadius = Mathf.Sqrt(normalizedIndex) * plan.WorldSize * 0.43f;
                var angle = i * GoldenAngle + Range(-0.22f, 0.22f);
                var candidate = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * baseRadius;
                candidate = ClampToWorld(candidate, plan.WorldSize, margin);

                for (var attempt = 0; attempt < 12 && IsTooClose(plan, candidate, minSpacing); attempt++)
                {
                    angle += GoldenAngle * 0.5f + Range(-0.12f, 0.12f);
                    baseRadius = Mathf.Lerp(plan.WorldSize * 0.16f, plan.WorldSize * 0.46f, Random01());
                    candidate = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * baseRadius;
                    candidate = ClampToWorld(candidate, plan.WorldSize, margin);
                }

                plan.Cities.Add(new WorldCityPlan
                {
                    Name = $"City {i + 1}",
                    Position = candidate,
                    Radius = Mathf.Lerp(300f, 1800f, settings.buildingSettings.density) * Range(0.75f, 1.25f),
                    PopulationTarget = Mathf.RoundToInt(Mathf.Lerp(25000f, 180000f, settings.buildingSettings.density) * Range(0.65f, 1.35f))
                });
            }
        }

        private void GenerateRoads(WorldPlan plan)
        {
            if (!settings.roadSettings.connectAllCities || plan.Cities.Count < 2)
            {
                return;
            }

            for (var i = 1; i < plan.Cities.Count; i++)
            {
                var nearestIndex = FindNearestPreviousCity(plan, i);
                var from = plan.Cities[nearestIndex];
                var to = plan.Cities[i];
                plan.Roads.Add(new WorldRoadPlan
                {
                    Name = nearestIndex == 0 ? $"Main Road Capital - {to.Name}" : $"Road {from.Name} - {to.Name}",
                    From = from.Position,
                    To = to.Position,
                    Width = nearestIndex == 0 ? settings.roadSettings.mainRoadWidth : settings.roadSettings.secondaryRoadWidth
                });
            }
        }

        private void GenerateRivers(WorldPlan plan)
        {
            var count = Mathf.Max(1, Mathf.RoundToInt(settings.terrainSettings.riverAmount * 4f));
            for (var i = 0; i < count; i++)
            {
                var x = Mathf.Lerp(plan.WorldSize * 0.12f, plan.WorldSize * 0.88f, Random01());
                plan.Rivers.Add(new WorldRiverPlan
                {
                    Name = $"River {i + 1}",
                    Start = new Vector2(x, plan.WorldSize),
                    End = new Vector2(Clamp(x + Range(-0.22f, 0.22f) * plan.WorldSize, 0f, plan.WorldSize), 0f),
                    Width = Mathf.Lerp(24f, 90f, Random01())
                });
            }
        }

        private void GenerateMountains(WorldPlan plan)
        {
            var count = Mathf.Max(1, Mathf.RoundToInt(settings.terrainSettings.mountainAmount * 8f));
            for (var i = 0; i < count; i++)
            {
                var t = (i + 1f) / (count + 1f);
                var position = new Vector2(plan.WorldSize * t, plan.WorldSize * (0.78f + Range(-0.1f, 0.1f)));
                plan.Mountains.Add(new WorldMountainPlan
                {
                    Name = $"Mountain {i + 1}",
                    Position = ClampToWorld(position, plan.WorldSize, plan.WorldSize * 0.04f),
                    Radius = Mathf.Lerp(450f, 2200f, Random01()),
                    Height = Mathf.Lerp(350f, 2600f, Random01())
                });
            }
        }

        private void GenerateParksAndDistricts(WorldPlan plan)
        {
            var districtsPerCity = Mathf.Clamp(settings.buildingSettings.districtsPerCity, 1, 64);
            for (var cityIndex = 0; cityIndex < plan.Cities.Count; cityIndex++)
            {
                var city = plan.Cities[cityIndex];
                var parkRadius = city.Radius * Mathf.Lerp(0.14f, 0.34f, settings.terrainSettings.parkAmount);
                plan.Parks.Add(new WorldParkPlan
                {
                    Name = $"{city.Name} Central Park",
                    Position = city.Position + new Vector2(city.Radius * 0.2f, city.Radius * 0.1f),
                    Radius = parkRadius
                });

                for (var i = 0; i < districtsPerCity; i++)
                {
                    var angle = Mathf.PI * 2f * i / districtsPerCity;
                    var districtCenter = city.Position + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * city.Radius * 0.38f;
                    var side = city.Radius * 0.45f;
                    plan.Districts.Add(new WorldDistrictPlan
                    {
                        Name = $"{city.Name} District {i + 1}",
                        CityIndex = cityIndex,
                        Bounds = new Rect(districtCenter.x - side * 0.5f, districtCenter.y - side * 0.5f, side, side),
                        Density = settings.buildingSettings.density
                    });
                }
            }
        }

        private bool IsTooClose(WorldPlan plan, Vector2 candidate, float minSpacing)
        {
            for (var i = 0; i < plan.Cities.Count; i++)
            {
                if (Vector2.Distance(plan.Cities[i].Position, candidate) < minSpacing)
                {
                    return true;
                }
            }

            return false;
        }

        private int FindNearestPreviousCity(WorldPlan plan, int cityIndex)
        {
            var bestIndex = 0;
            var bestDistance = float.MaxValue;
            var target = plan.Cities[cityIndex].Position;
            for (var i = 0; i < cityIndex; i++)
            {
                var distance = (plan.Cities[i].Position - target).sqrMagnitude;
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    bestIndex = i;
                }
            }

            return bestIndex;
        }

        private Vector2 ClampToWorld(Vector2 value, float size, float margin)
        {
            return new Vector2(Clamp(value.x, margin, size - margin), Clamp(value.y, margin, size - margin));
        }

        private float Range(float min, float max)
        {
            return Mathf.Lerp(min, max, Random01());
        }

        private float Random01()
        {
            return (float)random.NextDouble();
        }

        private static float Clamp(float value, float min, float max)
        {
            return Mathf.Min(Mathf.Max(value, min), max);
        }
    }
}
