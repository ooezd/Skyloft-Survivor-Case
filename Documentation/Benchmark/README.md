# Android baseline protocol

## Decision and scope

Reference device: Samsung Galaxy A50 (user-provided; record actual model/OS/GPU from the run).
The older installed gameplay APK is a build/install smoke test, not the reference for this comparison.
Both baseline and optimized builds must include the same benchmark protocol and measurement code.

This evening: validate the runner, build/install the current unoptimized gameplay, capture repeated
device runs, then identify and tag the baseline commit. No pooling, mesh reduction, rendering tuning,
or gameplay refactor belongs in this checkpoint.

## What the test guarantees

`hard-route-v1` runs the existing Game scene and Hard configuration for 180 gameplay seconds.
The runner injects directional input into the existing PlayerController. Each six-second segment is
three seconds moving followed by three seconds stationary; directions are right, forward, left,
back. The 24-second cycle repeats. The speed, attack range, fire interval and enemy behavior remain
the normal gameplay values.

- Spawn angles use a private seeded RNG (`230926`), independent of global Unity random consumers.
- Player health is clamped to at least one. Contact attacks and hit feedback still execute.
- Upgrade spawning is disabled to avoid variable attack/damage multipliers.
- Normal enemy death, targeting, projectiles, VFX, damage numbers and wave scheduling execute.
- Lifetime kills are not written by benchmark matches (including Editor validation).
- Target is 60 FPS, vSync count zero, screen sleep disabled during the run. Previous values are restored.
- The first 15 gameplay seconds warm up actual combat; they remain in raw CSV but are excluded from
  aggregate frame statistics. Spawn/asset startup spikes are therefore still inspectable separately.

This is **repeatable input, not deterministic lockstep simulation**. Delta-time integration, spawn
retry timing at the cap, targeting and kill timing can diverge with frame rate. We deliberately do not
set `Time.captureFramerate`, force deltaTime, or rewrite gameplay to conceal these differences.
World-space route drift, kills, spawn counts and active enemies are recorded. Compare workloads as
well as timing; a faster run with substantially fewer enemies is not sufficient optimization evidence.
The forced survival and disabled upgrades mean this is not a difficulty/balance acceptance test.

If workload divergence prevents a useful conclusion, add a separately versioned fixed-population
25/50/100-enemy scenario with shooting disabled. That isolates crowd/rendering/animation and does
not replace this combat/lifecycle workload. Do not change the protocol between baseline and final.

## Build and launch

1. Use Unity 6000.4.10f1, Android selected, Game scene saved, match duration 180.
2. Run `Skyloft > Validation > Check Benchmark Protocol` and `Check Wave System`.
3. Run `Skyloft > Benchmark > Build Android APK`.
4. Output: `Builds/Benchmark/Skyloft-Benchmark.apk` and `build.txt`.
5. Install the APK on the USB-debugging-enabled A50. Package ID is
   `com.ooezd.skyloft.benchmark`, display name `Survivinho Benchmark`. It does not replace the normal game.
6. Launch the benchmark app. The full run begins automatically, with no touch input required.
   On completion the regular win screen appears. Relaunch the process for each independent repeat;
   the in-game Replay button is not the benchmark repetition workflow.

The build is Development, without Deep Profiling, script debugging or Profiler autoconnect.
The build processor injects the runner into the build's copy of the scene, not the saved scene.
`SKYLOFT_BENCHMARK` is a build-only define, not a permanent Player Setting. Normal builds exclude
the runner and benchmark gameplay hooks. Product name/package ID/app-bundle settings are restored
in a `finally` block. Do not build another player concurrently.

`build.txt` and each run contain a source fingerprint over Assets, Packages and ProjectSettings
(paths and contents, excluding file timestamps). It is computed before temporary product settings.
MCP can pass a source revision label to `BenchmarkBuild.Build("<revision>")`; the menu defaults to
`working-tree`. A fingerprint is identity evidence, not a replacement for the final baseline Git tag.

## Results and measurement limits

Output is under `Application.persistentDataPath/Benchmarks/<UTC timestamp>/`:

