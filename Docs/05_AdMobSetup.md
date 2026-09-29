# 5. AdMob Setup

## 5.1 Account
1. Sign up at admob.google.com with the Google account that owns the Play Console / Apple developer email.
2. Payments → complete **payments profile, tax info (W-8BEN outside the US), address PIN** (mailed at $10 earnings).
3. Settings → Account → enable **Two-step verification** (required for payouts).

## 5.2 Apps
1. Apps → Add app → **Android** → "Is the app listed on a supported store?" **No** (link it after publishing) → name "Moon Pull". Copy the **App ID** (`ca-app-pub-…~…`).
2. Repeat for **iOS**.
3. After store publication: App settings → **Link to store**. `app-ads.txt` verification only starts then.

## 5.3 Ad units (per platform)
| Unit name | Format | Settings |
|---|---|---|
| `MP_Rewarded` | Rewarded | Reward 1 "reward" (the game decides the real reward); **Server-side verification off**; Frequency capping off |
| `MP_Interstitial` | Interstitial | Types: Video + Image; frequency capping **off** (the game paces itself) |
| `MP_AppOpen` | App open | Orientation: Portrait |
| `MP_Banner` | Banner | **Adaptive**, automatic refresh 60 s |

For each: *Advanced settings → eCPM floor → **Google optimized, high floor*** for rewarded and interstitial, *medium* for banner and app open.
Paste all eight IDs into `AdConfig` → *Android/iOS Production*. The per-placement split already lives in analytics (`placement` param), so one rewarded unit is enough. Add per-placement units later only if you want separate floors.

## 5.4 Unity plugin
1. `Packages/manifest.json`: add
   ```json
   "scopedRegistries": [
     { "name": "OpenUPM", "url": "https://package.openupm.com", "scopes": ["com.google"] }
   ]
   ```
   and in `dependencies`: `"com.google.ads.mobile": "<latest from openupm.com/packages/com.google.ads.mobile>"`. This also pulls **External Dependency Manager**.
2. Assets → Google Mobile Ads → **Settings**: Android App ID, iOS App ID. Tick **Delay app measurement**, which holds measurement until consent. Set **User Tracking Usage Description**: "Your data will be used to show you ads that are more relevant to you."
3. Assets → External Dependency Manager → Android Resolver → **Force Resolve**. iOS resolves at build (CocoaPods).
4. Confirm `MOONPULL_ADMOB` is active: `AdMobAdsService` compiles and the Editor still uses `MockAdsService`.

## 5.5 Mediation (Unity Ads, AppLovin, Meta Audience Network)
1. Create accounts: Unity Ads (Unity Cloud Dashboard → Monetization), AppLovin MAX, Meta Business Suite → Monetization Manager. Register the Android and iOS apps in each.
2. Get the IDs: Unity **Game ID** + rewarded/interstitial/banner **placement IDs**; AppLovin **SDK key**; Meta **placement IDs** (rewarded, interstitial, banner).
3. Install adapters (OpenUPM, same registry):
   `com.google.ads.mobile.mediation.unity`, `com.google.ads.mobile.mediation.applovin`, `com.google.ads.mobile.mediation.metaaudiencenetwork`. Force Resolve again.
4. AdMob → **Mediation → Create mediation group** per format × platform (4 × 2 = 8 groups). Add bidding ad sources: **AppLovin, Meta Audience Network, Unity Ads** (bidding for all three). Enter the IDs from step 2. Leave "Optimize" on.
5. AdMob → Privacy & messaging → GDPR → **Ad partners → Custom ad partners**: add AppLovin, Unity Ads, Meta. Without this, their EEA bids are dropped.
6. iOS: the GMA settings window auto-adds **SKAdNetworkItems** for Google; add each network's list (published in their docs) to `Info.plist` with a PostProcessBuild script or the adapter's own settings.
7. Verify with Ad Inspector on a test device (`MobileAds.OpenAdInspector`, e.g. from the debug panel): every source should show "Adapter initialized".

## 5.6 Consent messages (UMP)
Privacy & messaging:
1. **European regulations (GDPR)** → Create message → apps: both → languages: en, tr, es, pt-BR, de, fr, ru, ja → buttons **Consent / Do not consent / Manage options** → purposes: default IAB list → Publish.
2. **US state regulations** → Create → both apps → Publish. This covers CCPA-style opt-out, and the Settings "Privacy options" button reopens it.
3. **IDFA explainer (iOS)** → Create → Publish. UMP shows it before the system ATT prompt; `AttRequester` then only asks if the status is still undetermined.
4. Development builds force EEA geography (`ConsentBootStep.debugGeographyEeaInDevBuilds`); add your device's hashed ID from the console log to `AdConfig.testDeviceIds`.

## 5.7 app-ads.txt
1. Host `https://<your-domain>/app-ads.txt` (root of the developer website set in Play Console / App Store Connect).
2. Contents:
   ```
   google.com, pub-XXXXXXXXXXXXXXXX, DIRECT, f08c47fec0942fa0
   ```
   plus the lines each mediation network shows in its dashboard (Unity, AppLovin, Meta).
3. AdMob → Apps → app-ads.txt tab → "Check for updates" (verification takes up to 24 h after store linking).

## 5.8 Policy & brand safety
1. Blocking controls → **Sensitive categories**: block Gambling, Dating, Alcohol, Politics, Get-rich-quick.
2. **Max ad content rating: T** (also enforced in code via `RequestConfiguration`).
3. App settings → **Not child-directed**. The code sends `TagForChildDirectedTreatment.False`.
4. Placement compliance (built in): rewarded ads are opt-in only and greyed out when not loaded; interstitials play only after tapping Continue on the win screen, from level 4, every 2 levels, 45 s apart, never in the first 3 minutes of lifetime play; app open is never in the first session; the banner shows only on menu/shop and never overlaps buttons (`BannerSafeArea`).

## 5.9 Testing checklist
- [ ] Dev build shows the "Test Ad" label on every format.
- [ ] Closing a rewarded ad early → toast "Watch the whole ad…", no reward.
- [ ] Airplane mode → rewarded buttons greyed; back online → enabled within 2–32 s (backoff).
- [ ] Remove Ads purchase → banner disappears immediately; interstitial/app open never show; rewarded still works.
- [ ] Background the app for 35 s on level-select → app open ad (second session onward).
- [ ] Release build uses production IDs (check logcat for the unit ID). Never click your own live ads.
