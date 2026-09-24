# Optimization experiments — 2026-09-24

The `baseline-gameplay` tag and its three A50 runs are the unoptimized reference. Each candidate
below must keep the Hard route, 180-second match, seed, enemy cap, quality and device settings.
Diagnostic runs may use a Profiler connection; final performance comparisons use the unattached
three-run protocol. Source fingerprints identify each APK.

## Candidate 1: skinned enemy LODs

Baseline enemy: 36,902 triangles, 19,906 vertices, 52 bones, two material sections, one skinned
renderer. At 100 living enemies, the source mesh alone represents up to 3.69 million enemy triangles
before frustum culling, LOD selection, shadows and passes.

`EnemyArtOptimizer.Rebuild()` generated two skinned mesh assets with the original bone weights,
bind poses, two submeshes and two materials retained:

| Level | Screen threshold | Triangles | Vertices | Reduction vs original |
| --- | ---: | ---: | ---: | ---: |
| LOD0 | 0.110 | 36,902 | 19,906 | 0% |
| LOD1 | 0.055 | 12,916 | 8,336 | 65.0% |
| LOD2 | 0.010 | 5,534 | 4,190 | 85.0% |

Source FBX and original texture bytes remain unchanged. The generator temporarily enables readable
mesh import, then restores the exact original `.meta` bytes; Git diff verified no source importer
change. In Editor, forced-LOD camera captures at normal and enlarged framing showed consistent
silhouettes. This is a basic visual check, not a final device quality sign-off. The second capture
uses 20-degree FOV only for inspection; the saved game camera remained at 50 degrees. No QA objects
were saved to the scene. The LOD meshes retain 52 bones, so animation processing does not disappear.

Tooling: an embedded, Editor-only copy of [UnityMeshSimplifier](https://github.com/Whinarn/UnityMeshSimplifier)
3.1.1, revision `53fdb31`, MIT licensed. The package license and third-party notice are included.
Its code is used to generate `.asset` meshes and is excluded from Android builds.

First unattached A50 diagnostic run (`20260924-135323-574`) completed with the expected source
fingerprint. AP 32.2 → 50.0 °C; battery 28.8 → 29.9 °C; thermal status 0 before/after.
Mean 88.96 ms and p95 120.87 ms versus baseline run means 100.77–105.69 ms and p95s
148.12–156.11 ms. Peak enemies 100, kills 127, spawns 227, mean enemies 73.58 (baseline
74.36–75.10). This is a promising single-run result, not the final three-run improvement claim.
Peak total-used-memory recorder value was 407.0 MB versus baseline 376.0–376.4 MB: extra LOD
renderers/meshes carry a memory cost, and this metric needs repeated confirmation. The profiler
was not attached. This experiment changed geometry/LOD only; textures, materials and shadows
remained unchanged. The exact tested APK is preserved under `Builds/Optimization/LOD/` locally.
After the build, whitespace in generated Unity asset text and vendored metadata was normalized for
Git; the commit therefore does not byte-match the run source fingerprint. Mesh values, prefab
configuration and gameplay code were unchanged. The APK SHA256 in `lod-build.json` is the binary
identity for this diagnostic result.

## Next candidate gates

- Texture import: each enemy material uses a 2048x2048 diffuse and normal map; Android currently
  inherits the 2048 maximum. Try 1024 on Android only after the LOD run, measure texture memory and
  inspect the A50 screen. These four textures are shared across all enemies, so memory saving is not
  multiplied by the number of enemies.
- Materials: two texture sets require an atlas or other shader change to combine without altering
  appearance. Check render counters before spending time on an atlas. Preserve the two original
  materials and explain a decision to defer if the draw-call gain is small.
- Shadows: the active Mobile pipeline has a 4096 main-light shadow atlas, soft shadows and 50-unit
  shadow distance. Evaluate separately from LODs, and check shadow quality at actual game scale.
- CPU: the diagnostic Profiler showed EnemyController.Update rising from about 1.2 to 12.5 ms as
  population grows. Isolate separation before changing data structures. Pooling is conditional on
  lifecycle/GC evidence and must reset every spawned object's state correctly.
