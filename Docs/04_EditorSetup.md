# 4. Unity Editor Setup

One scene (`Main.unity`). Boot, consent, menu and gameplay share it: no scene loads, no duplicated services, and a faster cold start.

## 4.1 Project & packages
1. Unity Hub → **Unity 2022.3 LTS** (latest patch) → open this repo folder. `Packages/manifest.json` pulls URP, Localization, TMP, IAP, iOS Support, Test Framework.
2. Window → TextMeshPro → **Import TMP Essential Resources**.
3. Asset Store → **DOTween (free)** → import → *Tools → Demigiant → DOTween Utility Panel → Setup DOTween*.
4. Project Settings → Player → Other → **Scripting Define Symbols**: add `MOONPULL_DOTWEEN` (Android and iOS). Without it, UI still works but without animation.
5. Google Mobile Ads + Firebase: see `05_AdMobSetup.md` and `06_FirebaseSetup.md`. If you import them as `.unitypackage` instead of UPM, also add `MOONPULL_ADMOB`, `MOONPULL_FIREBASE_ANALYTICS`, `MOONPULL_FIREBASE_CRASHLYTICS`, `MOONPULL_FIREBASE_REMOTE_CONFIG` manually. UPM installs set these automatically through `versionDefines`.
6. Project Settings → Player → **Active Input Handling: Both** (or Input Manager (Old)).

## 4.2 Render pipeline
1. Create → Rendering → URP Asset (with Universal Renderer) → `URP_Mobile`.
2. Settings: HDR off, MSAA 2x, Render Scale 1, **Depth Texture on**, Opaque Texture off, Main Light shadows 1024, 1 cascade, 25 m, Additional Lights: Per Vertex, 2 max, Soft Shadows off.
3. Project Settings → Graphics and Quality: assign `URP_Mobile`, delete every quality level except one named `Mobile`.
4. Build the shaders from `03_WaterShaderGraph.md`.

## 4.3 ScriptableObject assets
Create in `Assets/MoonPull/ScriptableObjects/` via *Create → MoonPull → …*. Defaults are the designed values; only the fields listed below need input.

| Asset | Must set |
|---|---|
| `GameConfig`, `MoonConfig`, `TideConfig`, `WaterConfig`, `SeabedConfig`, `BoatConfig`, `CameraConfig`, `ScoreConfig`, `NearMissConfig`, `WaveLaunchConfig`, `FullMoonConfig`, `PickupConfig`, `LevelRunnerConfig`, `PassengerConfig`, `WeatherConfig`, `CreatureConfig`, `BossConfig`, `RewindConfig`, `FeedbackConfig`, `LevelGenConfig`, `EconomyConfig`, `IapConfig` | nothing (defaults) |
| `AdConfig` | production unit IDs (see 05), test device IDs |
| `PoolWarmupConfig` | table 4.6 |
| `SfxLibrary` | one entry per `SfxId`, plus menu music, Full Moon layer, wave loop |
| `PlacementPrefabSet` | every non-obstacle prefab plus fallback obstacles (4.6) |
| `Region_*` ×5 + `RegionCatalog` | table 4.4 |
| `Boat_*` ×20 + `BoatCatalog` | table 4.5, default boat = `boat_dinghy` |
| `Mission_*` ×9 + `MissionCatalog` | table 4.7 |

### 4.4 Regions
| # | id | nameKey | starsToUnlock | Signature (kind) | Idle coins/h |
|---|---|---|---|---|---|
| 1 | tropical_lagoon | region.tropical_lagoon | 0 | Coral Reef (LowObstacle) | 60 |
| 2 | frozen_north | region.frozen_north | 35 | Icicle Cave (HighObstacle) | 90 |
| 3 | volcanic_isles | region.volcanic_isles | 80 | Lava Spire (LowObstacle) | 130 |
| 4 | sunken_city | region.sunken_city | 130 | Ruined Aqueduct (HighObstacle) | 180 |
| 5 | midnight_sea | region.midnight_sea | 185 | Ghost Wreck (LowObstacle) | 240 |

Lighthouse stage costs: region 1 keeps the defaults `200 … 2600`; multiply by 1.75 / 2.5 / 3.25 / 4.0 for regions 2–5. Palettes use a pastel night sky (top darker than bottom); water shallow color is always the lightest value in the region so depth reads instantly.

