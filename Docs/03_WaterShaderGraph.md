# 3. Shader Graph Recipes

All gameplay-relevant water math lives in `Assets/MoonPull/Art/Shaders/MoonPullWater.hlsl` and mirrors `WaveMath.cs`. The graph only calls it, so the boat always sits on the rendered surface.

## 3.1 Water mesh

1. Blender (or ProBuilder): plane **60 × 12 units**, **120 × 24 quads** (0.5 unit spacing = `WaterConfig.FollowSnap`).
2. Triangulate, export FBX, import with **Read/Write off**, **Normals: Calculate**, **Mesh Compression: Medium**.
3. Place at `(0, 0, 4)`, so the front edge sits just in front of the boat lane (z = 0) and the far edge fades into fog.

## 3.2 `SG_Water` (URP Lit Shader Graph)

Graph Settings: **Universal**, Material **Lit**, Surface **Transparent**, Blend **Alpha**, Workflow **Specular**, **Depth Write: Force Enabled** (so rocks sort correctly), **Receive Shadows off**.

### Blackboard properties
| Name | Reference | Type | Default |
|---|---|---|---|
| Shallow Color | `_ShallowColor` | Color | (0.45, 0.85, 0.85, 0.85) |
| Deep Color | `_DeepColor` | Color | (0.10, 0.25, 0.45, 0.95) |
| Foam Color | `_FoamColor` | Color | (0.95, 0.95, 1, 1) |
| Depth Fade | `_DepthFade` | Float | 2.5 |
| Foam Distance | `_FoamDistance` | Float | 0.35 |
| Foam Noise Scale | `_FoamNoiseScale` | Float | 6 |
| Full Moon Tint | `_FullMoonTint` | Color | (0.85, 0.9, 1, 1) |
| Smoothness | `_Smoothness` | Float | 0.85 |

Leave `_MP_*` globals **off** the blackboard; the HLSL include declares them and `WaterSurface.cs` sets them.

### Vertex stage
1. **Position** node (Space: World) → `WorldPos`.
2. **Custom Function** node: Type **File**, Source `MoonPullWater.hlsl`, Name `MoonPullWave`. Inputs: `WorldPos` (Vector3). Outputs: `Height` (Float), `Normal` (Vector3).
3. **Split** WorldPos → recombine with **Vector3**(R, `Height`, B) → **Transform** (World → Object, Position) → **Vertex Position**.
4. `Normal` → **Transform** (World → Object, Direction) → **Vertex Normal**.

### Fragment stage
1. **Scene Depth** (Eye) minus **Screen Position** (Raw).w → `waterDepth`. Enable **Depth Texture** on the URP asset.
2. `saturate(waterDepth / _DepthFade)` → **Lerp**(Shallow Color, Deep Color) → `baseColor`.
3. Foam: `1 - saturate(waterDepth / _FoamDistance)` × **Step**(0.45, **Gradient Noise**(UV × `_FoamNoiseScale` + Time × 0.15)) → `foamMask`.
4. **Lerp**(baseColor, Foam Color, foamMask) → `color`.
5. Full Moon: second Custom Function, Name `MoonPullWaterLevel` (outputs `Level`, `FullMoon`). **Lerp**(color, color × `_FullMoonTint` × 1.6, `FullMoon`) → **Base Color**.
6. **Emission** = `foamMask` × 0.25 + `FullMoon` × 0.35 × Full Moon Tint (silver glow that survives low exposure).
7. **Alpha** = Lerp(Shallow.a, Deep.a, depth factor) + foamMask (saturate).
8. **Smoothness** = `_Smoothness`, **Specular** = (0.2, 0.2, 0.2).

### Low-poly look
Add **DDX/DDY** of World Position → **Cross Product** → **Normalize** → **Normal (World)** input. This gives flat-shaded facets without splitting mesh vertices.

### Material
`M_Water` from `SG_Water`. `WaterSurface.ApplyPalette` overrides the three colors per region through a MaterialPropertyBlock, which keeps the SRP Batcher active.

## 3.3 `SG_SkyGradient` (Unlit, used by a Skybox material)
1. Custom property-less graph: **Custom Function** (String mode) `Out = lerp(_MP_SkyBottom, _MP_SkyTop, saturate(In * 1.4 + 0.3));`. Declare `float4 _MP_SkyTop; float4 _MP_SkyBottom;` in the string body header.
2. Input: **View Direction** (World) → Normalize → Split.G.
3. Add stars: **Voronoi** (Cell Density 120) → **Step** 0.03 → × saturate(G) × 0.8 → add to Base Color.
4. Material `M_SkyGradient` → Lighting window → Environment → **Skybox Material**. `RegionAmbience` drives the colors.

## 3.4 `SG_Moon` (Unlit)
Property `_Brightness` (Float, default 1). Base Color = (0.95, 0.95, 1) × `_Brightness`, Emission = same × 1.5. Put a **Fresnel** (power 3) rim × 0.4 on top. `MoonView` sets `_Brightness` via MaterialPropertyBlock (0.08 eclipse, 1.8 Full Moon).

## 3.5 Optional: wet line on rocks (`SG_Rock`)
Lit graph. Custom Function `MoonPullWaterLevel` → `Level`. `mask = smoothstep(Level + 0.15, Level - 0.05, WorldPos.y)` → Lerp(rock albedo, albedo × 0.55, mask) and Smoothness + 0.5 × mask. Rocks visibly darken where the tide touches them, which helps players read heights.

## 3.6 Performance checklist
- Water: **1 draw call**, ~5.8k verts; no Refraction/Opaque Texture (expensive on Mali/Adreno 6xx).
- URP asset: HDR off, MSAA 2x, Depth Texture on, Opaque Texture off, Shadows: 1 cascade, 25 m distance, Soft Shadows off.
- Fog: use the built-in (RenderSettings) fog that `WeatherSystem` drives; every URP Lit graph supports it without extra passes.
