using System.Collections.Generic;
using UnityEngine;
using ZZCityGen.Data;

namespace ZZCityGen.Simulation
{
    public sealed class TrafficSimulator : MonoBehaviour
    {
        [SerializeField] private int activeVehicles;
        [SerializeField] private float congestionIndex;
        private readonly List<TransportLinkPlan> links = new List<TransportLinkPlan>();
        private WorldGenerationSettings settings;

        public void Configure(MasterPlan plan, WorldGenerationSettings generationSettings)
        {
            settings = generationSettings;
            links.Clear();
            links.AddRange(plan.transportLinks);
            activeVehicles = Mathf.RoundToInt(plan.economy.totalPopulation * 0.12f);
            congestionIndex = CalculateCongestion();
        }

        private void Update()
        {
            if (settings == null || !settings.enableTrafficSimulation || !Application.isPlaying)
            {
                return;
            }

            var rushHourWave = Mathf.Abs(Mathf.Sin(Time.time * 0.03f));
            congestionIndex = Mathf.Clamp01(CalculateCongestion() + rushHourWave * 0.22f);
        }

        private float CalculateCongestion()
        {
            var capacity = Mathf.Max(1, links.Count * 8000);
            return Mathf.Clamp01(activeVehicles / (float)capacity);
        }
    }
}
