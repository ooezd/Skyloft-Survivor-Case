Unity has no folder named `Original`; the matching provided assets are in `Assets/Art Source`. This baseline covers that folder, inspected through Unity MCP and read-only Editor evaluation in Unity **6000.4.10f1**, with **Android** active.

No assets were modified, reimported, moved, renamed, or optimized. No scene was saved.

**Model baseline**

Paths below are relative to `Assets/Art Source/`.

| Model | Rig / avatar configuration | Read/Write | Mesh compression | Vertices | Triangles | Submeshes / material slots | Unique materials |
|---|---|---|---|---:|---:|---:|---:|
| `player.fbx` | Generic / NoAvatar | Off | Off | 12,586 | 19,450 | 3 / 3 | 2 |
| `enemy.fbx` | Generic / NoAvatar | Off | Off | 19,906 | 36,902 | 2 / 2 | 2 |
| `rifle.fbx` | Generic / NoAvatar | Off | Off | 1,718 | 1,988 | 2 / 2 | 1 |
| **Total** | | | | **34,210** | **58,340** | **7 / 7** | **5** |

Counts are from imported mesh assets, not source-file control points or scene instances. Triangles were retrieved from each triangle-topology submesh’s index count.

| Model / mesh | Renderer | Vertices | Triangles by submesh | Material slots, in order |
|---|---|---:|---|---|
| Player / `Soldier_body` | SkinnedMeshRenderer | 7,518 | 11,194 | `Soldier_body1` |
| Player / `Soldier_head` | SkinnedMeshRenderer | 5,068 | 1,820 + 6,436 | `Soldier_body1`, `Soldier_head6` |
| Enemy / `Ch30` | SkinnedMeshRenderer | 19,906 | 28,458 + 8,444 | `Ch30_Body1`, `Ch30_Body` |
| Rifle / `body` | MeshRenderer | 1,612 | 1,832 | `SAR80` |
| Rifle / `mag` | MeshRenderer | 106 | 156 | `SAR80` |

All three model importers share these settings:

| Setting | Current value |
|---|---|
| Scale / use file scale | 1 / enabled |
| Mesh optimization flags | Everything |
| Weld vertices | Enabled |
| Index format | Auto |
| Normals / tangents | Import / CalculateMikk |
| Blend-shape import | Enabled; all imported meshes have 0 blend shapes |
| Material import / location | ImportViaMaterialDescription / InPrefab |
| External material remaps | None |
| Optimize Game Objects | Disabled |
| Camera / light / visibility import | Enabled |
| Constraint import | Disabled |
| Generate secondary UV | Disabled |

**Rigs and animations**

The character skeletons are embedded in their FBXs. Neither character has a generated Avatar subasset or source Avatar assigned.

| Model | Skinning details |
|---|---|
| Player | 56 distinct referenced bones across both renderers. Body: 51 bones/bind poses, root `mixamorig:Hips`. Head: 7 bones/bind poses, root `mixamorig:Spine2`. |
| Enemy | 52 bones/bind poses, root `mixamorig:Hips`. |
| Rifle | No skinned renderers, referenced bones, or mesh bind poses. Importer still reports Generic. |

Animation import is **enabled on all three**. Shared configuration: `KeyframeReduction`, rotation/position/scale error values **0.5 / 0.5 / 0.5**, resample curves enabled, animated custom properties disabled, and wrap mode `Default`. There are no explicit custom clip configurations; the following are default takes and imported clips:

| FBX | Clip / take | Importer frame range | Imported duration | FPS | Loop time / pose | Curve bindings |
|---|---|---:|---:|---:|---|---:|
| `player.fbx` | `Take 001` | 0–100 | 0.033333 s | 30 | Off / Off | 1 |
| `player.fbx` | `mixamo.com` | 0–1 | 0.033333 s | 30 | Off / Off | 296 |
| `enemy.fbx` | `mixamo.com` | 0–1 | 0.033333 s | 30 | Off / Off | 355 |
| `rifle.fbx` | None | — | — | — | — | — |

The imported `Take 001` duration differs from its default take range. Its only curve is `Geo/Soldier_body → m_Enabled`, with keys at 0 and 0.033333 seconds.

All three imported clips are non-Legacy and report `humanMotion=false`. Unity also exposes three `__preview__` clips; these are excluded from the provided animation count. No standalone `.anim`, Avatar, or Animator Controller assets were found in the audited folder.