- `started.json`: run identity/configuration, written before timing starts. A folder with only this
  file indicates an incomplete/crashed run and is not a valid measurement.
- `frames.csv`: preallocated in-memory samples written after the run, with wall/game time, frame
  interval, active enemies, kills, spawns, player X/Z, GC bytes, Main Thread nanoseconds, total used
  memory, draw calls and triangles.
- `summary.json`: status, source identity, device/OS/GPU/API, resolution, quality, mean/median/p95/p99/
  maximum frame time, time-weighted mean enemy count, peak enemies, kills/spawns and peak memory.

Only `status=completed` device runs count as baseline measurements. Pause/focus loss, interruption
and buffer overflow produce invalid statuses. Editor runs are explicitly marked `editor=true` and
are functional checks only. OS process termination may prevent the summary from being written.

Frame interval is real wall time between runner Updates, not GPU duration. `Main Thread` may include
frame limiting and waits; it is not CPU busy time. Counter data comes from completed frames and can
lag the workload columns. Unsupported/unpopulated counters are `-1`, not zero. No GPU timing is
claimed by this runner. Use a separate Unity Profiler capture to investigate CPU/GPU bottlenecks;
keep that diagnostic capture separate from unattached comparison runs.

No per-frame file I/O, strings, LINQ, logging or dynamic buffer growth occurs in the sampler. Its
fixed 65,536-frame buffer and ProfilerRecorder overhead are included in both builds. Summaries use
nearest-rank percentiles, and do not average instantaneous FPS values. Frame capping can hide gains
at 60 FPS, so also compare slow frames and inspect the Profiler rather than reporting fabricated FPS gains.

Analyze exported runs with the standard-library-only helper:

```text
python Documentation/Benchmark/analyze.py <run-folder>
python Documentation/Benchmark/analyze.py <optimized-run-folder> <baseline-run-folder>
```

It checks CSV/summary consistency, shows 30-second workload windows, and refuses a headline
improvement when platform/protocol settings differ, a run is invalid, Editor data is involved, or
spawn/kill/mean-enemy workload changes by more than 5%. This threshold is a review flag, not proof
that smaller differences are harmless. Inspect per-window load and repeat across three runs.

## Device procedure

Keep resolution, quality, graphics API, target FPS, orientation, brightness, power-saving settings,
USB/power state and background apps consistent. Record battery and thermal observations outside
the measured interval. Let the device return to a comparable thermal state between runs. Use three
fresh-process runs per build. Avoid touching the display, notifications, changing focus or screen
recording during performance runs. Video capture is a separate presentation task.

Example commands (use the Unity SDK's adb executable):

```powershell
adb devices -l
adb install -r Builds/Benchmark/Skyloft-Benchmark.apk
adb shell am force-stop com.ooezd.skyloft.benchmark
adb shell monkey -p com.ooezd.skyloft.benchmark -c android.intent.category.LAUNCHER 1
adb pull /sdcard/Android/data/com.ooezd.skyloft.benchmark/files/Benchmarks Documentation/Benchmark/Results
```

Verify the actual persistent-data output path from `BENCHMARK START` in logcat if the device's path
differs. Do not clear app data to repeat runs. Keep raw results and build identity together; APKs
remain ignored by Git and should be distributed as release artifacts.

## Tonight's completion gate

- Runner checks, wave checks and a full real-time Editor integration run pass.
- Normal saved Game scene contains no runner; normal controls and kill-saving path remain intact.
- Benchmark Android APK builds and launches on the A50 without affecting the installed normal game.
- At least one device run validates output/behavior, then three comparable runs establish baseline.
- Capture a separate Profiler run for diagnosis if time allows; never label Editor stats as device data.
- Commit/tag the measured source and store measurements; optimize only after this gate.

If the device cannot connect tonight, stop at a validated APK and record that device baseline is
pending. Do not manufacture baseline numbers or treat the previous APK as an equivalent reference.

## Sources

- [Unity ProfilerRecorder](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/Unity.Profiling.ProfilerRecorder.html)
- [Build-only scripting defines](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/BuildPlayerOptions-extraScriptingDefines.html)
- [Profiling on target devices](https://unity.com/how-to/best-practices-for-profiling-game-performance)
