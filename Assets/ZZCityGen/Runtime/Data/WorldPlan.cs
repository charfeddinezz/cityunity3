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
        public List<WorldParkTreePlan> ParkTrees = new List<WorldParkTreePlan>();
        public List<WorldParkPondPlan> ParkPonds = new List<WorldParkPondPlan>();
        public List<WorldParkPathPlan> ParkPaths = new List<WorldParkPathPlan>();
        public List<WorldInfrastructurePlan> Infrastructure = new List<WorldInfrastructurePlan>();
        public List<WorldUtilityLinePlan> UtilityLines = new List<WorldUtilityLinePlan>();
        public List<WorldTrafficRoutePlan> TrafficRoutes = new List<WorldTrafficRoutePlan>();
        public List<WorldSiteReservationPlan> SiteReservations = new List<WorldSiteReservationPlan>();
        public List<WorldPlanningRecommendationPlan> PlanningRecommendations = new List<WorldPlanningRecommendationPlan>();
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
                                plainText = lot.plainText,
                                matchedPrefabId = lot.matchedPrefabId,
                                matchedPrefabCategory = lot.matchedPrefabCategory,
                                matchedFootprintMeters = lot.matchedFootprintMeters,
                                matchedHeightMeters = lot.matchedHeightMeters,
                                matchedPrefabPlainText = lot.matchedPrefabPlainText
                            });
                        }
                    }

                    if (district.trees != null)
                    {
                        foreach (var tree in district.trees)
                        {
                            worldPlan.ParkTrees.Add(new WorldParkTreePlan
                            {
                                name = tree.name,
                                districtName = district.name,
                                position = tree.position,
                                heightMeters = tree.heightMeters
                            });
                        }
                    }

                    if (district.ponds != null)
                    {
                        foreach (var pond in district.ponds)
                        {
                            worldPlan.ParkPonds.Add(new WorldParkPondPlan
                            {
                                name = pond.name,
                                districtName = district.name,
                                center = pond.center,
                                radiusMeters = pond.radiusMeters
                            });
                        }
                    }

                    if (district.paths != null)
                    {
                        foreach (var path in district.paths)
                        {
                            worldPlan.ParkPaths.Add(new WorldParkPathPlan
                            {
                                name = path.name,
                                districtName = district.name,
                                pathPoints = new List<Vector2>(path.pathPoints),
                                widthMeters = path.widthMeters
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

            if (plan.infrastructure != null)
            {
                foreach (var infrastructure in plan.infrastructure)
                {
                    worldPlan.Infrastructure.Add(new WorldInfrastructurePlan
                    {
                        name = infrastructure.name,
                        type = infrastructure.type,
                        position = infrastructure.position,
                        serviceRadiusMeters = infrastructure.serviceRadiusMeters,
                        capacity = infrastructure.capacity,
                        ownerCityName = infrastructure.ownerCityName
                    });
                }
            }

            if (plan.utilityLines != null)
            {
                foreach (var line in plan.utilityLines)
                {
                    worldPlan.UtilityLines.Add(new WorldUtilityLinePlan
                    {
                        name = line.name,
                        type = line.type,
                        from = line.from,
                        to = line.to,
                        capacity = line.capacity
                    });
                }
            }

            if (plan.trafficRoutes != null)
            {
                foreach (var route in plan.trafficRoutes)
                {
                    worldPlan.TrafficRoutes.Add(new WorldTrafficRoutePlan
                    {
                        name = route.name,
                        type = route.type,
                        pathPoints = new List<Vector2>(route.pathPoints),
                        frequencyPerHour = route.frequencyPerHour,
                        vehicleCount = route.vehicleCount
                    });
                }
            }

            if (plan.siteReservations != null)
            {
                foreach (var reservation in plan.siteReservations)
                {
                    worldPlan.SiteReservations.Add(new WorldSiteReservationPlan
                    {
                        ownerName = reservation.ownerName,
                        purpose = reservation.purpose,
                        position = reservation.position,
                        radiusMeters = reservation.radiusMeters,
                        score = reservation.score
                    });
                }
            }

            if (plan.planningRecommendations != null)
            {
                foreach (var recommendation in plan.planningRecommendations)
                {
                    worldPlan.PlanningRecommendations.Add(new WorldPlanningRecommendationPlan
                    {
                        name = recommendation.name,
                        purpose = recommendation.purpose,
                        position = recommendation.position,
                        score = recommendation.score,
                        rationale = recommendation.rationale
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
    public sealed class WorldInfrastructurePlan
    {
        public string name;
        public InfrastructureType type;
        public Vector2 position;
        public float serviceRadiusMeters;
        public int capacity;
        public string ownerCityName;
    }

    [Serializable]
    public sealed class WorldUtilityLinePlan
    {
        public string name;
        public UtilityLineType type;
        public Vector2 from;
        public Vector2 to;
        public int capacity;
    }

    [Serializable]
    public sealed class WorldTrafficRoutePlan
    {
        public string name;
        public TransportType type;
        public List<Vector2> pathPoints = new List<Vector2>();
        public int frequencyPerHour;
        public int vehicleCount;
    }

    [Serializable]
    public sealed class WorldSiteReservationPlan
    {
        public string ownerName;
        public SitePurpose purpose;
        public Vector2 position;
        public float radiusMeters;
        public float score;
    }

    [Serializable]
    public sealed class WorldPlanningRecommendationPlan
    {
        public string name;
        public SitePurpose purpose;
        public Vector2 position;
        public float score;
        public string rationale;
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

        public string matchedPrefabId;
        public PrefabCategory matchedPrefabCategory;
        public Vector2 matchedFootprintMeters;
        public float matchedHeightMeters;
        public string matchedPrefabPlainText;
    }

    [Serializable]
    public sealed class WorldParkTreePlan
    {
        public string name;
        public string districtName;
        public Vector2 position;
        public float heightMeters;
    }

    [Serializable]
    public sealed class WorldParkPondPlan
    {
        public string name;
        public string districtName;
        public Vector2 center;
        public float radiusMeters;
    }

    [Serializable]
    public sealed class WorldParkPathPlan
    {
        public string name;
        public string districtName;
        public List<Vector2> pathPoints = new List<Vector2>();
        public float widthMeters;
    }
}
