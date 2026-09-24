# Functional validation, 2026-09-23

This directory contains **Editor functional evidence**, not an Android baseline or optimization claim.

- Unity 6000.4.10f1; Game scene; real-time (timeScale 1), 180-second Hard scripted run.
- `BenchmarkChecks.Run()`: 11 checks passed (route boundaries, ideal route closure, percentile edge
  case, benchmark lethal-hit feedback, and normal death when protection is disabled).
- `WaveSystemChecks.Run(...)`: 29 checks passed.
- Completed run: Won, remaining time zero, 4,951 sampled frames; 4,330 after the 15-second warmup.
- 235 enemies spawned, 136 kills; peak active population 100. Player remained alive at health 1.
- Upgrade spawner disabled. Lifetime kill preference stayed at **903** before/during/after the run.
- Python analyzer accepted timestamp monotonicity, raw/summary frame counts, warmup counts and p95.
- After stopping Play Mode, the saved Game scene had no BenchmarkRunner.
- A first MCP call immediately after entering Play Mode timed out without adding the runner; a
  read confirmed no runner, and the retry succeeded. The Editor did not advance while unfocused
  (`Application.runInBackground=false`); this was temporarily enabled for the functional run and
  restored to false afterward. Neither condition is a gameplay performance finding.

`editor-integration-summary.json` archives the full-run summary before the later metadata-only
addition of difficulty, enemy cap and wave-plan JSON. It is not an eligible input for baseline/final
comparison. Those fields do not alter the gameplay or sampling loop; checks passed again after
their addition. Android validation remains a separate gate.

On September 24, `device-paused-summary.json` was retrieved from a pre-existing device run. It
records `invalid-paused` at 118.31 seconds, confirming that interruption is not reported as a
completed run. Its earlier source identity excludes it from the accepted baseline group.

GC and memory values in this Editor capture include Editor/tooling overhead. Render counters were
unavailable and explicitly marked as such. Do not copy these timing/GC values into a device report.
