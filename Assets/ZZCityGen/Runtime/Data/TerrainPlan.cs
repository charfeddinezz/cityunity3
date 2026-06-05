using System;
using System.Collections.Generic;
using UnityEngine;

namespace ZZCityGen.Data
{
    [Serializable]
    public sealed class TerrainPlan
    {
        public List<MountainRangePlan> mountains = new List<MountainRangePlan>();
        public List<ValleyPlan> valleys = new List<ValleyPlan>();
        public List<RiverPlan> rivers = new List<RiverPlan>();
        public List<LakePlan> lakes = new List<LakePlan>();
    }

    [Serializable]
    public sealed class MountainRangePlan
    {
        public string name;
        public Vector2 start;
        public Vector2 end;
        public float widthMeters;
        public float peakElevation;
    }

    [Serializable]
    public sealed class ValleyPlan
    {
        public string name;
        public Vector2 start;
        public Vector2 end;
        public float widthMeters;
        public float depth;
    }

    [Serializable]
    public sealed class RiverPlan
    {
        public string name;
        public List<Vector2> path = new List<Vector2>();
        public float widthMeters;
        public Vector2 flowDirection;
    }

    [Serializable]
    public sealed class LakePlan
    {
        public string name;
        public Vector2 center;
        public float radiusMeters;
        public float surfaceElevation;
    }
}
