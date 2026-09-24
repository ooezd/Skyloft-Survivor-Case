# A50 diagnostic Profiler capture — September 24, 2026

Capture completed; recording stopped and benchmark app force-stopped. No optimization applied.

- Binary capture: `Builds/Profiler/A50-baseline-diagnostic.data` (203,369,168 bytes).
- Exported main/render inclusive markers: `Builds/Profiler/markers.json`.
- Frame range: 0–1718. Deep Profiling off; GPU module off. CPU, Rendering and Memory enabled.
- Device run `20260924-092534-584`: completed, same baseline source fingerprint, 124 kills,
  223 spawns, peak 100 enemies. Profiler-attached data is excluded from the three-run baseline.
- Thermal observations: AP 33.6 → 53.0 C; battery 31.6 → 31.9 C; observed thermal status 0.
- ADB forwarded local port 34999 to the benchmark Unity socket. Capture started after app launch;
  capture-relative seconds are not exactly gameplay seconds. Last gameplay seconds may extend
  beyond the saved Profiler range; the device run itself completed 180 seconds.
- Export via MCP reported a five-second timeout but completed on the Editor main thread. The
  resulting JSON was parsed successfully; do not repeat capture merely because the RPC timed out.

## Initial findings

Across 369 frames at capture-relative 120–165 seconds:

| Marker | Mean inclusive ms/frame |
| --- | ---: |
| Main thread frame | 122.14 |
| Main: Gfx.WaitForPresentOnGfxThread | 73.02 |
| Main: ScriptRunBehaviourUpdate | 16.96 |
| Main: EnemyController.Update (all enemies combined) | 12.53 |
| Main: Animators.Update | 8.87 |
| Render: Gfx.PresentFrame | 108.90 |
| Render: GfxDeviceVK.Present | 105.79 |

Nested markers overlap; main/render threads run concurrently. Do not add these values or treat
render CPU markers such as Bloom as GPU pass timings. Detailed totals: high-density-summary.json.

The dominant observed pattern is presentation waiting. GPU/render pressure is the first hypothesis,
not a measured GPU-duration conclusion: presentation can also include synchronization/frame pacing.
Enemy Update is a secondary measurable CPU cost, but separation versus other code has not yet been
isolated with finer markers. No evidence yet supports pooling as the primary FPS fix.

Static inspection found Mobile_RPAsset render scale 1, main-light shadowmap resolution 4096 and soft
shadows enabled. These are experiment candidates, not measured per-pass costs.

## Resume plan

1. Open the saved capture in Unity Profiler. Compare low/ramp/high density; inspect Timeline and
   slow frames. Verify active render pipeline/camera/light settings and rendering counters.
2. Run one controlled rendering experiment first (e.g. shadow atlas resolution 4096 → 2048),
   preserving original assets and gameplay load. Assess both frame timing and visible shadows.
   Use separate diagnostic variants if testing render scale or postprocessing; avoid mixing changes.
3. If presentation wait falls, retain justified visual tradeoffs and repeat the unconnected three-run
   protocol. If it does not, investigate GPU/presentation further before broad gameplay refactoring.
4. Profile EnemyController work more narrowly; consider spatial separation and allocation fixes,
   then pooling only where lifecycle spikes/allocations justify it. Verify reuse correctness.
5. Finish normal-device gameplay checks and a clean normal APK, then README, before/after results,
   AI decisions and 3–5 minute video. Keep the September 24 evening / September 25 delivery target.

Binary capture and large marker export stay in ignored Builds; preserve them locally or distribute
as evidence artifacts separately. Source and baseline tag remain unchanged.

References: [Unity Android profiling](https://docs.unity.com/en-us/engine/6000.0/manual/platform-specific/android/developing/testing-and-debugging/profile-on-an-android-device),
[Unity marker interpretation](https://docs.unity3d.com/6000.0/Documentation/Manual/profiler-markers.html).
