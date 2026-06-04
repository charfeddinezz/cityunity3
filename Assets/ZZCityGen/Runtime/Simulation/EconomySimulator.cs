using UnityEngine;
using ZZCityGen.Data;

namespace ZZCityGen.Simulation
{
    public sealed class EconomySimulator : MonoBehaviour
    {
        [SerializeField] private float simulatedYears;
        [SerializeField] private int population;
        [SerializeField] private int jobs;
        [SerializeField] private float electricityMegawatts;
        [SerializeField] private float waterMegalitersPerDay;

        private WorldGenerationSettings settings;

        public void Configure(MasterPlan plan, WorldGenerationSettings generationSettings)
        {
            settings = generationSettings;
            population = plan.economy.totalPopulation;
            jobs = plan.economy.estimatedJobs;
            electricityMegawatts = plan.economy.electricityMegawatts;
            waterMegalitersPerDay = plan.economy.waterMegalitersPerDay;
        }

        private void Update()
        {
            if (settings == null || !settings.enableEconomySimulation || !Application.isPlaying)
            {
                return;
            }

            var yearsDelta = Time.deltaTime / 60f;
            simulatedYears += yearsDelta;
            if (settings.enableDynamicGrowth)
            {
                var growth = Mathf.RoundToInt(population * 0.012f * yearsDelta);
                population += growth;
                jobs += Mathf.RoundToInt(growth * 0.45f);
            }
        }
    }
}