### 4.5 Boats
| id | nameKey | Rarity | Price | Perk | Value |
|---|---|---|---|---|---|
| boat_dinghy | boat.dinghy.name | Common | 0 | None | 0 |
| boat_skiff | boat.skiff.name | Common | 500 | CoinBonusPercent | 10 |
| boat_fisher | boat.fisher.name | Common | 800 | PassengerBonusPercent | 15 |
| boat_ducky | boat.ducky.name | Common | 1200 | PickupRadiusBonus | 0.3 |
| boat_lantern | boat.lantern.name | Common | 1600 | FullMoonBonusSeconds | 1 |
| boat_catamaran | boat.catamaran.name | Rare | 2500 | LaunchBonusPercent | 10 |
| boat_tug | boat.tug.name | Rare | 3500 | CrashShields | 1 |
| boat_moonrunner | boat.moonrunner.name | Rare | IAP only | StartingMoonstones | 1 |
| boat_pearl | boat.pearl.name | Rare | 5000 | CoinBonusPercent | 20 |
| boat_viking | boat.viking.name | Rare | 6500 | NearMissWindowPercent | 15 |
| boat_gondola | boat.gondola.name | Epic | 8000 | PassengerBonusPercent | 25 |
| boat_icebreaker | boat.icebreaker.name | Epic | 10000 | LaunchBonusPercent | 20 |
| boat_ember | boat.ember.name | Epic | 12000 | FullMoonBonusSeconds | 2 |
| boat_sub | boat.sub.name | Epic | 15000 | IdleIncomeBonusPercent | 25 |
| boat_pirate | boat.pirate.name | Epic | 18000 | CoinBonusPercent | 30 |
| boat_swan | boat.swan.name | Epic | 21000 | NearMissWindowPercent | 25 |
| boat_steamer | boat.steamer.name | Legendary | 25000 | IdleIncomeBonusPercent | 50 |
| boat_ghost | boat.ghost.name | Legendary | 30000 | NearMissWindowPercent | 35 |
| boat_starliner | boat.starliner.name | Legendary | 35000 | StartingMoonstones | 2 |
| boat_leviathan | boat.leviathan.name | Legendary | 45000 | CrashShields | 2 |

`boat_moonrunner`: tick **Iap Only**; its id must equal `IapConfig.StarterPackBoatId`.

### 4.6 Prefabs
Every placement prefab: root has **PooledObject** + the view script, a `Visual` child assigned to *Visual Root*, optional `Burst` ParticleSystem (Play On Awake off).

| Prefab | View script | Pivot | Notes | Warmup |
|---|---|---|---|---|
| Rock_A/B/C (low) | PlacementView | **top** centre | mesh extends to y = −6; *Authored Width* 1.8; spiky/triangular silhouette + up-chevrons | 6 each |
| Bridge_A/B/C (high) | PlacementView | **bottom** centre | extends to y = +8; flat underside + hanging fringe + down-chevrons | 6 each |
| Signature ×5 | PlacementView | per kind | shape language same as its kind | 3 each |
| Star / Coin / Moonstone | PlacementView | centre | Animator spin loop | 20 / 24 / 7 |
| Chest | ChestView | bottom (sits on bed) | `RevealSparkle` child | 2 |
| Sandbar | PlacementView | top centre | *Authored Width* 5 | 2 |
| Dock | DockView | deck surface | 3 passenger figures | 2 |
| Dolphin / Whale / Shark / Kraken | CreatureView | water line | Animator: bool `Engaged`, trigger `Trigger` | 2 / 2 / 2 / 3 |
| Lighthouse | LighthouseBeamView | base | child Spot Light, 45° | 3 |
| Harbor | PlacementView | water line | pier + flags | 1 |
| BoatDebris | BoatDebris | centre | 8 plank children, Rigidbody (mass 0.2, kinematic), **no colliders** | 1 |

Colorblind rule: low obstacles are always pointed and upward-facing, high obstacles always flat-bottomed and hanging. Never rely on color alone.

Boat models (`BoatDefinition.ModelPrefab`): pivot at the waterline, bow facing +X, hull ≈ 1.6 × 0.6, mast top at +1.8 (matches `BoatConfig` boxes). No colliders.

