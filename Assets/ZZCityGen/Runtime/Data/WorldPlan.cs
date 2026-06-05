using System;
using System.Collections.Generic;
using UnityEngine;

namespace ZZCityGen.Data
{
    [Serializable]
    public sealed class WorldPlan
    {
        public int Seed;
        public int WorldSize;
        public List<WorldCityPlan> Cities = new List<WorldCityPlan>();
        public List<WorldRoadPlan> Roads = new List<WorldRoadPlan>();
        public List<WorldRiverPlan> Rivers = new List<WorldRiverPlan>();
        public List<WorldMountainPlan> Mountains = new List<WorldMountainPlan>();
        public List<WorldParkPlan> Parks = new List<WorldParkPlan>();
        public List<WorldDistrictPlan> Districts = new List<WorldDistrictPlan>();
    }

    [Serializable]
    public sealed class WorldCityPlan
    {
        public string Name;
        public Vector2 Position;
        public float Radius;
        public int PopulationTarget;
    }

    [Serializable]
    public sealed class WorldRoadPlan
    {
        public string Name;
        public Vector2 From;
        public Vector2 To;
        public float Width;
    }

    [Serializable]
    public sealed class WorldRiverPlan
    {
        public string Name;
        public Vector2 Start;
        public Vector2 End;
        public float Width;
    }

    [Serializable]
    public sealed class WorldMountainPlan
    {
        public string Name;
        public Vector2 Position;
        public float Radius;
        public float Height;
    }

    [Serializable]
    public sealed class WorldParkPlan
    {
        public string Name;
        public Vector2 Position;
        public float Radius;
    }

    [Serializable]
    public sealed class WorldDistrictPlan
    {
        public string Name;
        public int CityIndex;
        public Rect Bounds;
        public float Density;
    }
}
