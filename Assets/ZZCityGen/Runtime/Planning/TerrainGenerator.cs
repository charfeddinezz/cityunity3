using System;
using System.Collections.Generic;
using UnityEngine;
using ZZCityGen.Data;
using ZZCityGen.Utilities;

namespace ZZCityGen.Planning
{
    public sealed class TerrainGenerator
    {
        private readonly WorldGenerationSettings settings;
        private readonly System.Random random;
        private readonly SeededNameGenerator names;

        public TerrainGenerator(WorldGenerationSettings settings, int seed)
        {
            this.settings = settings ?? new WorldGenerationSettings();
            random = new System.Random(seed);
            names = new SeededNameGenerator(seed);
        }

        public TerrainPlan BuildTerrainPlan(Vector2 worldSize)
        {
            var plan = new TerrainPlan();
            BuildMountains(plan, worldSize);
            BuildValleys(plan, worldSize);
            BuildRivers(plan, worldSize);
            BuildLakes(plan, worldSize);
            return plan;
        }

        private void BuildMountains(TerrainPlan plan, Vector2 worldSize)
        {
            var mountainCount = Mathf.Max(1, Mathf.RoundToInt(settings.mountainAmount * 5f + 1f));
            for (var i = 0; i < mountainCount; i++)
            {
                var start = RandomWorldPoint(worldSize);
                var angle = Range(0f, Mathf.PI * 2f);
                var length = worldSize.x * Range(0.12f, 0.24f);
                var end = ClampToWorld(start + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * length, worldSize);
                var width = Mathf.Lerp(worldSize.x * 0.08f, worldSize.x * 0.18f, settings.mountainAmount);
                plan.mountains.Add(new MountainRangePlan
                {
                    name = names.NextFeatureName("MountainRange", i),
                    start = start,
                    end = end,
                    widthMeters = width,
                    peakElevation = Mathf.Clamp01(0.72f + Range(0f, settings.mountainAmount * 0.22f))
                });
            }
        }

        private void BuildValleys(TerrainPlan plan, Vector2 worldSize)
        {
            var valleyCount = Mathf.Max(1, Mathf.RoundToInt((1f - settings.mountainAmount) * 3f + 1f));
            for (var i = 0; i < valleyCount; i++)
            {
                var source = plan.mountains.Count > 1 ? plan.mountains[random.Next(plan.mountains.Count)].start : RandomWorldPoint(worldSize);
                var target = plan.mountains.Count > 1 ? plan.mountains[random.Next(plan.mountains.Count)].end : RandomWorldPoint(worldSize);
                if (Vector2.Distance(source, target) < worldSize.x * 0.12f)
                {
                    target = RandomWorldPoint(worldSize);
                }

                plan.valleys.Add(new ValleyPlan
                {
                    name = names.NextFeatureName("Valley", i),
                    start = source,
                    end = target,
                    widthMeters = Mathf.Lerp(worldSize.x * 0.08f, worldSize.x * 0.16f, 1f - settings.mountainAmount),
                    depth = Mathf.Lerp(0.12f, 0.48f, settings.mountainAmount)
                });
            }
        }

        private void BuildRivers(TerrainPlan plan, Vector2 worldSize)
        {
            var riverCount = Mathf.Max(1, Mathf.RoundToInt(settings.waterAmount * 4f));
            for (var i = 0; i < riverCount; i++)
            {
                var source = plan.mountains.Count > 0 ? ClosestMountainPoint(plan.mountains[random.Next(plan.mountains.Count)].start, worldSize) : RandomWorldPoint(worldSize);
                var path = BuildRiverPath(source, worldSize);
                plan.rivers.Add(new RiverPlan
                {
                    name = names.NextFeatureName("River", i),
                    path = path,
                    widthMeters = Mathf.Lerp(18f, 88f, settings.waterAmount),
                    flowDirection = path.Count >= 2 ? (path[path.Count - 1] - path[0]).normalized : Vector2.up
                });
            }
        }

        private void BuildLakes(TerrainPlan plan, Vector2 worldSize)
        {
            var lakeCount = Mathf.Max(1, Mathf.RoundToInt(settings.waterAmount * 3f));
            for (var i = 0; i < lakeCount; i++)
            {
                var center = RandomWorldPoint(worldSize);
                plan.lakes.Add(new LakePlan
                {
                    name = names.NextFeatureName("Lake", i),
                    center = center,
                    radiusMeters = Mathf.Lerp(worldSize.x * 0.06f, worldSize.x * 0.14f, settings.waterAmount),
                    surfaceElevation = Mathf.Clamp01(Range(0.12f, 0.42f) + settings.waterAmount * 0.08f)
                });
            }
        }

        private List<Vector2> BuildRiverPath(Vector2 source, Vector2 worldSize)
        {
            var path = new List<Vector2> { source };
            var target = ChooseRiverTermination(source, worldSize);
            var segments = random.Next(3, 6);
            var current = source;
            for (var step = 0; step < segments; step++)
            {
                var direction = (target - current).normalized;
                direction += new Vector2(Range(-0.28f, 0.28f), Range(-0.28f, 0.28f));
                direction.Normalize();
                var segmentLength = Range(worldSize.x * 0.12f, worldSize.x * 0.22f);
                current = ClampToWorld(current + direction * segmentLength, worldSize);
                path.Add(current);
            }

            if (path.Count == 1)
            {
                path.Add(target);
            }

            path[path.Count - 1] = target;
            return path;
        }

        private Vector2 ChooseRiverTermination(Vector2 source, Vector2 worldSize)
        {
            if (random.NextDouble() < 0.5)
            {
                var edge = random.Next(4);
                switch (edge)
                {
                    case 0: return new Vector2(0f, Range(0f, worldSize.y));
                    case 1: return new Vector2(worldSize.x, Range(0f, worldSize.y));
                    case 2: return new Vector2(Range(0f, worldSize.x), 0f);
                    default: return new Vector2(Range(0f, worldSize.x), worldSize.y);
                }
            }

            return new Vector2(Range(worldSize.x * 0.15f, worldSize.x * 0.85f), Range(worldSize.y * 0.15f, worldSize.y * 0.85f));
        }

        private Vector2 ClosestMountainPoint(Vector2 point, Vector2 worldSize)
        {
            var direction = new Vector2(Range(-1f, 1f), Range(-1f, 1f)).normalized;
            return ClampToWorld(point + direction * Range(worldSize.x * 0.06f, worldSize.x * 0.16f), worldSize);
        }

        private Vector2 RandomWorldPoint(Vector2 worldSize)
        {
            return new Vector2(Range(0f, worldSize.x), Range(0f, worldSize.y));
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
