using ZZCityGen.WorldGenerator.Core;
using ZZCityGen.WorldGenerator.Core.Logging;
using ZZCityGen.WorldGenerator.Core.Events;
using ZZCityGen.WorldGenerator.Core.Settings;

namespace ZZCityGen.WorldGenerator.Generators.MasterPlan
{
    public class MasterPlanGenerator
    {
        private readonly MasterDatabase db;
        private readonly WorldSettings settings;

        public MasterPlanGenerator(MasterDatabase db, WorldSettings settings)
        {
            this.db = db;
            this.settings = settings;
        }

        public void Build()
        {
            GeneratorLogger.Info("MasterPlanGenerator", "Starting master plan build");

            // Placeholder: real implementation will sample settings and database assets
            // and create city/road/lot records. For now we emit an event to signal completion.

            EventBus.Publish("MasterPlan:Built", null);
            GeneratorLogger.Info("MasterPlanGenerator", "Master plan build complete (stub)");
        }
    }
}