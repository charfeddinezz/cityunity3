# ZZ CityGen Architecture

ZZ CityGen is organized as a Unity-first procedural world generation toolkit. The generator always creates a deterministic Master Plan before it instantiates scene objects, which keeps terrain, cities, roads, infrastructure, economy, and streaming decisions consistent.

## Pipeline

1. **Master Plan**: Calculates the world seed, regions, city hierarchy, district layout, natural features, transport links, and macro economy.
2. **Terrain Stage**: Converts natural feature plans into terrain markers and future terrain-authoring inputs.
3. **City Stage**: Splits each city into specialized districts and places lots using the asset catalog footprint rules.
4. **Transport Stage**: Connects cities with highways and rail and flags bridge/tunnel requirements when terrain or water crossings are predicted.
5. **Simulation Stage**: Configures economy, traffic, growth, and chunk streaming systems.
6. **Optimization Stage**: Marks generated roots as static and prepares the world for LOD and visibility optimization passes.

## Extension Points

- `AssetCatalog` stores prefab dimensions, footprints, height, priority, and allowed districts so lots can select compatible objects without overlap.
- `IZCityGenPlugin` lets custom packages add city types, buildings, landmarks, or simulation rules by extending the Master Plan before generation.
- `WorldGenerationSettings` exposes world size, city counts, climate, population, transport, simulation, LOD, and streaming parameters in the Unity Inspector.

## Unity Compatibility

The scripts use standard Unity runtime/editor APIs and assembly definitions, so the project is prepared for Unity 6-era editors, including future Unity 2026 installations that support the same C# and UnityEngine API surface.
