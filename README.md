# ZZ CityGen

ZZ CityGen is a smart procedural world and city generator for Unity. It is designed for open-world games, driving games, simulations, city-planning prototypes, engineering research, and educational projects.

## What it Generates

- Continents, island chains, or nation-scale worlds from a deterministic world seed.
- A complete Master Plan before scene generation starts.
- A central capital megacity plus suburbs, rural towns, industrial cities, coastal cities, tourism cities, university cities, and rural villages.
- Specialized districts for business, residential, industrial, education, government, tourism, parks, airports, ports, freight terminals, and utilities.
- Airports, ports, freight terminals, power plants, water treatment facilities, and unique landmarks as first-class plan data.
- Terrain feature plans for rivers, lakes, forests, deserts, and mountain ranges.
- Highway, secondary road, rail, metro, and tram networks with bridge and tunnel requirement flags.
- Economy, population, job sectors, electricity, water, freight, traffic, congestion, growth, and chunk streaming scaffolding.
- Automatic names for worlds, regions, cities, districts, natural features, infrastructure, and landmarks.
- Automatic map layer plans for world, settlement, transport, and infrastructure views.

## Unity Setup

1. Open this repository in Unity.
2. Add a `ZZCityGen.Generation.WorldGenerator` component to a scene object, or use **Tools > ZZ CityGen > World Generator**.
3. Tune the `WorldGenerationSettings` values in the Inspector.
4. Click each generation stage independently or click **Generate Complete World**.
5. Assign an optional `AssetCatalog` to use your own prefabs with footprint-aware lot placement.

## Folder Layout

- `Assets/ZZCityGen/Runtime/Data`: serializable settings, plans, and asset catalog definitions.
- `Assets/ZZCityGen/Runtime/Planning`: deterministic Master Plan generation.
- `Assets/ZZCityGen/Runtime/Generation`: scene generation orchestration.
- `Assets/ZZCityGen/Runtime/Simulation`: economy and traffic simulation scaffolding.
- `Assets/ZZCityGen/Runtime/Streaming`: chunk activation scaffolding.
- `Assets/ZZCityGen/Runtime/Plugins`: extension interfaces and runtime registry.
- `Assets/ZZCityGen/Editor`: Unity Editor control window.
- `Documentation`: architecture and implementation notes.

## Development Status

This repository now contains a production-oriented foundation: data models, deterministic planning, editor controls, plugin hooks, simulation scaffolds, streaming scaffolds, save/load utilities, and placeholder scene generation. The next phase is replacing placeholder primitives with terrain meshes, road splines, prefab libraries, LOD groups, and real-world GIS importers.
