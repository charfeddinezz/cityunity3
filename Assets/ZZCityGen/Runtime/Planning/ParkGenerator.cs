using System;
using System.Collections.Generic;
using UnityEngine;
using ZZCityGen.Data;
using ZZCityGen.Utilities;

namespace ZZCityGen.Planning
{
    public sealed class ParkGenerator
    {
        private readonly WorldGenerationSettings settings;
        private readonly System.Random random;
        private readonly SeededNameGenerator names;

        public ParkGenerator(WorldGenerationSettings settings, int seed)
        {
            this.settings = settings ?? new WorldGenerationSettings();
            random = new System.Random(seed + 1927);
            names = new SeededNameGenerator(seed + 1927);
        }

        public void BuildParks(MasterPlan plan)
        {
            if (plan == null || plan.cities == null || !settings.generateParks)
            {
                return;
            }

            foreach (var city in plan.cities)
            {
                foreach (var district in city.districts)
                {
                    if (district.type != DistrictType.Park && district.type != DistrictType.PublicPark)
                    {
                        continue;
                    }

                    BuildDistrictPark(district);
                }
            }
        }

        private void BuildDistrictPark(DistrictPlan district)
        {
            district.ponds = new List<ParkPondPlan>();
            district.trees = new List<ParkTreePlan>();
            district.paths = new List<ParkPathPlan>();

            var interior = GetInteriorBounds(district.bounds, 0.08f);
            if (interior.width <= 2f || interior.height <= 2f)
            {
                return;
            }

            GeneratePonds(district, interior);
            GenerateTrees(district, interior);
            GeneratePaths(district, interior);
        }

        private Rect GetInteriorBounds(Rect bounds, float shrinkRatio)
        {
            var shrinkX = bounds.width * shrinkRatio;
            var shrinkY = bounds.height * shrinkRatio;
            return new Rect(bounds.xMin + shrinkX, bounds.yMin + shrinkY, bounds.width - shrinkX * 2f, bounds.height - shrinkY * 2f);
        }

        private void GeneratePonds(DistrictPlan district, Rect interior)
        {
            var pondCount = Mathf.Clamp(Mathf.RoundToInt(settings.waterAmount * 2f), 1, 3);
            for (var i = 0; i < pondCount; i++)
            {
                var radius = Mathf.Lerp(6f, 18f, (float)random.NextDouble()) * Mathf.Clamp01(district.density + 0.3f);
                var position = new Vector2(
                    Range(interior.xMin + radius, interior.xMax - radius),
                    Range(interior.yMin + radius, interior.yMax - radius));

                district.ponds.Add(new ParkPondPlan
                {
                    name = names.NextFeatureName("Pond", i),
                    center = position,
                    radiusMeters = Mathf.Min(radius, Mathf.Min(interior.width, interior.height) * 0.22f)
                });
            }
        }

        private void GenerateTrees(DistrictPlan district, Rect interior)
        {
            var densityFactor = Mathf.Lerp(0.38f, 0.92f, district.density);
            var treeCount = Mathf.RoundToInt((interior.width * interior.height) / 450f * densityFactor);
            treeCount = Mathf.Clamp(treeCount, 10, 120);

            for (var i = 0; i < treeCount; i++)
            {
                var position = GetTreePosition(district, interior);
                district.trees.Add(new ParkTreePlan
                {
                    name = names.NextFeatureName("Tree", i),
                    position = position,
                    heightMeters = Mathf.Lerp(4.5f, 11f, (float)random.NextDouble())
                });
            }
        }

        private Vector2 GetTreePosition(DistrictPlan district, Rect interior)
        {
            for (var attempt = 0; attempt < 16; attempt++)
            {
                var candidate = new Vector2(
                    Range(interior.xMin, interior.xMax),
                    Range(interior.yMin, interior.yMax));

                if (IsPositionInPond(candidate, district.ponds, 2f))
                {
                    continue;
                }

                return candidate;
            }

            return interior.center;
        }

        private bool IsPositionInPond(Vector2 position, List<ParkPondPlan> ponds, float buffer)
        {
            foreach (var pond in ponds)
            {
                if (Vector2.Distance(position, pond.center) < pond.radiusMeters + buffer)
                {
                    return true;
                }
            }
            return false;
        }

        private void GeneratePaths(DistrictPlan district, Rect interior)
        {
            var pathCount = Mathf.Clamp(Mathf.RoundToInt(1 + district.density * 2f), 1, 3);
            for (var i = 0; i < pathCount; i++)
            {
                var path = new ParkPathPlan
                {
                    name = names.NextFeatureName("ParkPath", i),
                    widthMeters = Mathf.Lerp(1.5f, 3.8f, district.density)
                };

                var start = new Vector2(
                    i % 2 == 0 ? interior.xMin : Range(interior.xMin, interior.xMax),
                    Range(interior.yMin, interior.yMax));
                var end = new Vector2(
                    i % 2 == 1 ? interior.xMax : Range(interior.xMin, interior.xMax),
                    Range(interior.yMin, interior.yMax));

                path.pathPoints.Add(start);
                path.pathPoints.Add(interior.center);
                path.pathPoints.Add(end);
                district.paths.Add(path);
            }
        }

        private float Range(float min, float max)
        {
            return Mathf.Lerp(min, max, (float)random.NextDouble());
        }
    }
}
