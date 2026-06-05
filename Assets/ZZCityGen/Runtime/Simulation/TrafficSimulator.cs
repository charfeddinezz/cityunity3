using System.Collections.Generic;
using UnityEngine;
using ZZCityGen.Data;

namespace ZZCityGen.Simulation
{
    public sealed class TrafficSimulator : MonoBehaviour
    {
        [SerializeField] private int activeVehicles;
        [SerializeField] private int activeTransitVehicles;
        [SerializeField] private int activeFreightVehicles;
        [SerializeField] private float congestionIndex;
        private readonly List<TransportLinkPlan> links = new List<TransportLinkPlan>();
        private readonly List<TrafficRoutePlan> trafficRoutes = new List<TrafficRoutePlan>();
        private WorldGenerationSettings settings;

        public void Configure(MasterPlan plan, WorldGenerationSettings generationSettings)
        {
            settings = generationSettings;
            links.Clear();
            links.AddRange(plan.transportLinks);
            trafficRoutes.Clear();
            if (plan.trafficRoutes != null)
            {
                trafficRoutes.AddRange(plan.trafficRoutes);
            }

            var carRoutes = trafficRoutes.FindAll(route => route.type == TransportType.Car).Count;
            var busRoutes = trafficRoutes.FindAll(route => route.type == TransportType.Bus).Count;
            var trainRoutes = trafficRoutes.FindAll(route => route.type == TransportType.Rail).Count;
            var metroRoutes = trafficRoutes.FindAll(route => route.type == TransportType.Metro).Count;

            activeVehicles = Mathf.RoundToInt(plan.economy.totalPopulation * (0.12f + carRoutes * 0.0025f));
            activeTransitVehicles = Mathf.RoundToInt(busRoutes * 6f + trainRoutes * 8f + metroRoutes * 10f + plan.transportLinks.Count * 0.7f);
            activeFreightVehicles = Mathf.RoundToInt(plan.economy.freightTonsPerDay / 18f);
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
            var capacity = 0;
            foreach (var link in links)
            {
                capacity += GetLinkCapacity(link.type);
            }

            var weightedVehicles = activeVehicles + activeFreightVehicles * 2 - activeTransitVehicles * 12;
            return Mathf.Clamp01(Mathf.Max(0, weightedVehicles) / (float)Mathf.Max(1, capacity));
        }

        private int GetLinkCapacity(TransportType type)
        {
            switch (type)
            {
                case TransportType.Highway:
                    return 14000;
                case TransportType.Rail:
                case TransportType.Metro:
                    return 22000;
                case TransportType.Tram:
                    return 9000;
                case TransportType.Bus:
                    return 12000;
                case TransportType.Car:
                    return 9000;
                default:
                    return 6500;
            }
        }
    }
}
