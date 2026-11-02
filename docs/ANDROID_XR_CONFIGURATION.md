# Android / OpenXR configuration gate

Completed 2026-11-02 (Hong Kong), on feature/xr-package-foundation. This completes Phase 2 Step 1 project configuration and Editor validation. It is not an APK build or a hardware/passthrough/hand-grab test.

Correction discovered during Gate A: Core 207's public CLI skips HeavyProcessing tasks still in their two-minute cooldown. The original 89-task report was incomplete and did not establish that every Required task passed. AndroidXRSetup now invokes the source-verified internal UpdateTasks overload with bypassCooldown=true and saves its completed summary. This exposed the missing source-manifest GameActivity rule. XRFoundationSetup creates the missing source manifest through the SDK API; see GATE_A_PASSTHROUGH.md and the latest reports for the corrected, expanded check. No rules are ignored or suppressed.

## Applied configuration

| Area | Saved value |
| --- | --- |
| Editor / packages | Unity 6000.0.85f1; existing pinned package set unchanged |
| Android ID | com.brickbloom.mr.dev |
| Android API | Minimum 32, target 34 |
| Runtime / ABI | IL2CPP, ARM64 (retained) |
| Graphics | Vulkan only; automatic graphics selection off; Linear color retained |
| Install location / orientation | Auto / LandscapeLeft, matching installed Meta SDK configuration updater |
| Build profile | Assets/BrickBloom/BuildProfiles/QuestDevelopment.asset; Android, Development Build on |
| XR startup | Android XRGeneralSettings and XRManagerSettings sub-assets; InitManagerOnStart, automaticLoading and automaticRunning enabled |
| Provider | Exactly one Android loader: UnityEngine.XR.OpenXR.OpenXRLoader |
| Meta configuration | HandsOnly; Passthrough Required; Quest 3 included in retained device targets |
| Android features | Meta XR, existing Meta foveation, Hand Tracking Subsystem, Meta Hand Tracking Aim, Oculus Touch Controller Profile |
| System loading background | ContextualPassthrough |

The profile uses shared PlayerSettings and the existing global scene list. No scene was replaced or generated in this step. When XRFoundation.unity is created, configure the profile's scene override before building the first XR APK. Profile activation is local Editor state: select QuestDevelopment in File > Build Profiles on another checkout, or run the setup method below.

`BuildTarget.Android` is 13; `BuildTargetGroup.Android` is 7 in this Unity version. The tool uses enum values and Editor APIs. XR settings' serialized `Keys: 07000000` is correct; do not substitute 13 there.

## Required-rule results

Meta Project Setup evaluated 89 applicable Android tasks: **0 outstanding Required**. Native OpenXR validation: **0 errors**. XR Core Utils BuildValidationRule collection: **0 required errors**. The aggregate report also checks the requested app ID/API/graphics, Android loader/startup, selected development profile and enabled hand features.

The initial report identified two additional Required issues, corrected explicitly:

- Meta's OVRInput compatibility check requires Oculus Touch Interaction Profile. Enabled the profile; HandsOnly remains authoritative and no controller gameplay or rig was added.
- Required passthrough forbids a black system loading background. Set ContextualPassthrough through OVRProjectConfig's API.

`insightPassthroughEnabled` is obsolete and has no effect in Core 207.0.1. Its serialized value remains 0 intentionally; the supported `insightPassthroughSupport` property is Required. Scene-level OVRManager and OVRPassthroughLayer setup belongs to the next milestone.

Saved evidence: [Meta task report](validation/android-meta.json) and [aggregate OpenXR/settings report](validation/android-xr.json). Remaining recommendations include HDR/MSAA/stereo/rendering quality, splash screen, optional input migrations and simulator installation. These are reported, not suppressed or automatically fixed. Review rendering recommendations when preparing the actual Quest scene. Scene-dependent rules must be rerun then; a project-level pass with the template scene does not certify the future rig.

Android-target C# compilation succeeded. Edit Mode regression tests: **47/47 passed, zero failed/skipped**; evidence in ignored Logs/brickbloom-android-editmode.xml and .log. Final configuration log: Logs/brickbloom-android-final.log. Reopen validation log: Logs/brickbloom-android-validate-reopen.log. No Gradle, IL2CPP native build or headset check was run.

## Reproduce without hand-editing serialized assets

Close other Editors for this project. From the project root:

```powershell
& 'C:\Program Files\Unity\Hub\Editor\6000.0.85f1\Editor\Unity.exe' -batchmode -nographics -buildTarget Android -projectPath 'D:\self\project\metavr\My project (3)' -executeMethod BrickBloom.Editor.AndroidXRSetup.ApplyAndValidate -reportFile docs/validation/android-meta.json -logFile Logs/brickbloom-android-setup.log
```

For validation without explicit setup changes, replace ApplyAndValidate with ValidateOnly. SDK initialization can still perform its own automatic updates. Do not add -quit: the method waits for package/task registration and exits with 0 for pass, 2 for failed gate, or 1 for execution failure. Inspect the completed report/log because a Windows GUI executable launched from PowerShell may return before Unity finishes.

Implementation is in Assets/BrickBloom/Editor/AndroidXRSetup.cs, outside the pure core assembly. No automatic setup runs on normal import. A SessionState flag resumes an explicitly requested validation after profile activation triggers domain reload. The first setup run exposed this reload boundary; the continuation was corrected and setup rerun successfully.

Verified APIs in installed sources: XRGeneralSettingsPerBuildTarget.CreateDefaultSettingsForBuildTarget/CreateDefaultManagerSettingsForBuildTarget (only when absent), XRPackageMetadataStore.AssignLoader, OVRProjectConfig properties/CommitProjectConfig, public OVRProjectSetupCLI.GenerateProjectSetupReport and OpenXRProjectValidation.GetCurrentValidationIssues. No FixAll call or validation-ignore mutation is used. No vendor code is patched.

Two narrowly scoped internal API accesses are version-guarded: Unity 6000.0 BuildProfile.CreateInstance plus profile platform properties, and XR Core Utils 2.6.0 BuildValidator.GetCurrentValidationIssues. Exact signatures were inspected in source and exercised by compilation/execution; mismatches fail rather than silently skipping checks. Profile creation source: [UnityCsReference 6000.0 BuildProfileCreate.cs](https://github.com/Unity-Technologies/UnityCsReference/blob/6000.0/Editor/Mono/BuildProfile/BuildProfileCreate.cs). Review these accesses before an Editor/package upgrade.

## Dirty-file handling and commit boundary

Pre-existing PlayerSettings changes were Unity Cloud project ID, projectName and organizationId. They are compatible and preserved in the working tree, but deliberately excluded from this Android milestone's staged blob. The new app ID/API/graphics/install-location/orientation settings are included in the commit. No wholesale restore, scene rewrite or .meta replacement occurred. The other three original dirty files remain untouched and uncommitted. The Unity Cloud project link is not needed for this offline development build.

Next: create the standalone Quest passthrough scene using the saved profile, then build/install an APK and verify the room/cube rendering on Quest 3 before integrating hand grabbing or snapping.