### 4.7 Missions
| id | type | target | reward | minLevelIndex | descriptionKey |
|---|---|---|---|---|---|
| m_near_5 | NearMisses | 5 | 80 | 0 | mission.near_misses |
| m_near_12 | NearMisses | 12 | 150 | 5 | mission.near_misses |
| m_levels_3 | CompleteLevels | 3 | 120 | 0 | mission.complete_levels |
| m_stars_15 | CollectStars | 15 | 100 | 0 | mission.collect_stars |
| m_fullmoon_2 | FullMoons | 2 | 120 | 1 | mission.full_moons |
| m_launch_5 | WaveLaunches | 5 | 100 | 2 | mission.wave_launches |
| m_chests_1 | FindChests | 1 | 100 | 3 | mission.find_chests |
| m_passengers_2 | DeliverPassengers | 2 | 100 | 6 | mission.deliver_passengers |
| m_threestar_2 | ThreeStarLevels | 2 | 150 | 2 | mission.three_star |

## 4.8 Localization
1. Window → Asset Management → Localization Tables → *Create Localization Settings*.
2. Locale Generator: add `en, tr, es, pt-BR, de, fr, ru, ja`.
3. Locale Selectors order: **Specific Locale Selector (en)** last; keep **System Locale Selector** first.
4. New **String Table Collection** named `UI` (exact name, used by `UnityLocalizationService.Table`). Enable **Preload All Tables**.
5. Collection → Extensions → **CSV Extension** → *Add Default Columns* → Import `Assets/MoonPull/Localization/MoonPull_UI.csv`. Headers already match the default column names (`English(en)`, …).
6. Fonts: TMP SDF from **Nunito** (Latin Extended + Cyrillic, static atlas 2048). Add **Noto Sans JP** as a *Dynamic* fallback font asset (keeps the build small). Assign the fallback in the primary font asset.
7. To add or change strings, edit `Tools/generate_localization.py`, run it, and re-import the CSV. It also regenerates `LocKeys.cs`.

## 4.9 Scene hierarchy (`Assets/MoonPull/Scenes/Main.unity`)
```
[Systems]
  GameManager        GameManager, AppLifecycle, TimeScaleController
  Boot               Bootstrapper + boot steps (4.10)
  Meta               MetaGame
  Ads                AdsCoordinator, MockAdOverlay
  Audio              AudioService
    Music            2× AudioSource (loop), AudioLowPassFilter
    MusicLayer       AudioSource (loop)
    Sfx              8× AudioSource
    Waves            AudioSource (loop), DynamicWaveAudio
  Analytics          AnalyticsEventRouter
  Feedback           GameFeedbackDirector
  Pools              PoolManager
[Gameplay]
  Simulation         SimulationLoop
  Moon               MoonController
  Tide               TideModel
  Water              WaterSurface, Seabed
    WaterMesh        MeshFilter, MeshRenderer (M_Water)
  Boat               BoatController, BoatView
    ModelRoot
  Level              LevelSession, LevelRunner, ObstacleInteractionSystem, PickupSystem,
                     WaveLauncher, FullMoonMode, ScoreSystem, RewindController
  Layers             PassengerSystem, WeatherSystem, CreatureSystem, KrakenBoss
  Visuals            RegionAmbience
    LighthouseIsland LighthouseIslandView (+ StageRoot child), placed left of x = 0 for the menu shot
  Tutorial           TutorialController
Main Camera          Camera (Perspective, FOV 40), CameraRig
  MoonAnchor         local (−1.2, 0, 14)
    Moon             MoonView, MeshRenderer (SG_Moon), TrailRenderer, Point Light
Directional Light    moonlight, soft blue, intensity 0.6
EventSystem
UI                   Canvas (Overlay), CanvasScaler (1080×1920, match 0.5), UiRouter, PopupManager
  SafeArea           SafeAreaFitter
    BannerSafe       BannerSafeArea (stretch)  ← all screens and popups inside
      Loading, Menu, Hud, Fail, Win, Shop          (UIScreen subclasses, CanvasGroup each)
      Popups: IdleIncome, LoginStreak, DailySpin, Missions, Settings, Lighthouse, BossChest, Pause
    TopBar           StateVisibility, CoinCounter
    HandHint         HandHint (CanvasGroup + Hand image), anchored right of the moon
  CoinFly            CoinFlyEffect (+12 coin Images)
  Toast              Toast (CanvasGroup + label)
  DebugPanel         DebugPanel
```

