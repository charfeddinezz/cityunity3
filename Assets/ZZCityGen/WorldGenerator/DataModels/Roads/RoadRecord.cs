using UnityEngine;

namespace ZZCityGen.WorldGenerator.DataModels.Roads
{
    [System.Serializable]
    public class RoadRecord
    {
        public string id;
        public string name;
        public Vector2[] points;
        public float width = 4f;
        public int lanes = 2;
        public string[] tags;
    }
}