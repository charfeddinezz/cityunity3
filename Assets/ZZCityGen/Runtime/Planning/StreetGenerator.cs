using System;
using System.Collections.Generic;
using UnityEngine;
using ZZCityGen.Data;
using ZZCityGen.Utilities;

namespace ZZCityGen.Planning
{
    public sealed class StreetGenerator
    {
        private readonly WorldGenerationSettings settings;
        private readonly System.Random random;
        private readonly SeededNameGenerator names;

        public StreetGenerator(WorldGenerationSettings settings, int seed)
        {
            this.settings = settings ?? new WorldGenerationSettings();
            random = new System.Random(seed + 871);
            names = new SeededNameGenerator(seed + 871);
        }

        public RoadNetworkPlan BuildStreetNetwork(MasterPlan plan)
        {
            var network = new RoadNetworkPlan();
            if (plan == null || plan.cities == null)
            {
                return network;
            }

            foreach (var city in plan.cities)
            {
                BuildCityStreetNetwork(network, city, plan.worldSizeMeters);
            }

            return network;
        }

        private void BuildCityStreetNetwork(RoadNetworkPlan network, CityPlan city, Vector2 worldSize)
        {
            if (city == null)
            {
                return;
            }

            var mainDestinations = GetMainStreetDestinations(city, worldSize);
            var citySegments = new List<StreetSegmentPlan>();
            var mainStreetSegments = new List<StreetSegmentPlan>();
            for (var i = 0; i < mainDestinations.Count; i++)
            {
                var target = mainDestinations[i];
                var street = CreateStreetSegment($"Main Street {city.name} {i + 1}", city.position, target, 14f, "Main");
                mainStreetSegments.Add(street);
                citySegments.Add(street);
                network.MainStreets.Add(street);
            }

            for (var i = 0; i < mainDestinations.Count; i++)
            {
                var a = mainDestinations[i];
                var b = mainDestinations[(i + 1) % mainDestinations.Count];
                var ring = CreateStreetSegment($"Main Ring {city.name} {i + 1}", a, b, 12f, "Main");
                mainStreetSegments.Add(ring);
                citySegments.Add(ring);
                network.MainStreets.Add(ring);
            }

            foreach (var district in city.districts)
            {
                BuildSecondaryDistrictStreets(network, city, district, mainStreetSegments, citySegments, worldSize);
            }

            CreateCityIntersections(network, city, citySegments);
            CreateCityRoundabouts(network, city);
        }

        private List<Vector2> GetMainStreetDestinations(CityPlan city, Vector2 worldSize)
        {
            var targets = new List<Vector2>();
            var demand = Mathf.Clamp(city.districts.Count, 3, 5);

            foreach (var district in city.districts)
            {
                if (targets.Count >= demand)
                {
                    break;
                }

                targets.Add(ClampToWorld(district.bounds.center, worldSize));
            }

            var angleStep = 360f / demand;
            for (var i = targets.Count; i < demand; i++)
            {
                var angle = i * angleStep * Mathf.Deg2Rad;
                var point = city.position + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * city.radiusMeters * 0.86f;
                targets.Add(ClampToWorld(point, worldSize));
            }

            return targets;
        }

        private void BuildSecondaryDistrictStreets(RoadNetworkPlan network, CityPlan city, DistrictPlan district, List<StreetSegmentPlan> mainStreets, List<StreetSegmentPlan> citySegments, Vector2 worldSize)
        {
            if (district == null)
            {
                return;
            }

            var center = ClampToWorld(district.bounds.center, worldSize);
            var halfWidth = district.bounds.width * 0.45f;
            var halfHeight = district.bounds.height * 0.45f;
            var west = ClampToWorld(new Vector2(center.x - halfWidth, center.y), worldSize);
            var east = ClampToWorld(new Vector2(center.x + halfWidth, center.y), worldSize);
            var south = ClampToWorld(new Vector2(center.x, center.y - halfHeight), worldSize);
            var north = ClampToWorld(new Vector2(center.x, center.y + halfHeight), worldSize);

            var horizontal = CreateStreetSegment($"Secondary Street {district.name} East-West", west, east, 8f, "Secondary");
            var vertical = CreateStreetSegment($"Secondary Street {district.name} North-South", south, north, 8f, "Secondary");
            network.SecondaryStreets.Add(horizontal);
            network.SecondaryStreets.Add(vertical);
            citySegments.Add(horizontal);
            citySegments.Add(vertical);

            ConnectToNearestMain(network, city, horizontal.from, mainStreets, citySegments);
            ConnectToNearestMain(network, city, horizontal.to, mainStreets, citySegments);
            ConnectToNearestMain(network, city, vertical.from, mainStreets, citySegments);
            ConnectToNearestMain(network, city, vertical.to, mainStreets, citySegments);
        }

        private void ConnectToNearestMain(RoadNetworkPlan network, CityPlan city, Vector2 point, List<StreetSegmentPlan> mainStreets, List<StreetSegmentPlan> citySegments)
        {
            if (mainStreets == null || mainStreets.Count == 0)
            {
                return;
            }

            var nearest = FindNearestMainNode(mainStreets, point);
            if (Vector2.Distance(nearest, point) < 1f)
            {
                return;
            }

            var connector = CreateStreetSegment($"Secondary Connector {city.name} {point.x:0}_{point.y:0}", point, nearest, 6f, "Secondary");
            network.SecondaryStreets.Add(connector);
            citySegments.Add(connector);
        }