**Texture baseline**

All **17 textures** have the following settings, individually verified:

| Setting | Value for every texture |
|---|---|
| Max texture size | 2048 |
| Texture type / shape | Default / Texture2D |
| Mipmaps | Enabled |
| Streaming mipmaps | Disabled |
| Read/Write | Disabled |
| sRGB | Enabled |
| Compression | Compressed; quality 50; Crunch disabled |
| Default format | AutomaticCompressed |
| Android override | Disabled; inherits default settings |
| Android settings returned | Max 2048, AutomaticCompressed, Compressed, quality 50, Crunch off |
| Android ETC2 fallback | UseBuildSettings |
| Currently loaded format | ASTC_6x6 |
| Filtering / wrapping | Bilinear / Repeat |

The shared settings above apply to every row below. Paths are relative to `Assets/Art Source/`.

| Texture path | Source dimensions | Imported dimensions | Mip levels |
|---|---:|---:|---:|
| `player.fbm/Soldier_Body_diffuse.png` | 1024×1024 | 1024×1024 | 11 |
| `player.fbm/Soldier_Body_normal.png` | 512×512 | 512×512 | 10 |
| `player.fbm/Soldier_Body_specular.png` | 512×512 | 512×512 | 10 |
| `player.fbm/Soldier_head_diffuse.png` | 1024×1024 | 1024×1024 | 11 |
| `player.fbm/Soldier_head_normal.png` | 512×512 | 512×512 | 10 |
| `player.fbm/Soldier_head_specular.png` | 512×512 | 512×512 | 10 |
| `enemy.fbm/Ch30_1001_Diffuse.png` | 4096×4096 | 2048×2048 | 12 |
| `enemy.fbm/Ch30_1001_Glossiness.png` | 4096×4096 | 2048×2048 | 12 |
| `enemy.fbm/Ch30_1001_Normal.png` | 4096×4096 | 2048×2048 | 12 |
| `enemy.fbm/Ch30_1001_Specular.png` | 4096×4096 | 2048×2048 | 12 |
| `enemy.fbm/Ch30_1002_Diffuse.png` | 4096×4096 | 2048×2048 | 12 |
| `enemy.fbm/Ch30_1002_Glossiness.png` | 4096×4096 | 2048×2048 | 12 |
| `enemy.fbm/Ch30_1002_Normal.png` | 4096×4096 | 2048×2048 | 12 |
| `enemy.fbm/Ch30_1002_Specular.png` | 4096×4096 | 2048×2048 | 12 |
| `rifle_text/SAR_80_low_SAR80_AlbedoTransparency.png` | 2048×2048 | 2048×2048 | 12 |
| `rifle_text/SAR_80_low_SAR80_MetallicSmoothness.png` | 2048×2048 | 2048×2048 | 12 |
| `rifle_text/SAR_80_low_SAR80_Normal.png` | 2048×2048 | 2048×2048 | 12 |

The five files named as normal maps currently have texture type `Default`, not `NormalMap`; sRGB is enabled on these as well.

**Materials and shader dependencies**

There are **5 unique embedded materials using 1 unique shader: `Universal Render Pipeline/Lit`**. The characters account for 4 materials; the weapon uses 1. No standalone `.mat` files exist in this folder.

| Embedded material location | Base map | Normal map | Smoothness |
|---|---|---|---:|
| `player.fbx → Soldier_body1` | `Soldier_Body_diffuse.png` | `Soldier_Body_normal.png` | 0.251233 |
| `player.fbx → Soldier_head6` | `Soldier_head_diffuse.png` | `Soldier_head_normal.png` | 0.251233 |
| `enemy.fbx → Ch30_Body1` | `Ch30_1002_Diffuse.png` | `Ch30_1002_Normal.png` | 0.447214 |
| `enemy.fbx → Ch30_Body` | `Ch30_1001_Diffuse.png` | `Ch30_1001_Normal.png` | 0.447214 |
| `rifle.fbx → SAR80` | Unassigned | Unassigned | 0.309965 |

All five materials use opaque surfaces, render queue 2000, metallic workflow, metallic value 0, and alpha clipping disabled. The four character materials enable `_NORMALMAP`; the rifle material has no enabled keywords.

Eight unique textures are assigned across these materials. The two player specular maps, four enemy specular/glossiness maps, and three rifle textures are present but unassigned in the inspected materials. These are baseline observations only; no optimization decisions have been made.