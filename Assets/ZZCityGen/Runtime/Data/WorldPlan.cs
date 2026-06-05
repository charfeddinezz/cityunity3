using System;
using System.Collections.Generic;
using UnityEngine;

namespace ZZCityGen.Data
{
    [Serializable]
    public sealed class WorldPlan
    {
        public List<WorldCityPlan> Cities = new List<WorldCityPlan>();
        public List<WorldRoadPlan> Roads = new List<WorldRoadPlan>();
        public List<WorldRiverPlan> Rivers = new List<WorldRiverPlan>();
        public List<WorldMountainPlan> Mountains = new List<WorldMountainPlan>();
        public List<WorldParkPlan> Parks = new List<WorldParkPlan>();
        public List<WorldDistrictPlan> Districts = new List<WorldDistrictPlan>();
        public List<WorldLotPlan> Lots = new List<WorldLotPlan>();
        public TerrainPlan Terrain = new TerrainPlan();
        public RoadNetworkPlan RoadNetwork = new RoadNetworkPlan();

        public static WorldPlan FromMasterPlan(MasterPlan plan)
        {
            var worldPlan = new WorldPlan();
            if (plan == null)
            {
                return worldPlan;
            }

            for (var i = 0; i < plan.cities.Count; i++)
            {
                var city = plan.cities[i];
                worldPlan.Cities.Add(new WorldCityPlan
                {
                    name = city.name,
                    archetype = city.archetype,
                    position = city.position,
                    bounds = city.bounds,
                    radiusMeters = city.radiusMeters,
                    populationTarget = city.populationTarget,
                    populationCurrent = city.populationCurrent,
                    economy = city.economy,
                    isPrimaryCity = i == 0
                });

                foreach (var district in city.districts)
                {
                    worldPlan.Districts.Add(new WorldDistrictPlan
                    {
                        name = district.name,
                        cityName = city.name,
                        type = district.type,
                        center = district.bounds.center,
                        sizeMeters = new Vector2(district.bounds.width, district.bounds.height)
                    });

                    if (district.type == DistrictType.PublicPark)
                    {
                        worldPlan.Parks.Add(new WorldParkPlan
                        {
                            name = district.name,
                            center = district.bounds.center,
                            radiusMeters = Mathf.Max(1f, Mathf.Max(district.bounds.width, district.bounds.height) * 0.5f)
                        });
                    }

                    if (district.lots != null)
                    {
                        foreach (var lot in district.lots)
                        {
                            worldPlan.Lots.Add(new WorldLotPlan
                            {
                                name = lot.name,
                                districtName = district.name,
                                center = lot.center,
                                widthMeters = lot.widthMeters,
                                lengthMeters = lot.lengthMeters,
                                areaSquareMeters = lot.areaSquareMeters,
                                zoneType = lot.zoneType,
                                plainText = lot.plainText
                            });
                        }
                    }
                }
            }

            foreach (var link in plan.transportLinks)
            {
                worldPlan.Roads.Add(new WorldRoadPlan
                {
                    name = link.name,
                    type = link.type,
                    from = link.from,
                    to = link.to
                });
            }

            foreach (var feature in plan.naturalFeatures)
            {
                if (IsRiverFeature(feature.featureType))
                {
                    worldPlan.Rivers.Add(new WorldRiverPlan
                    {
                        name = feature.name,
                        start = feature.start,
                        end = feature.end,
                        widthMeters = feature.widthOrRadius
                    });
                }

                if (IsMountainFeature(feature.featureType))
                {
                    worldPlan.Mountains.Add(new WorldMountainPlan
                    {
                        name = feature.name,
                        position = feature.start,
                        radiusMeters = feature.widthOrRadius,
                        elevation = (feature.startElevation + feature.endElevation) * 0.5f
                    });
                }
            }

            worldPlan.Terrain = plan.terrainPlan;
            worldPlan.RoadNetwork = plan.roadNetwork;
            return worldPlan;
        }

        private static bool IsRiverFeature(string featureType)
        {
            return !string.IsNullOrEmpty(featureType) && featureType.IndexOf("river", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static bool IsMountainFeature(string featureType)
        {
            return !string.IsNullOrEmpty(featureType) && featureType.IndexOf("mountain", StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }

    [Serializable]
    public sealed class WorldCityPlan
    {
        public string name;
        public CityArchetype archetype;
        public Vector2 position;
        public Rect bounds;
        public float radiusMeters;
        public int populationTarget;
        public int populationCurrent;
        public bool isPrimaryCity;
        public CityEconomyPlan economy = new CityEconomyPlan();
    }

    [Serializable]
    public sealed class WorldRoadPlan
    {
        public string name;
        public TransportType type;
        public Vector2 from;
        public Vector2 to;
    }

    [Serializable]
    public sealed class WorldRiverPlan
    {
        public string name;
        public Vector2 start;
        public Vector2 end;
        public float widthMeters;
    }

    [Serializable]
    public sealed class WorldMountainPlan
    {
        public string name;
        public Vector2 position;
        public float radiusMeters;
        public float elevation;
    }

    [Serializable]
    public sealed class WorldParkPlan
    {
        public string name;
        public Vector2 center;
        public float radiusMeters;
    }

    [Serializable]
    public sealed class WorldDistrictPlan
    {
        public string name;
        public string cityName;
        public DistrictType type;
        public Vector2 center;
        public Vector2 sizeMeters;
    }

    [Serializable]
    public sealed class WorldLotPlan
    {
        public string name;
        public string districtName;
        public Vector2 center;
        public float widthMeters;
        public float lengthMeters;
        public float areaSquareMeters;
        public DistrictType zoneType;
        public string plainText;
    }
}