### Key references
| Component | Wire |
|---|---|
| SimulationLoop.tickables (order matters) | WeatherSystem, MoonController, TideModel, WaterSurface, BoatController, LevelRunner, ObstacleInteractionSystem, PickupSystem, CreatureSystem, KrakenBoss, WaveLauncher, FullMoonMode, PassengerSystem, LevelSession, RewindController |
| WaterSurface | TideModel, Follow = Main Camera, Surface Mesh/Renderer = WaterMesh |
| BoatController | WaterSurface, Seabed |
| BoatView | BoatController, ModelRoot, PoolManager, BoatDebris prefab |
| LevelSession | all configs + catalogs, every gameplay system above, CameraRig |
| RewindController | SimulationLoop, Moon, Tide, Water, Boat, FullMoon, Kraken, Runner, ObstacleInteraction, CameraRig |
| AdsCoordinator | AdConfig, GameManager, TimeScaleController |
| RewardedButton (each) | AdsCoordinator + placement (Fail: RewindTide, Win: TripleReward (Pulse on), Idle: DoubleIdle, Spin: ExtraSpin, Chest: OpenBossChest). BoatCard buttons get it at runtime. |
| GameFeedbackDirector | FeedbackConfig, NearMissConfig, AudioService, TimeScaleController, CameraRig, BoatController, LevelSession |
| UiRouter | the six screens + PopupManager |
| Screens/Popups | fields are named after what they need; every popup needs PopupManager + its Close button |

Layout rule: the Win screen's x3 button sits **above** the reward, Continue/Home at the bottom inside `BannerSafe`, ≥ 150 px apart. The banner only shows on Menu/Shop and `BannerSafe` lifts content above it.

## 4.10 Boot steps (components on `Boot`, drag into `Bootstrapper.steps` in this order)
| # | Step | Timeout (s) | Runs In Consent State |
|---|---|---|---|
| 1 | FirebaseBootStep | 4 | no |
| 2 | SaveBootStep | 5 | no |
| 3 | LocalizationAudioBootStep | 5 | no |
| 4 | MetaBootStep | 2 | no |
| 5 | ConsentBootStep | **0** (waits for user) | **yes** |
| 6 | AnalyticsConsentBootStep | 2 | no |
| 7 | AdsBootStep | 6 | no |
| 8 | IapBootStep | 8 | no |
| 9 | AnalyticsBindStep | 1 | no |
| 10 | RemoteConfigApplyStep | 1 | no |

## 4.11 Build settings
| Setting | Android | iOS |
|---|---|---|
| Scripting backend | IL2CPP | IL2CPP |
| Architectures | **ARM64 only** | ARM64 |
| Min version | API 24 (Android 7.0) | iOS 13.0 |
| Target | the API level Play Console currently requires for new apps and updates (API 35 since Aug 2025; check the console for the 2026 deadline) | latest Xcode SDK |
| Output | **Build App Bundle (.aab)**, Split Application Binary off (< 150 MB) | Xcode project → Archive |
| Graphics APIs | Vulkan, OpenGLES3 | Metal |
| Texture compression | **ASTC** (6×6 default, 4×4 for UI) | ASTC |
| Managed stripping | **Medium** (`link.xml` preserves IAP tangles) | Medium |
| Other | Optimized Frame Pacing on, Custom Main Manifest (06), keystore set, Minify Release (R8) on with Firebase/GMA proguard rules auto-added | `NSUserTrackingUsageDescription` via Google Mobile Ads settings |

Orientation: **Portrait** only (disable autorotation). Splash: background `#0B1026`, logo centered, draw mode *All Sequential*; Unity logo only if your license requires it. Audio: music Vorbis q50 *Streaming*, SFX ADPCM *Decompress On Load*. Mesh compression Medium, Optimize Mesh Data on. Build size target: < 80 MB (typical for this setup: ~45–60 MB).

## 4.12 Verify
1. Window → General → Test Runner → EditMode → **Run All** (all green).
2. Play in Editor: mock consent → menu with MOCK BANNER → Play level 1 → drag vertically anywhere → hand hint disappears after the first rock.
3. F1 opens the debug panel: skip ad grace, win levels, test interstitial pacing with the mock ad screen ("Close early" must show the toast and grant nothing).
4. Profiler (Deep Profile off) on a mid-range Android device: **GC Alloc = 0 B** on ordinary gameplay frames. Small allocations appear only on discrete events (near-miss callout text, analytics event params), never every frame.