        private Vector2 FindNearestMainNode(List<StreetSegmentPlan> mainStreets, Vector2 point)
        {
            var best = point;
            var bestDistance = float.MaxValue;
            foreach (var segment in mainStreets)
            {
                EvaluateEndpoint(segment.from);
                EvaluateEndpoint(segment.to);
            }

            return best;

            void EvaluateEndpoint(Vector2 endpoint)
            {
                var distance = Vector2.Distance(endpoint, point);
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    best = endpoint;
                }
            }
        }

        private void CreateCityIntersections(RoadNetworkPlan network, CityPlan city, List<StreetSegmentPlan> citySegments)
        {
            for (var i = 0; i < citySegments.Count; i++)
            {
                for (var j = i + 1; j < citySegments.Count; j++)
                {
                    if (TryGetSegmentIntersection(citySegments[i].from, citySegments[i].to, citySegments[j].from, citySegments[j].to, out var intersection)
                        && !IsEndpointIntersection(intersection, citySegments[i])
                        && !IsEndpointIntersection(intersection, citySegments[j]))
                    {
                        AddOrMergeIntersection(network, intersection, citySegments[i].name, citySegments[j].name);
                    }
                }
            }

            var mainNames = new List<string>();
            foreach (var segment in citySegments)
            {
                if (Vector2.Distance(segment.from, city.position) < 1f || Vector2.Distance(segment.to, city.position) < 1f)
                {
                    mainNames.Add(segment.name);
                }
            }

            if (mainNames.Count > 1)
            {
                AddOrMergeIntersection(network, city.position, mainNames.ToArray());
            }
        }

        private void AddOrMergeIntersection(RoadNetworkPlan network, Vector2 position, params string[] connectedSegments)
        {
            const float snapThreshold = 12f;
            var existing = network.Intersections.Find(i => Vector2.Distance(i.position, position) < snapThreshold);
            if (existing != null)
            {
                foreach (var segmentName in connectedSegments)
                {
                    if (!existing.connectedSegments.Contains(segmentName))
                    {
                        existing.connectedSegments.Add(segmentName);
                    }
                }
                return;
            }

            network.Intersections.Add(new IntersectionPlan
            {
                name = $"Intersection {position.x:0}_{position.y:0}",
                position = position,
                connectedSegments = new List<string>(connectedSegments)
            });
        }

        private void CreateCityRoundabouts(RoadNetworkPlan network, CityPlan city)
        {
            if (!settings.generateRoundabouts)
            {
                return;
            }

            var cityIntersections = network.Intersections.FindAll(i => Vector2.Distance(i.position, city.position) <= city.radiusMeters * 1.2f);
            foreach (var intersection in cityIntersections)
            {
                if (intersection.connectedSegments.Count < 3)
                {
                    continue;
                }

                var radius = Mathf.Clamp(city.radiusMeters * 0.06f, 10f, 30f);
                network.Roundabouts.Add(new RoundaboutPlan
                {
                    name = $"Roundabout {intersection.name}",
                    center = intersection.position,
                    radiusMeters = radius,
                    entryCount = Mathf.Clamp(intersection.connectedSegments.Count, 3, 6)
                });
            }

            if (network.Roundabouts.Count == 0 && city.districts.Count > 2)
            {
                network.Roundabouts.Add(new RoundaboutPlan
                {
                    name = $"Roundabout City Center {city.name}",
                    center = city.position,
                    radiusMeters = Mathf.Clamp(city.radiusMeters * 0.05f, 12f, 28f),
                    entryCount = Mathf.Clamp(city.districts.Count, 3, 6)
                });
            }
        }

        private StreetSegmentPlan CreateStreetSegment(string name, Vector2 from, Vector2 to, float widthMeters, string streetClass)
        {
            return new StreetSegmentPlan
            {
                name = name,
                from = from,
                to = to,
                lengthMeters = Vector2.Distance(from, to),
                widthMeters = widthMeters,
                roadClass = streetClass
            };
        }

        private bool TryGetSegmentIntersection(Vector2 a1, Vector2 a2, Vector2 b1, Vector2 b2, out Vector2 intersection)
        {
            intersection = default;
            var denominator = (a1.x - a2.x) * (b1.y - b2.y) - (a1.y - a2.y) * (b1.x - b2.x);
            if (Mathf.Abs(denominator) < 0.0001f)
            {
                return false;
            }

            var a = a1.x * a2.y - a1.y * a2.x;
            var b = b1.x * b2.y - b1.y * b2.x;
            var x = (a * (b1.x - b2.x) - (a1.x - a2.x) * b) / denominator;
            var y = (a * (b1.y - b2.y) - (a1.y - a2.y) * b) / denominator;
            intersection = new Vector2(x, y);

            return IsPointOnSegment(intersection, a1, a2) && IsPointOnSegment(intersection, b1, b2);
        }

        private bool IsPointOnSegment(Vector2 point, Vector2 start, Vector2 end)
        {
            var minX = Mathf.Min(start.x, end.x) - 0.5f;
            var maxX = Mathf.Max(start.x, end.x) + 0.5f;
            var minY = Mathf.Min(start.y, end.y) - 0.5f;
            var maxY = Mathf.Max(start.y, end.y) + 0.5f;
            return point.x >= minX && point.x <= maxX && point.y >= minY && point.y <= maxY;
        }

        private bool IsEndpointIntersection(Vector2 point, StreetSegmentPlan segment)
        {
            return Vector2.Distance(point, segment.from) < 1f || Vector2.Distance(point, segment.to) < 1f;
        }

        private Vector2 ClampToWorld(Vector2 position, Vector2 worldSize)
        {
            return new Vector2(
                Mathf.Clamp(position.x, 0f, worldSize.x),
                Mathf.Clamp(position.y, 0f, worldSize.y));
        }
    }
}
