# Accepted unoptimized Android baseline — September 24, 2026

Three fresh-process runs completed on the Samsung Galaxy A50 (SM-A505F). This checkpoint adds
measurement infrastructure, with no gameplay performance optimization. Local tag: `baseline-gameplay`.
The tag includes this evidence; measured source is commit `d4fd79f` (unchanged Assets/Packages/ProjectSettings).

## Build and protocol

- Unity 6000.4.10f1; Android 11/API 30; Mali-G72; Vulkan; Mobile quality; 1080×2340.
- Development ARM64 APK, clean build, no Deep Profiling or Profiler autoconnect; target 60 FPS.
- `hard-route-v1`: 180-second Hard combat, seed 230926, fixed input route, cap 100 enemies.
- First 15 gameplay seconds excluded from aggregates, retained in CSV. Player protected from death;
  upgrades disabled; normal attacks, enemy deaths, contact feedback and lifecycle work retained.
- Source SHA256: `cd0931caedfdd988eb2ce979f3d4ff5a619b35be566110da1e3e7111bf655aef`.
- APK SHA256: `162c4b02dd36f11d752f94c9b779941f449722eddf1f96128c8dc76902185c9d`.
- APK: `Builds/Benchmark/Skyloft-Benchmark.apk`; package `com.ooezd.skyloft.benchmark`.
- Device identity matched the clean-build manifest. Working-tree source hash independently rechecked.
  Full manifest: [build.json](Results/build.json).

## Measurements

Frame times are milliseconds; lower is better. Mean enemies is time-weighted. Raw started/summary
JSON and frame CSV are retained in each run directory under Results.

| Run (UTC ID) | Mean ms | p95 ms | p99 ms | Kills / spawns | Mean / peak enemies |
| --- | ---: | ---: | ---: | ---: | ---: |
| 1 (`20260924-075520-273`) | 105.69 | 156.11 | 222.51 | 124 / 224 | 75.10 / 100 |
| 2 (`20260924-090257-781`) | 100.77 | 148.12 | 174.51 | 124 / 223 | 74.36 / 100 |
| 3 (`20260924-091349-543`) | 105.06 | 154.95 | 230.04 | 123 / 222 | 75.07 / 100 |

Median of the three run means: **105.06 ms**.
Median of the three per-run p95 values: **154.95 ms**.
These are descriptive summaries of three runs, not a pooled percentile or a confidence interval.
Mean frame intervals correspond to approximately 9.5–9.9 FPS during the measured workload.
No difference between these runs is an optimization gain: they use the same APK.

All three reach 100 active enemies; the 120–180 second windows average about 99.6–99.8 enemies.
Total kills (123–124), spawns (222–224) and mean population (74.36–75.10) are close, but simulation
state is not identical. Frame-dependent movement, targeting and cap retries can diverge. Preserve
this protocol in the optimized build, repeat three runs, and check per-window load as well as totals.
A >5% workload difference is an analyzer review flag, not a statistical acceptance threshold.

## Thermal observations and limitations

| Run | AP °C before → after | Battery °C before → after |
| --- | ---: | ---: |
| 1 | 34.4 → 50.0 | 30.1 → 30.6 |
| 2 | 31.2 → 48.2 | 27.8 → 28.9 |
| 3 | 33.3 → 50.7 | 30.1 → 30.8 |

USB powered, battery 100%, power saving off for the runs. Observed Android thermal status was 0
before/after, which does not prove the absence of hardware throttling. Run 2 followed a short
unmeasured conditioning run and 30 seconds idle. Temperature histories are not identical. Values
were sampled outside measurement, not continuously. Brightness and background activity were not
independently logged. See [context.json](Results/context.json) for retained observations.
The p99 spread (174.51–230.04 ms) especially cautions against claiming small gains from one run.

The recorder measures wall-time Update intervals. Main Thread includes waits and is not CPU busy
time. Rendering counters are unavailable; no GPU timing was collected. GC samples are available,
but allocations alone do not establish the dominant bottleneck. Development-build and recorder
overhead are included; this is not a release-build FPS claim or a normal-game balance test.

## Validation and excluded runs

The Python analyzer passed CSV/summary frame counts, warmup counts, positive frame times,
monotonic timestamps and p95 consistency on all three accepted runs. Source identity and completed
device status matched. Prior functional evidence includes 11 benchmark checks, 29 wave checks and
one full Editor integration, with lifetime kills unchanged; see [Validation](Validation/README.md).

- `20260923-204602-866`: early device smoke test; old source identity, excluded.
- `20260923-205051-894`: rejected incremental build with stale injected identity; excluded.
  Keep [BUILD_IDENTITY.md](BUILD_IDENTITY.md) as the failure/correction audit.
- `20260924-074342-076`: invalid-paused at 118.31 seconds, old identity; excluded.
- `20260924-090053-401`: short conditioning run, force-stopped with started.json only; excluded.
- Editor results are functional evidence only, never Android baseline measurements.

## Next action

Capture a separate A50 Unity Profiler session at low, ramping and high enemy density. Determine
whether rendering/skinning/Animator, movement/separation or object lifecycle dominates before
choosing optimizations. Broad cleanup/refactoring is lower priority than measured costs and final
case deliverables. See [NEXT_STEPS.md](NEXT_STEPS.md) for the September 24 delivery plan.

Use a **Clean Build** for the normal final APK after benchmark builds so cached processed scene
data cannot retain the injected benchmark runner. Verify normal touch controls, death, replay,
persistence, upgrades and all three difficulties separately.
