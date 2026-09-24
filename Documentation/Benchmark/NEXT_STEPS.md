# After the measured baseline

The delivery target discussed on September 23 is completion by the evening of **September 24**,
with submission on September 25. Midnight does not shift that deadline.

## September 24 priorities

1. Capture a **separate diagnostic Unity Profiler session on the A50**, using the same Hard route.
   Inspect low density (15-30s), burst/ramp (60-90s), and high density (120-180s). Keep Deep Profiling
   off initially. Separate useful CPU work from frame limiting / present waits. The existing raw
   Main Thread metric contains waits and is not enough to label a CPU bottleneck. GPU time and
   rendering counters are unavailable in the current runner; do not invent them.
2. Rank measured costs across rendering/skinning/Animator, enemy movement/separation, attacks,
   object lifetime and allocations. Inspect both slow frames and steady high-density frames.
3. Make one bounded change group at a time, then repeat the same protocol and check load parity.
   Preserve player/enemy/weapon recognition, required animations and the original source assets.
4. After demonstrated improvements, run normal touch gameplay, win/loss/replay, lifetime-kill
   persistence and all three difficulty smoke checks on the device. Benchmark invulnerability and
   disabled pickups are not proof of normal balance or correctness.
5. Reserve the final evening block for README, before/after evidence, three AI decisions, the
   3-5 minute gameplay/MCP video, a clean normal APK, fresh-clone verification and final Git tag.

## Candidates, not confirmed bottlenecks

- Current enemy: 36,902 triangles, 52 bones, two material slots. At 100 enemies this is 3,690,200
  enemy triangles before visibility, passes and shadows are considered. Inspect the actual device
  rendering/animation cost before choosing geometry, skinning, texture or shader changes.
- Separation walks the entire active list from every active enemy each frame (quadratic work).
  A spatial grid is a candidate only if movement is a significant CPU cost. The current interface
  enumeration is also an allocation candidate to inspect with allocation call stacks.
- Enemy/projectile/VFX/damage-number Instantiate/Destroy is a candidate for pooling. Reuse must
  reset health, death/spawn coroutines, colliders, animator layers/update mode, timers, materials,
  transforms, trail/particles and subscriptions. A projectile must not damage a newly reused enemy
  through a stale target reference; use lifetime identity or invalidate outstanding targets.
- HUD already avoids rebuilding unchanged strings; targeting already runs at an interval. Do not
  spend the limited schedule on cosmetic micro-refactors ahead of measured large costs.

Do not lower enemy counts, change the route, remove gameplay feedback or change the FPS target
and call the resulting frame-rate difference an optimization under identical conditions. If quality
settings intentionally change, document the visual/performance tradeoff and keep other load fixed.

## Evidence and build caveats

Three accepted device runs measured 100.77-105.69 ms mean frames under the scripted Hard workload.
They establish the unoptimized baseline, not the dominant cause. See RESULTS.md for the source
identity, raw evidence, workload parity and thermal limitations.

Benchmark builds inject a runner into processed scene data. Always use the provided clean benchmark
build helper, and use a **Clean Build for the subsequent normal APK** to remove cached processed
benchmark scene data. Verify the on-device source fingerprint against the build manifest before
accepting measurements. A successful build alone did not catch the previously observed stale scene
identity; the read -> build -> device verification loop did.

Source references: [Unity build callbacks and incremental caching](https://docs.unity3d.com/ja/current/Manual/build-callbacks.html),
[target-device profiling guidance](https://unity.com/how-to/best-practices-for-profiling-game-performance).
