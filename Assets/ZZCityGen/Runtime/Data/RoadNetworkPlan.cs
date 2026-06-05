using System;
using System.Collections.Generic;
using UnityEngine;

namespace ZZCityGen.Data
{
    [Serializable]
    public sealed class RoadNetworkPlan
    {
        public List<HighwayPlan> Highways = new List<HighwayPlan>();
        public List<BridgePlan> Bridges = new List<BridgePlan>();
        public List<TunnelPlan> Tunnels = new List<TunnelPlan>();
        public List<StreetSegmentPlan> MainStreets = new List<StreetSegmentPlan>();
        public List<StreetSegmentPlan> SecondaryStreets = new List<StreetSegmentPlan>();
        public List<IntersectionPlan> Intersections = new List<IntersectionPlan>();
        public List<RoundaboutPlan> Roundabouts = new List<RoundaboutPlan>();
    }

    [Serializable]
    public sealed class HighwayPlan
    {
        public string name;
        public Vector2 from;
        public Vector2 to;
        public float lengthMeters;
        public bool requiresBridge;
        public bool requiresTunnel;
    }

    [Serializable]
    public sealed class BridgePlan
    {
        public string name;
        public Vector2 from;
        public Vector2 to;
        public float spanMeters;
    }

    [Serializable]
    public sealed class TunnelPlan
    {
        public string name;
        public Vector2 from;
        public Vector2 to;
        public float boreMeters;
    }

    [Serializable]
    public sealed class StreetSegmentPlan
    {
        public string name;
        public Vector2 from;
        public Vector2 to;
        public float lengthMeters;
        public float widthMeters;
        public string roadClass;
    }

    [Serializable]
    public sealed class IntersectionPlan
    {
        public string name;
        public Vector2 position;
        public List<string> connectedSegments = new List<string>();
    }

    [Serializable]
    public sealed class RoundaboutPlan
    {
        public string name;
        public Vector2 center;
        public float radiusMeters;
        public int entryCount;
    }
}
