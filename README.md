# Flying AI Swarm

**A Unity/C# prototype for flying-agent behaviour, formations and pooled combat objects.**

The project combines a Burst-compiled flocking job, spatial neighbour lookup, configurable formation strategies and finite-state machines. It is a technical source-code sample for exploring how those systems fit together.

[Source walkthrough](https://portfolio.michaelrusinek.workers.dev/work/flying-ai-swarm/) · [Polish setup guide](SETUP_GUIDE_PL.md)

## Start with these files

| File | What to inspect |
| --- | --- |
| [FlockingJob.cs](Assets/Scripts/FlyingAI/Flocking/FlockingJob.cs) | `IJobParallelFor`, separation/alignment/cohesion and spatial-hash neighbour lookup |
| [FlockingSystem.cs](Assets/Scripts/FlyingAI/Flocking/FlockingSystem.cs) | Persistent native buffers, entity registration, job scheduling and result application |
| [FormationFactory.cs](Assets/Scripts/FlyingAI/Formations/FormationFactory.cs) | Creating formation implementations from configuration |
| [FlyingAIStateMachine.cs](Assets/Scripts/FlyingAI/States/FlyingAIStateMachine.cs) | State transitions for individual agents |
| [ObjectPoolBase.cs](Assets/Scripts/FlyingAI/Pooling/ObjectPoolBase.cs) | Prewarming, spawn/despawn, pool growth and cleanup |
| [DemoSceneBuilder.cs](Assets/Scripts/Editor/DemoSceneBuilder.cs) | Editor menu that assembles a demonstration scene |

## How the flocking path works

`FlockingSystem` keeps agent data in persistent `NativeArray` buffers. The flocking job reads a spatial hash and sorted agent indices, examines adjacent cells, and writes steering results. The orchestrator schedules jobs and applies their results back to Unity agents.

Formation creation is separated through `IFormation` and `FormationFactory`. The factory selects V, circle, grid, line, diamond or custom formations from a `FormationConfig`. Enemy and projectile pools reuse component instances and can expand when configured to do so.

These are code-level mechanisms. Their presence alone does not establish a frame-rate target, allocation budget or production-readiness claim.

## Open the project

1. Clone the repository and add its root folder to Unity Hub.
2. Use **Unity 6000.4.6f1**, the version recorded in [ProjectVersion.txt](ProjectSettings/ProjectVersion.txt). Allow Unity to resolve [the package manifest](Packages/manifest.json) and review any import or compilation errors before proceeding.
3. In a disposable scene, select **Flying AI → Generate Demo Scene**. This command creates a new scene and a demo prefab; save any scene work first.
4. Review the generated managers, pool and configuration assignments. The builder looks up existing configuration assets; missing assets or references require setup.
5. The demo controls are **S** to spawn an enemy, **F** to spawn a formation and **K** to clear enemies. See [DemoSpawner.cs](Assets/Scripts/FlyingAI/Demo/DemoSpawner.cs).

The repository includes tweening and UniTask components under `Assets/Plugins`. Package resolution, compilation and the complete demo flow have **not been rerun for this documentation update**. The older Polish guide is supplementary; the committed editor version and source are authoritative where they differ.

## Performance and limitations

- The source uses Jobs, Burst, native containers and spatial hashing. No reproducible profiler capture or benchmark harness accompanies the previously advertised FPS results, so those results are not presented as verified measurements here.
- Spatial hashing narrows neighbour searches; it does not guarantee linear cost for every agent distribution.
- Pooling reduces repeated instantiation, but expansion still creates objects and the cleanup path copies the active set. Pooling is not a guarantee of zero allocations across the application.
- The generated demo does not add a `FlockingSystem` component. Inspect the intended scene wiring before using it to evaluate the job pipeline.
- The walkthrough diagram is a source-derived schematic, not a gameplay recording.

For a useful benchmark, record the commit, hardware, Unity version, build settings, agent count and scene workload together with CPU/frame-time and GC-allocation captures. Keep editor and standalone-player measurements separate.

## Scope and licensing

This repository is a prototype for code review and experimentation. Original source code is covered by the [MIT license](LICENSE). Bundled third-party plugins and assets retain their own licensing terms.
