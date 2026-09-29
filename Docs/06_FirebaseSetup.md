# 6. Firebase Setup

## 6.1 Project
1. console.firebase.google.com → **Add project** "Moon Pull" → enable **Google Analytics** → create a new GA4 property (region: your company's).
2. Add **Android app**: package `com.<studio>.moonpull`, SHA-1 of your upload key (optional, for later auth). Download `google-services.json` → `Assets/` (git-ignored).
3. Add **iOS app**: bundle id `com.<studio>.moonpull`. Download `GoogleService-Info.plist` → `Assets/` (git-ignored).
4. Project settings → Integrations → **Google Play** link, and AdMob link (6.6).

## 6.2 SDK (UPM tarballs, so the `MOONPULL_FIREBASE_*` defines switch on automatically)
1. Download the latest **Firebase Unity SDK** `.tgz` packages: `com.google.firebase.app`, `…analytics`, `…crashlytics`, `…remote-config` (use v12 or newer; Consent Mode v2 needs ≥ 11.7).
2. Copy them to `Packages/firebase/` → Package Manager → **+ → Add package from tarball** (app first).
3. Force Resolve (Android). iOS pods resolve at build.
4. Play the Editor: the console should show `[Analytics] consent …` from the debug service (Editor never calls Firebase natively), with no compile errors.

## 6.3 Analytics
1. **Consent defaults** are already in place: `Assets/Plugins/Android/AndroidManifest.xml` (enable Player → Publishing → **Custom Main Manifest**) and the iOS `ConsentDefaultsPostProcess`. Analytics starts denied; `AnalyticsConsentBootStep` applies the UMP/TCF result.
2. GA4 → Admin → Data retention: **14 months**.
3. Register custom definitions (Admin → Custom definitions), event-scoped: `level`, `stars`, `reason`, `placement`, `ad_format`, `ad_source`, `chain`, `region`, `boat`, `spin_type`, `product_id`, `step`, `source`. Metrics: `duration_sec`, `score`, `progress_pct`, `coins`, `value`.
4. Mark as **key events**: `level_complete`, `iap_purchase`, `ad_revenue`.
5. Funnel exploration: `tutorial_step (step=1)` → `tutorial_step (step=2)` → `funnel_level_01` … `funnel_level_10`.
6. DebugView: Android `adb shell setprop debug.firebase.analytics.app com.<studio>.moonpull`; iOS scheme argument `-FIRDebugEnabled`.
7. Revenue: if you link AdMob (6.6), GA4 also receives Google's automatic `ad_impression` event with revenue. Our custom `ad_impression` carries no `value`, so revenue is not double-counted, but impression counts are. Use our `ad_revenue` for LTV and ROAS dashboards and Google's for AdMob-native reports. Rename ours if the duplicate name confuses your team.

## 6.4 Crashlytics
1. Console → Crashlytics → **Enable**.
2. Android IL2CPP symbols: Player → Publishing → **Symbols: Public** (Create symbols.zip). After each release build:
   `firebase crashlytics:symbols:upload --app=<ANDROID_APP_ID> <path>/*.symbols.zip`
3. iOS: the Crashlytics Unity plugin adds the dSYM upload run script to Xcode automatically. Keep **Debug Information Format = DWARF with dSYM** for Release.
4. Custom keys set automatically: `state`, `level`, `boat`. Breadcrumbs: `level_start N`. Non-fatals: save corruption (`InvalidDataException`).
5. Verify: add a temporary `throw new System.Exception("test")` behind a debug button, run a release build, relaunch, and check the dashboard after about 5 minutes.

## 6.5 Remote Config
Console → Remote Config → create parameters (types as listed). Local defaults mirror the ScriptableObjects, and `RemoteConfigApplyStep` clamps every value.

| Key | Type | Default | Allowed range (clamped) | Tunes |
|---|---|---|---|---|
| `interstitial_start_level` | Number | 4 | 2–20 | first level (1-based) with interstitials |
| `interstitial_every_levels` | Number | 2 | 1–10 | levels between interstitials |
| `fullscreen_cooldown_seconds` | Number | 45 | 30–600 | min gap after any full-screen ad |
| `lifetime_grace_seconds` | Number | 180 | 180–1800 | no interstitials before this much lifetime play |
| `app_open_min_background_seconds` | Number | 30 | 30–3600 | background time before app open |
| `rewarded_level_multiplier` | Number | 3 | 2–5 | "Triple Your Reward" |
| `rewarded_idle_multiplier` | Number | 2 | 2–4 | "Double Idle Earnings" |
| `difficulty_multiplier` | Number | 1.0 | 0.5–1.5 | level generator difficulty |
| `starter_pack_intro_hours` | Number | 48 | 0–168 | discounted starter pack window |

Recommended setup:
1. Conditions: `Tier1` (US, CA, GB, AU, DE, NO, SE, CH, JP), `Tier2`, `Rest`. Start Tier1 with `interstitial_every_levels = 3`, since Tier1 users are worth more as retained players than as extra impressions.
2. **A/B Testing** → Remote Config experiment: `interstitial_every_levels` 2 vs 3, goal = **ad_revenue per user D7** with retention D7 as a guardrail.
3. Fetch interval: 12 h in release (`FirebaseBootStep.fetchIntervalHours`); dev builds fetch every launch. Values fetched late apply on the next session, by design.

## 6.6 AdMob ↔ Firebase link
AdMob → Apps → App settings → **Link to Firebase** (both apps). This gives AdMob user-level metrics and GA4 audiences for mediation reporting.

## 6.7 Privacy
- Disable **Google Signals** in GA4 unless you need cross-device reports (it adds disclosure obligations).
- Play Data safety and App Store privacy labels: see item 14 when requested. Collected via Firebase: device IDs (App instance ID), app interactions, crash logs, diagnostics. Via AdMob: advertising ID, approximate location (IP), app interactions.
