# Rejected incremental build identity (kept for audit)

**Do not use this APK as the final baseline.** Device validation found that its injected scene
still reported the preceding `9f49...` fingerprint. Unity reused cached processed scene data even
though the build helper produced a new manifest. This led to the subsequent CleanBuildCache fix
and an explicit assertion that scene injection ran. The `20260923-205051-894` device run is retained
as rejected identity evidence, not part of the final three-run group.

- Protocol: `hard-route-v1`.
- Unity: `6000.4.10f1`; Android ARM64; Development build.
- Package: `com.ooezd.skyloft.benchmark` (separate from the installed normal game).
- APK: `Builds/Benchmark/Skyloft-Benchmark.apk` (ignored by Git).
- APK SHA256: `2383efc99dfa0371f862d54b54a6299d3d2e2083d8a132d1490178518fa7e0e3`.
- Source fingerprint: `3ba43e7f0889045a61b7d4d35d4302f38c16be512fa76a353f33cf05e17dfda5`.
- Build completed: `2026-09-23T20:48:45Z`.
- Base gameplay commit: `467e062`, plus the benchmark instrumentation in this checkpoint.
- Build result: Succeeded. The sole logged error in the BuildReport was an MCP request timeout
  (`Main thread operation timed out after 5000ms`) while Unity was busy building. There was no
  Android compilation/packaging error in the report. Do not report this as a zero-error log.

The then-current Assets/Packages/ProjectSettings files were independently hashed after the build and
matched the embedded source fingerprint above. The temporary package/product/preloaded-asset
settings were restored both in Editor memory and on disk; ProjectSettings had no remaining diff.

The earlier `20260923-204602-866` run is a device smoke test built from fingerprint
`9f49ea5cb604ec65dc66afb4bf1548ef1a65aaa5fd39c31686c093bd34bae268`. Its runtime protocol is the same,
but the build helper's async scheduling/settings restoration were subsequently corrected. Keep both
early device runs separate from the final baseline group, whose identity will be recorded in RESULTS.md.

Rebuilding can produce a different APK hash even from the same source. Use the source fingerprint,
Git checkpoint, protocol, runtime configuration and per-run metadata together to identify a run.
