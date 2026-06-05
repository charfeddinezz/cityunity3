# WorldGenerator Core

This folder contains the Core Foundation for ZZCityGen's WorldGenerator system.

Components included:
- Settings (WorldSettings ScriptableObject)
- MasterDatabase (in-memory asset registry)
- Save/Load provider interfaces and FileSaveProvider
- Logging via GeneratorLogger
- EventBus for simple topic publish/subscribe
- Validator utility skeleton
- Data models: CityRecord, RoadRecord, LotRecord
- Generator skeleton: MasterPlanGenerator (stub)

Next: wire `WorldGenerator` to load a `WorldSettings` asset, instantiate `MasterPlanGenerator`, and run validation hooks before stages.