using System;
using System.Collections.Generic;
using UnityEngine;
using ZZCityGen.Data;

namespace ZZCityGen.Planning
{
    public sealed class HighwayGenerator
    {
        private readonly WorldGenerationSettings settings;
        private readonly System.Random random;
        private readonly SeededNameGenerator names;

        public HighwayGenerator(WorldGenerationSettings settings, int seed)
        {
            this.settings = settings ?? new WorldGenerationSettings();
            random = new System.Random(seed);
            names = new SeededNameGenerator(seed);
        }

        public RoadNetworkPlan BuildRoadNetwork(MasterPlan plan)
        {
            var network = new RoadNetworkPlan();
            if (plan.cities.Count == 0)
            {
                return network;
            }

            var majorCities = plan.cities.FindAll(city => city.archetype != CityArchetype.Village);
            if (majorCities.Count == 0)
            {
                majorCities = plan.cities;
            }

            var capital = majorCities[0];
            for (var i = 1; i < majorCities.Count; i++)
            {
                var city = majorCities[i];
                var highway = CreateHighway(capital, city, plan);
                network.Highways.Add(highway);

                if (highway.requiresBridge)
                {
                    network.Bridges.Add(new BridgePlan
                    {
                        name = $"Bridge {highway.name}",
                        from = highway.from,
                        to = highway.to,
                        spanMeters = highway.lengthMeters * 0.15f
                    });
                }

                if (highway.requiresTunnel)
                {
                    network.Tunnels.Add(new TunnelPlan
                    {
                        name = $"Tunnel {highway.name}",
                        from = highway.from,
                        to = highway.to,
                        boreMeters = highway.lengthMeters * 0.12f
                    });
                }
            }

            return network;
        }

        private HighwayPlan CreateHighway(CityPlan capital, CityPlan city, MasterPlan plan)
        {
            var from = capital.position;
            var to = city.position;
            var length = Vector2.Distance(from, to);
            var requiresBridge = settings.generateBridgesAndTunnels && CrossesRiver(from, to, plan.terrainPlan.rivers);
            var requiresTunnel = settings.generateBridgesAndTunnels && !requiresBridge && CrossesMountain(from, to, plan.terrainPlan.mountains);
            return new HighwayPlan
            {
                name = $"Highway {capital.name} - {city.name}",
                from = from,
                to = to,
                lengthMeters = length,
                requiresBridge = requiresBridge,
                requiresTunnel = requiresTunnel
            };
        }

        private bool CrossesRiver(Vector2 from, Vector2 to, List<RiverPlan> rivers)
        {
            foreach (var river in rivers)
            {
                for (var i = 1; i < river.path.Count; i++)
                {
                    var a = river.path[i - 1];
                    var b = river.path[i];
                    if (MinimumDistanceBetweenSegments(from, to, a, b) < river.widthMeters * 0.8f)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private bool CrossesMountain(Vector2 from, Vector2 to, List<MountainRangePlan> mountains)
        {
            foreach (var mountain in mountains)
            {
                if (MinimumDistanceBetweenSegments(from, to, mountain.start, mountain.end) < mountain.widthMeters * 0.7f)
                {
                    return true;
                }
            }

            return false;
        }

        private float MinimumDistanceBetweenSegments(Vector2 p1, Vector2 p2, Vector2 q1, Vector2 q2)
        {
            var d1 = DistanceToSegment(p1, q1, q2);
            var d2 = DistanceToSegment(p2, q1, q2);
            var d3 = DistanceToSegment(q1, p1, p2);
            var d4 = DistanceToSegment(q2, p1, p2);
            return Mathf.Min(Mathf.Min(d1, d2), Mathf.Min(d3, d4));
        }

        private float DistanceToSegment(Vector2 point, Vector2 start, Vector2 end)
        {
            var segment = end - start;
            var length = segment.sqrMagnitude;
            if (length <= 0.0001f)
            {
                return Vector2.Distance(point, start);
            }

            var t = Mathf.Clamp01(Vector2.Dot(point - start, segment) / length);
            return Vector2.Distance(point, start + segment * t);
        }
    }
}
