# BrickBloom MR

**Finish a small brick model on your real desk, using only your hands.**

BrickBloom MR is an offline, seated, hands-only mixed reality building game for Meta Quest 3. An 8 × 8 board sits on your real table in passthrough. You pinch 2×2 and 2×4 bricks from a tray and release them on a translucent guide. A brick stays only when its target is empty, the layer below supports it, its colour and quarter-turn match, and it was released close enough. Otherwise it returns to the tray. There are three models in sequence: a six-brick bridge, a seven-brick pyramid and a nine-brick castle. Hold a fingertip on **NEXT** to move on, or on **RESET** to start again. No controller, account or network is needed.

The full project story is in [docs/PROJECT_STORY.md](docs/PROJECT_STORY.md).

![Pinch interaction with a virtual brick](docs/screenshots/01_Hand_Tracking.png)

![Completed brick structure](docs/screenshots/02_Level_Complete.png)

## Status

- The development build has been played on a Quest 3: all three models completed, NEXT and RESET used, reset while holding a brick.
- A signed, non-development release APK builds successfully. It has not yet been installed and verified on the headset.
- Day-by-day engineering notes and evidence are in [docs/PROGRESS.md](docs/PROGRESS.md).

## Requirements

- Unity **6000.0.85f1** with Android Build Support, URP 17.0.4
- Meta XR SDK 207.0.1 (`com.meta.xr.sdk.core`, `com.meta.xr.sdk.interaction`, `com.meta.xr.sdk.interaction.ovr`), resolved from `Packages/manifest.json`
- Meta Quest 3 with hand tracking enabled

## Project layout

| Path | Assembly | Contents |
| --- | --- | --- |
| `Assets/BrickBloom/Scripts/{Bricks,Core,Interaction}` | `BrickBloom.Core` | Board, blueprint validation, snapping and support rules, dwell timer. **No Unity engine references.** |
| `Assets/BrickBloom/Scripts/Presentation` | `BrickBloom.Presentation` | Workbench, pieces, ghost targets, progress pips, instruction sign. |
| `Assets/BrickBloom/XR` | Assembly-CSharp | Meta Interaction SDK adapters: hand grab ownership, hold-to-press buttons. |
| `Assets/BrickBloom/Editor` | Editor | Batch-mode scene setup, validation and build entry points. |
| `Assets/BrickBloom/Data/Blueprints` | — | The three level blueprints (JSON). |
| `Assets/BrickBloom/Tests/EditMode` | NUnit | Edit Mode tests for the rules and blueprints. |

The rules live in a plain C# assembly so they can be tested without a headset, hands or controllers. The XR layer only reports where a brick was released; the board decides whether it stays. Each blueprint is a data file that must pass the same validator, and a test checks that every level can be built bottom-up from the tray.

## Running the tests

```sh
Unity -batchmode -nographics -buildTarget Android -projectPath . \
  -runTests -testPlatform EditMode -testResults Logs/editmode.xml
```

## Building

Development APK (installed as `com.brickbloom.mr.dev`):

```sh
Unity -batchmode -nographics -buildTarget Android -projectPath . \
  -executeMethod BrickBloom.Editor.XRFoundationSetup.BuildApk -logFile Logs/build.log
```

Release APK (`com.brickbloom.mr`, written to `Builds/Quest/BrickBloomMR-release.apk`):

```sh
Unity -batchmode -nographics -buildTarget Android -projectPath . \
  -executeMethod BrickBloom.Editor.ReleaseBuild.BuildReleaseApk -brickbloomVersionCode 1 -logFile Logs/release.log
```

The release build signs with `Keystore/brickbloom-release.keystore` and reads its password from `Keystore/keystore-password.txt`. That folder is git-ignored and is not part of this repository; supply your own to build a release.

## Credits

Brick models: [Kenney Brick Kit](https://www.kenney.nl), CC0 1.0 (see `Assets/BrickBloom/Models/Kenney/Kenney-BrickKit-License.txt`).

## Disclaimer

BrickBloom MR is a game. It is not a medical device and does not measure recovery.
