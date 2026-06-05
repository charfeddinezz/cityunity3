using System.Collections.Generic;
using System.IO;
using UnityEngine;
using ZZCityGen.Data;

namespace ZZCityGen.Core
{
    public sealed class LoadSystem : MonoBehaviour
    {
        public MasterPlan LoadMasterPlan(string filePath)
        {
            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
            {
                Debug.LogWarning($"LoadMasterPlan failed: file not found at {filePath}");
                return null;
            }

            return WorldSaveUtility.LoadMasterPlan(filePath);
        }

        public WorldPlan LoadWorldPlan(string filePath)
        {
            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
            {
                Debug.LogWarning($"LoadWorldPlan failed: file not found at {filePath}");
                return null;
            }

            return WorldSaveUtility.LoadWorldPlan(filePath);
        }

        public MasterPlan ReconstructMasterPlan(WorldPlan worldPlan, WorldGenerationSettings settings)
        {
            if (worldPlan == null)
            {
                return null;
            }

            var plan = new MasterPlan
            {
                seed = settings != null ? settings.worldSeed : 0,
                worldName = worldPlan.Terrain != null ? "Imported World" : "Imported World",
                worldSizeMeters = settings != null ? new Vector2(settings.WorldSizeMeters, settings.WorldSizeMeters) : new Vector2(1000f, 1000f),
                terrainPlan = worldPlan.Terrain,
                roadNetwork = worldPlan.RoadNetwork
            };

            plan.transportLinks = new List<TransportLinkPlan>();
            foreach (var road in worldPlan.Roads)
            {
                plan.transportLinks.Add(new TransportLinkPlan
                {
                    name = road.name,
                    type = road.type,
                    from = road.from,
                    to = road.to
                });
            }

            plan.infrastructure = new List<InfrastructurePlan>();
            foreach (var infrastructure in worldPlan.Infrastructure)
            {
                plan.infrastructure.Add(new InfrastructurePlan
                {
                    name = infrastructure.name,
                    type = infrastructure.type,
                    position = infrastructure.position,
                    serviceRadiusMeters = infrastructure.serviceRadiusMeters,
                    capacity = infrastructure.capacity,
                    ownerCityName = infrastructure.ownerCityName
                });
            }

            plan.utilityLines = new List<UtilityLinePlan>();
            foreach (var utility in worldPlan.UtilityLines)
            {
                plan.utilityLines.Add(new UtilityLinePlan
                {
                    name = utility.name,
                    type = utility.type,
                    from = utility.from,
                    to = utility.to,
                    capacity = utility.capacity
                });
            }

            plan.trafficRoutes = new List<TrafficRoutePlan>();
            foreach (var route in worldPlan.TrafficRoutes)
            {
                plan.trafficRoutes.Add(new TrafficRoutePlan
                {
                    name = route.name,
                    type = route.type,
                    pathPoints = new List<Vector2>(route.pathPoints),
                    frequencyPerHour = route.frequencyPerHour,
                    vehicleCount = route.vehicleCount
                });
            }

            plan.siteReservations = new List<SiteReservationPlan>();
            foreach (var reservation in worldPlan.SiteReservations)
            {
                plan.siteReservations.Add(new SiteReservationPlan
                {
                    ownerName = reservation.ownerName,
                    purpose = reservation.purpose,
                    position = reservation.position,
                    radiusMeters = reservation.radiusMeters,
                    score = reservation.score
                });
            }

            plan.planningRecommendations = new List<UrbanPlanningRecommendationPlan>();
            foreach (var recommendation in worldPlan.PlanningRecommendations)
            {
                plan.planningRecommendations.Add(new UrbanPlanningRecommendationPlan
                {
                    name = recommendation.name,
                    purpose = recommendation.purpose,
                    position = recommendation.position,
                    score = recommendation.score,
                    rationale = recommendation.rationale
                });
            }

            plan.cities = new List<CityPlan>();
            foreach (var worldCity in worldPlan.Cities)
            {
                var city = new CityPlan
                {
                    name = worldCity.name,
                    archetype = worldCity.archetype,
                    position = worldCity.position,
                    bounds = worldCity.bounds,
                    radiusMeters = worldCity.radiusMeters,
                    populationTarget = worldCity.populationTarget,
                    populationCurrent = worldCity.populationCurrent,
                    development = 0.5f,
                    economy = worldCity.economy,
                    districts = new List<DistrictPlan>()
                };

                plan.cities.Add(city);
            }

            foreach (var worldDistrict in worldPlan.Districts)
            {
                var city = plan.cities.Find(c => c.name == worldDistrict.cityName);
                if (city == null)
                {
                    continue;
                }

                city.districts.Add(new DistrictPlan
                {
                    name = worldDistrict.name,
                    type = worldDistrict.type,
                    bounds = new Rect(worldDistrict.center.x - worldDistrict.sizeMeters.x * 0.5f, worldDistrict.center.y - worldDistrict.sizeMeters.y * 0.5f, worldDistrict.sizeMeters.x, worldDistrict.sizeMeters.y),
                    populationTarget = 0,
                    jobsTarget = 0,
                    density = 0.5f,
                    development = 0.5f,
                    electricityMegawatts = 0f,
                    waterMegalitersPerDay = 0f
                });
            }

            return plan;
        }
    }
}
