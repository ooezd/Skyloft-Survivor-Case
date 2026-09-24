# AI-assisted benchmark decisions (draft for developer review)

These are records of this benchmark task, not claims that the developer personally approved every
implementation detail. The developer should review the decisions before using them in the case's
final three-decision AI work log. No credentials or device serial number are included here.

## 1. Scripted input versus a deterministic simulation rewrite

**User context:** The gameplay was largely complete, delivery was due soon, and the developer
suggested an automatically played device scenario to make before/after conditions deterministic.
The reference device was identified as a Samsung Galaxy A50.

**AI proposal and choice:** Use the existing gameplay with a fixed input route and a separate seeded
spawn RNG. Do not rewrite all delta-time movement, targeting and wave scheduling as a fixed-tick
simulation just to claim determinism. Disable random upgrades and prevent player death while keeping
contact hits, feedback, enemy deaths, attacks and normal spawn/lifetime costs.

**Limitation retained:** This controls inputs, not exact simulation state. Log actual enemy counts,
kills, spawns and position and inspect workload parity before accepting timing comparisons.

**Verification:** Route/health/percentile checks passed; a 180-second Editor integration completed;
the initial two device validations both reported 223 spawns and 124 kills, with approximately
105-106 ms mean frames. Those early runs are not the final accepted baseline identity group.

## 2. Isolate the measurement path and retain raw evidence

**Problem:** Automatic tests should neither overwrite the installed normal game nor contaminate
lifetime kills, and measurement code should not allocate strings or write files every frame.

**AI implementation:** A separate package ID; build-only benchmark define; guarded input/health/
spawn hooks; preallocated samples; raw CSV and JSON written after the run. The first 15 seconds are
retained but excluded from aggregate steady-state statistics. Unavailable counters are explicitly
marked instead of silently reported as zero.

**Verification:** The normal game's package/product settings and saved scene were inspected via
Unity MCP; the benchmark APK's manifest was checked and installed separately. Editor lifetime
kills stayed at 903. The analyzer checked raw/summary counts, timestamp ordering and p95.

**Correction:** Restoring PlayerSettings in memory was insufficient after BuildPipeline flushed
temporary values to disk. The helper now restores preloaded assets too and explicitly saves the
restored settings. A subsequent Git diff showed no ProjectSettings changes.

## 3. Reject a superficially successful build with stale identity

**Problem found during verification:** Unity reported a successful APK build and the new build
manifest contained fingerprint `3ba43e...`, but the newly installed APK reported `9f49ea...` in its
device result. A successful build alone was not proof of the intended processed scene content.

**AI mistake and correction:** The initial scene-injection design did not account for Unity's
incremental processed-scene cache. The AI checked the Unity build-callback documentation, retained
the conflicting run as rejected evidence, enabled CleanBuildCache and added a check that scene
injection actually ran. The normal final APK also requires a clean build after a benchmark build.

**Verification:** The clean APK and working-tree source fingerprint matched all three completed A50
runs. Raw-data checks passed. See RESULTS.md for accepted identity, timings and thermal limitations.

Reference: [Unity incremental build callbacks](https://docs.unity3d.com/ja/current/Manual/build-callbacks.html).
