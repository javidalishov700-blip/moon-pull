# Moon Pull

Hyper-casual Unity 6 (6000.3.22f1) game: the moon controls the tide and your boat sails on its own.

## How the project is built

The repository holds only source. On every build, the editor generator (`Assets/MoonPull/Editor/Generator`)
creates all materials, meshes, sprites, audio, prefabs, configs and the scene into `Assets/MoonPull/Generated`
(git-ignored). A wiring typo fails the build instead of shipping silently.

- Menu: **Moon Pull → Generate Content and Scene / Build iOS Xcode Project / Build Android APK**
- Headless: `-executeMethod MoonPull.EditorTools.MoonPullBuild.BuildIos` (or `BuildAndroid`)

## Shipping to TestFlight (no Mac needed)

Same pipeline as Slice & Blast:

1. **GitHub Actions → "iOS Xcode project"** (`.github/workflows/ios-xcode.yml`): Unity generates the Xcode
   project on a free Linux runner and publishes it as an `ios-xcode-<run>` release. Runs on push to `main`
   or manually. Secrets: `UNITY_EMAIL`, `UNITY_PASSWORD`, `UNITY_LICENSE`.
2. **Codemagic → "moon-pull-xcode-to-testflight"**: downloads it, signs, runs CocoaPods (AdMob), builds the
   `.ipa` and submits it to TestFlight.

Alternatives in `codemagic.yaml`: `moon-pull-ios` (Unity on the Mac, all in one) and `moon-pull-android` (APK).

## One-time setup

- App Store Connect: create the app with bundle id `com.javidalishov.moonpull`.
- Codemagic: add this repository; it uses the existing `unity` env group and the `SliceBlast ASC Key` integration.
- GitHub: add the three Unity secrets above.

## Before release

- AdMob: replace the Google test app IDs in `Assets/GoogleMobileAds/Resources/GoogleMobileAdsSettings.asset`
  and the ad unit IDs in the ads config.
- IAP (`MOONPULL_IAP`) and Firebase (`MOONPULL_FIREBASE_*`) are optional; without them mock/fallback services run.
