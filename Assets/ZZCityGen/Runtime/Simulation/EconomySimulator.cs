using UnityEngine;
using ZZCityGen.Data;

namespace ZZCityGen.Simulation
{
    public sealed class EconomySimulator : MonoBehaviour
    {
        [SerializeField] private float simulatedYears;
        [SerializeField] private int population;
        [SerializeField] private int jobs;
        [SerializeField] private int publicServiceJobs;
        [SerializeField] private int industrialJobs;
        [SerializeField] private int tourismJobs;
        [SerializeField] private float electricityMegawatts;
        [SerializeField] private float waterMegalitersPerDay;
        [SerializeField] private float freightTonsPerDay;

        private WorldGenerationSettings settings;

        public void Configure(MasterPlan plan, WorldGenerationSettings generationSettings)
        {
            settings = generationSettings;
            population = plan.economy.totalPopulation;
            jobs = plan.economy.estimatedJobs;
            publicServiceJobs = plan.economy.publicServiceJobs;
            industrialJobs = plan.economy.industrialJobs;
            tourismJobs = plan.economy.tourismJobs;
            electricityMegawatts = plan.economy.electricityMegawatts;
            waterMegalitersPerDay = plan.economy.waterMegalitersPerDay;
            freightTonsPerDay = plan.economy.freightTonsPerDay;
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
                publicServiceJobs += Mathf.RoundToInt(growth * 0.08f);
                industrialJobs += Mathf.RoundToInt(growth * 0.1f);
                tourismJobs += Mathf.RoundToInt(growth * 0.04f);
                electricityMegawatts += growth * 0.0016f;
                waterMegalitersPerDay += growth * 0.00022f;
                freightTonsPerDay += growth * 0.015f;
            }
        }
    }
}
