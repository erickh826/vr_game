# Progress

## 2026-10-31 — Project audit and foundation

Actual root: D:\self\project\metavr\My project (3). Not renamed. Git branch main. Initial unrelated modifications: Assets/TutorialInfo/Icons/URP.png, ProjectSettings/ProjectSettings.asset, ProjectSettings/ShaderGraphSettings.asset, ProjectSettings/URPProjectSettings.asset. Preserve these.

Unity 6000.0.85f1 (ee7790f1426a); installed matching Editor. URP 17.0.4, Input System 1.20.0, Test Framework 1.6.0. Packages and lockfile preserved. No declared Meta XR, Interaction SDK or XR Management/provider package. Unity built-in XR module is not Meta SDK integration.

Template project: Assets/Scenes/SampleScene.unity is the sole enabled build scene; existing custom scripts are tutorial Readme/editor scripts. Graphics default references PC_RPAsset; Android quality default index 0 references Mobile_RPAsset. Existing pipeline/material assets preserved.

Android module, SDK, NDK and OpenJDK folders exist. Player settings: Android scriptingBackend 1 (IL2CPP), architectures 2 (ARM64), minimum SDK 23, target SDK automatic, template application ID. No Android build performed; module presence is not proof of build readiness or Quest compatibility. No headset tests performed.

Existing Unity .gitignore covers caches and builds; retained. Added BrickBloom folder structure, independent foundation assembly, seven requested model/rule/board/resolver components, board-local pose value, six-brick bridge JSON and NUnit Edit Mode tests. Added engineering rules, specification and phased plan. No scenes, XR settings, package versions or existing metas intentionally changed.

Implementation decisions: full footprint support, minimum-corner grid coordinates, body-center snap poses, sorted bottom-up sequence, nearest 3D candidate and ordinal exact-tie ordering. JSON loading/runtime presentation and physical-piece ownership are deferred; current board only tracks target occupancy.

Validation: Unity 6000.0.85f1 batch-mode import and C# compilation succeeded. NUnit Edit Mode: 31 total, 31 passed, 0 failed, 0 skipped. First run was 30/31: the tie test fixture used decimal arithmetic that was not an exact tie; corrected to a symmetric X/Z layout and reran successfully. Evidence: Logs/brickbloom-editmode.xml and Logs/brickbloom-editmode.log (ignored generated output). Unity generated all new asset/folder metadata. No new modifications to pre-existing tracked paths appeared during import. Android build, Play Mode integration, XR and Quest hardware remain unverified. Next task: verify compatible Meta package versions from official documentation, then establish the hands/passthrough workbench scene and Quest build workflow.

## 2026-10-31 — Ownership review and XR transition preparation

Review found that slot occupancy alone allowed one physical piece to fill multiple matching targets. Fixed in pure C# with immutable registered PieceDefinition identities and bidirectional piece/slot ownership. Identity-free placement/snap overloads were removed; all callers/tests now use physical IDs. Duplicate releases, occupied targets, unknown IDs and mismatched definitions fail without acquiring ownership. Completed fires once per completed build after both mappings commit. Reset clears mappings/completion latch and retains reusable registered inventory.

Added shared BrickKinds/BrickColors constants with explicit schema-value contract tests; JSON remains unchanged. Defensive snapshots/copies retained. Added ownership tests covering duplicate release to same/different targets, duplicate slot ownership, invalid registration/IDs, reset/reuse, repeated and reentrant completion attempts, and mutation of preview/placed-result copies. Total Unity Edit Mode result: 47 passed, 0 failed, 0 skipped; C# compilation succeeded. Evidence: Logs/brickbloom-ownership.xml and Logs/brickbloom-ownership.log. Source whitespace check and final focused diff review are part of the commit check.

SYSTEM_SPEC now defines InTray/Held/SnapPreview/Released/Placed/TrackingLost transform writers, cancellation before snap/reset, main-thread event contract, and adapter rejection of callbacks from old grab/session generations. Rotate Button is the initial MVP control; Wrist Yaw Rotation is experimental and requires Quest 3 tests. Documented per-preview snapshot/DTO/LINQ/enumerator allocations without premature optimization or claims of measured GC cost.

Read-only XR audit reconfirmed Unity 6000.0.85f1/URP 17.0.4, Android IL2CPP/ARM64/Linear, Vulkan-first graphics, template app ID/minimum SDK 23 and no installed XR stack. Mobile URP has HDR on, MSAA off, render scale 0.8. Android tools/platforms are present; no Android build attempted. Verified Meta documentation and official Unity package registry metadata; proposed OpenXR 1.15.1 + Meta Core/Interaction/OVR bridge 207.0.1, with declared XR Hands 1.7.2 and XR Management 4.4.0 dependencies. Compatibility is documentation/metadata based, not a certified installed combination. See PHASE_2_XR_FOUNDATION.md and XR_PACKAGE_EVIDENCE.json for evidence, dependency caveats, exact setup sequence and hardware acceptance gates.

No XR packages installed or upgraded; no existing scenes, pipeline assets, settings, JSON or GUIDs intentionally changed. The original four dirty paths remain excluded from this milestone. Next priority is the first Quest 3 passthrough APK, followed immediately by SDK hand-grab cube validation; further architectural work waits for a demonstrated need. No hardware testing performed.

## 2026-11-01 — Phase 2 Step 1 package installation and Editor verification

Created feature/xr-package-foundation from the ownership milestone. Installed the five authorized direct manifest pins: Meta Core, Interaction Essentials and OVR integration 207.0.1; OpenXR 1.15.1; XR Management 4.4.0. Unity generated packages-lock.json. Actual transitive resolutions include XR Hands 1.7.3 (not the declared 1.7.2 minimum), XR Core Utils 2.6.0, Legacy Input Helpers 2.1.12 and built-in TMP 5.0.0 shim. Existing UGUI 2.0.0, URP 17.0.4 and Input System 1.20.0 retained. No UGUI/TMP C# conflict observed.

Unity 6000.0.85f1 import/C# compilation succeeded with no C# compiler errors. First and subsequent-launch Edit Mode runs both passed 47/47, zero failed/skipped. Evidence: Logs/brickbloom-xr-packages.xml/.log and Logs/brickbloom-xr-packages-repeat.xml/.log (ignored generated outputs). The second run validates reopening with generated settings/dependencies. No further core code changes were made.

SDK initialization automatically created Assets/XR, Assets/Oculus and resource defaults, linked XR config objects in EditorBuildSettings, added UI/sample tags and enabled Meta XR feature groups on Standalone/Android. These reviewed generated assets/metas are retained. Loader assignment is not complete: XRGeneralSettingsPerBuildTarget has no build-target entries. Import also changed Android preferred install location; after both runs the pre-install PlayerSettings snapshot was restored. All four pre-existing dirty files were byte/hash-verified identical to their pre-install state and excluded from this commit. SDK may change install location again on future launches; account for it in the next configuration task.

Added narrowly scoped Git ignores for generated DevAgent and OVR publishing settings (including metas), which may contain local credentials. DevAgent remains disabled. No AI feature was implemented. See XR_PACKAGE_VALIDATION.md for the full dependency table, source-inspected import behavior and remaining diagnostics.

Limitations: this is Editor compile/test validation, not full Android or Quest compatibility. Initial log contains headless settings-window diagnostics, a licensing refresh diagnostic despite resolved entitlement, and an unsuccessful direct removal of transitive Legacy Input Helpers. SDK partial-class import diagnostics and outstanding required project setup fixes remain. No vendor patch or forced dependency downgrade was applied. Android profile, app ID, API 32/34, Vulkan-only and explicit Android OpenXR loader initialization still require the second half of Step 1. No APK build or headset test performed.

## 2026-11-02 — Phase 2 Step 1 Android configuration gate passed

Added explicitly invoked Editor setup/validation tool outside BrickBloom.Core. Through verified Editor APIs, configured com.brickbloom.mr.dev, minimum API 32/target 34, Vulkan-only, retained IL2CPP/ARM64/Linear, and accepted Meta SDK Auto install location/LandscapeLeft orientation. Created QuestDevelopment Android build profile with Development Build on. Created Android XRGeneralSettings/XRManagerSettings sub-assets only where missing, assigned the OpenXR loader and enabled startup/automatic loading/running. BuildTargetGroup.Android is 7, distinct from BuildTarget.Android 13.

Configured OVRProjectConfig HandsOnly and Passthrough Required while retaining Quest 3 in device targets. Enabled Android Hand Tracking Subsystem and Meta Hand Tracking Aim alongside existing Meta XR features. Initial validation had two Required failures: Oculus Touch Interaction Profile for SDK OVRInput compatibility and MR system loading background. Explicitly enabled that profile and ContextualPassthrough; gameplay remains hands-only. Did not set the obsolete insightPassthroughEnabled field, modify vendor code, suppress validators or run FixAll.

Android-target compilation succeeded. Meta report evaluated 89 tasks with 0 outstanding Required issues; OpenXR and BuildValidationRule collections have 0 errors. Reports committed under docs/validation. Android-target Edit Mode regression tests passed 47/47, no failed/skipped tests. Logs remain under ignored Logs/. Non-required recommendations remain visible, including rendering/HDR/MSAA/stereo quality and optional input migration. No APK/native Android build or hardware tests were performed.

The first profile activation caused domain reload and lost the queued report callback; added a SessionState continuation and reran successfully. Tool includes version-guarded internal calls for Unity 6000.0 profile creation and Core Utils validation collection, inspected against exact source signatures. See ANDROID_XR_CONFIGURATION.md for reproduction, evidence and limitations.

PlayerSettings had pre-existing Cloud project ID/name/organization changes. They are compatible and preserved as uncommitted working-tree changes; staged PlayerSettings includes only this milestone's Android/XR settings. Other original dirty files remain untouched. No existing scene or core gameplay script changed; all existing GUIDs preserved. Next is Phase 2 Gate A: create the passthrough scene and verify an actual Quest 3 APK before hand-grab cube work.

## 2026-11-02 — Gate A scene and Android APK; hardware pending

Created XRFoundation via Editor APIs: one local OVRCameraRig prefab variant, Stage origin, enabled passthrough layer (Core 207 internally fixes it to Underlay), transparent-black CenterEyeAnchor with post processing/HDR off, and a blue 8 cm cube with unit root scale and separate visual/collider. No interaction/gameplay implementation or existing scene rewrite. New Quest URP/renderer use Forward/Auto, HDR/shadows/terrain holes off, 4x MSAA and render scale 1. Existing Mobile quality points to the new asset (also shared by iPhone in the template); PC/global pipeline assignment and original pipeline assets are preserved. Profile scene override and BuildPlayerOptions both select only XRFoundation. StereoRenderingPath.Instancing serializes as 2, while OpenXR was already SinglePassInstanced; enabled Subsampled Layout.

First IL2CPP ARM64/Gradle build succeeded but revealed a validation flaw: Core 207's public CLI omits HeavyProcessing tasks during their two-minute cooldown. The previous 89/91-task zero-Required results did not cover all checks. Corrected AndroidXRSetup to use the verified internal UpdateTasks overload with bypassCooldown=true; 93 tasks exposed a missing source-manifest GameActivity failure. Created the absent Assets/Plugins/Android/AndroidManifest.xml through OVRManifestPreprocessor, preserving any pre-existing manifests. Full report now explicitly includes GameActivity passed, 93 tasks / 0 Required, and OpenXR/BuildValidation no required errors. No rule ignored, vendor patch or dependency change.

Rebuilt the final APK successfully: 0 errors, 4 warnings, 65,350,688 APK bytes; first full build about 697 seconds, final incremental build about 50 seconds. Packaged manifest confirms development ID, API 32/34, ARM64, GameActivity and required hand tracking/passthrough. Raw log retains an unqualified Meta Required summary despite fresh full Android report passing; no individual failing rule appears on the final build. Documented this discrepancy and remaining optional warnings instead of claiming warning-free build. BuildReport totalSize is not APK file size; separate artifact report records actual bytes, SHA256 and package-lock hash. No authorized ADB device was present, so no installation or Quest 3 hardware test occurred.

SDK automatically removed unsupported Quest 1, retained Quest 3 and persisted OculusRuntimeSettings as a preloaded runtime asset. Retained these reviewed changes. Excluded build-populated URP runtime cache, formatting-only GraphicsSettings, unreferenced generated OVRBuildConfig, empty StreamingAssets metadata and SceneTemplateSettings defaults from the commit. They remain local: automatic approval review rejected the combined restore/delete command with only "blocked by policy", so no cleanup deletion was performed. Original three unrelated dirty files and global EditorBuildSettings were byte-verified unchanged; original Cloud fields remain unstaged. Initial Editor script compilation caught internal-only shadow setters and BuildReport count types; corrected before successful scene generation/build. All 47 core Edit Mode tests passed after the first APK; final regression result is recorded below. Next: install the recorded APK on Quest 3, complete both-eye/passthrough/head-tracking Gate A, then add the hand-grab rig for Gate B.

Final regression: 47/47 passed, 0 failed/skipped, after the cooldown-validation and manifest changes (Logs/brickbloom-gatea-editmode-final.xml/.log). Machine-readable summary: docs/validation/gate-a-tests.json.

## 2026-11-03 — Gate A hardware acceptance reported by user

User confirmed Quest 3 color room passthrough (supplied OVRPlugin initialization log excerpt), clear 8 cm blue cube in both eyes under Single-Pass Instanced Vulkan, stable 6DoF world placement while turning the head, and no crash/black screen. Marked Gate A passed based on this user report; the agent did not perform the hardware test. No claim of persistent spatial anchors, measured performance, menu/resume testing, or verified installed APK hash. Headset OS, duration and full capture were not provided. Added a separate hardware acceptance JSON; historical build-time reports remain unchanged.

Next is Gate B: attach the verified SDK interaction rig to the existing camera rig, add hand pinch/palm grabbing to the cube with kinematic/no-throw movement and no two-hand scaling, then test 20 grab/release cycles per hand, tracking loss, system menu/resume and relaunch recovery on Quest 3. This update changes documentation only; JSON parsing and Git whitespace checks run, with no Unity compilation/test rerun needed. Existing dirty files and build byproducts are untouched.

## 2026-11-03 — Installed Gate B APK after stale Gate A deployment

User could see the blue cube but could not move it. Read-only ADB inspection proved the installed base.apk SHA256 matched the Gate A artifact exactly, not the newer locally built Gate B APK. Installed the existing Gate B APK with adb install -r (Success), launched UnityPlayerGameActivity, and verified installed SHA256 equals the local APK. Device log confirms passthrough initialization and Gate B lifecycle diagnostics; no NullReferenceException/AssertionException/MissingReferenceException was found in the captured startup window. The last captured state was paused/unfocused with zero completed grab cycles; this does not establish grab functionality. User should wear/resume the headset and attempt near pinch/palm grabbing. Gate B hardware acceptance remains pending.

Deployment evidence: docs/validation/gate-b-deployment.json; raw device log is ignored under Logs/. No code, scene, packages or settings changed and no rebuild/tests were needed for this deployment correction. Existing uncommitted Gate B implementation and other user/build changes were preserved.

## 2026-11-13 — Gate B "cannot grab" investigation (in progress)

User reports the blue cube is visible but cannot be picked up by hand. Read-only ADB check: installed base.apk SHA256 8d7103e7…2fc027 equals the local Builds/Quest APK (diagnostics build of 2026-11-03 18:34, installed on device 2026-11-13 13:45), so this is not another stale-deployment case. Static review of XRFoundation.unity found no wiring defect: CubeRoot has kinematic Rigidbody, non-trigger 8 cm BoxCollider, Grabbable (one-grab free transformer, max 1 grab point), child HandGrabInteractable (Pinch|Palm, rigidbody and pointable element assigned); OVRCameraRigRef, hand refs and tracking-to-world transformer are assigned on hand/controller data sources; the only scene-disabled interactor objects are controller locomotion ones; hand skeleton version is OpenXR. The cube has near hand-grab only (no distance/ray grab).

Unattended relaunch through ADB with the proximity sensor overridden reached XR session VISIBLE but the app was paused by the system Guardian dialog before the scene's first frame, so no GateB Startup/Tracking diagnostics were produced; proximity override was restored afterwards. No conclusion on bug versus operation yet: it needs a worn-headset run with the log capture (Logs/brickbloom-gateb-live-grab.log, ignored). No code, scene, package or settings changes; no rebuild or tests run.

Worn-headset run 2026-11-13 15:29–15:31 (user performed; Logs/brickbloom-gateb-live-grab.log): Startup reported 4 HandGrabInteractors, cube at world (0, 0.78, 0.55), interactable active; no Unity errors or exceptions. Both hands were tracked with valid poses on the two active interactors. Across 35 two-second samples the closest wrist-to-cube distance was 0.434 m (right) and 0.549 m (left); wrist Z never exceeded 0.184 m against the cube's 0.55 m. grabbing=False throughout, no Select/Unselect events, 0 cycles. So no grab was attempted within near-grab range in this run: the failure is consistent with the hands not reaching the cube (only near hand-grab is configured), and grab behaviour at contact range remains untested. Limits: 2 s sampling can miss brief reaches; head pose and fingertip distance are not logged, so a hand-space versus render-space offset is not excluded. Gate B hardware acceptance still pending. No code/scene changes.

## 2026-11-13 — Gate B grab confirmed working; cause was operation, not a bug

User retried and reports the cube can be picked up: the hand must be at the cube and the fingers must actually close (pinch thumb+index, or close the hand) — hovering or touching with an open hand does nothing, and there is no distance grab. Device log (Logs/brickbloom-gateb-live-grab.log, 15:33:16–15:33:45) corroborates: 6 Select/Unselect pairs, all hand=Left grabType=Pinch, cube moved with the hand and stayed where released, scale stayed 1, 0 RecoveryReset, 0 Unity errors/exceptions. The earlier "cannot grab" report is therefore explained by reach/gesture, with no defect found in the grab wiring.

Not yet covered, so Gate B acceptance stays pending: right-hand grabs, palm grabs, 20 cycles per hand, tracking loss, system menu/resume and relaunch recovery.

Diagnostics defect found (not fixed): every Unselect logged hand=Unknown/grabType=None and leftCycles/rightCycles stayed 0 despite 6 completed cycles. GateBCubeDiagnostics clears the active hand in HandleSelectingInteractorRemoved before the Grabbable's Unselect pointer event is handled, so the cycle counters never increment. The counters cannot be used as acceptance evidence until this ordering is fixed; count Select/Unselect log lines instead. Usability note: the cube is fixed 0.55 m in front of the Stage origin at 0.78 m height, so the user has to walk to it. No code, scene, package or settings changes; no rebuild or tests run.

## 2026-11-13 — Fixed Gate B cycle counter ordering; rebuilt and installed

GateBCubeDiagnostics no longer clears the active hand when the interactable drops its selecting interactor; it marks the release pending and clears after the Grabbable's Unselect/Cancel pointer event has been logged and counted. Pause/focus/disable recovery checks use the held state (active and not release-pending). A forced release during pause/focus loss is treated as recovery and is not counted as a completed cycle. Only Assets/BrickBloom/XR/GateBCubeDiagnostics.cs changed; no BrickBloom.Core rule change, so no new NUnit test, and Edit Mode tests were not rerun.

Evidence: batch-mode Android build via XRFoundationSetup.BuildApk succeeded (C# compiled, preflight passed, 0 errors, 4 warnings, 115.8 s; Logs/brickbloom-gateb-cyclefix-build.log). APK 93,918,531 bytes, SHA256 5545fef75436e5c0959e04eb165bcfe4b9ee8eff19780c28768ff29961503376. adb install -r returned Success and the installed base.apk hash equals the local APK. The counter fix itself is not yet verified on the headset; that needs a worn run showing Unselect lines with the correct hand and incrementing leftCycles/rightCycles. Gate B acceptance remains pending. Not committed.

## 2026-11-13 — Gate B cycle run on headset (user performed, log analysed)

Run 15:44–15:53 on the installed 5545fef7… APK; log Logs/brickbloom-gateb-cycles.log (ignored). 0 Unity errors/exceptions.

- Counter fix verified on device: every Unselect line carries the correct hand and grab type, and leftCycles/rightCycles increment (first process ended at left 21, right 23).
- Cycles: 45 Select (Left 19 pinch + 2 palm, Right 22 pinch + 2 palm), 44 Unselect, 2 RecoveryReset. The 20-per-hand target is met; palm coverage is only 2 per hand. Select count is one short of Unselect+Reset (45 vs 46); not explained.
- Scale stayed (1,1,1) on every release; both RecoveryReset entries returned the cube to the tray pose.
- Delay before the cube can be grabbed is hand-tracking acquisition time, not range: after launch/resume the app keeps running frames while all interactors report tracked=False/connected=False, then grabs work as soon as a tracked hand reaches the cube. Measured with 2 s sampling: launch 1 hands tracked within 1.9 s; relaunch between 5.9 and 8.0 s; resumes mostly 0.1–2.1 s, one resume 10–12 s. Cause on the system/SDK side versus app side not determined. The app gives no visual cue that hands are not ready yet.
- Relaunch recovery: second process started with the cube at the tray pose and a right pinch grab succeeded 11.3 s after Startup.
- Not evidenced: tracking loss during a grab (no TrackingLostDuringGrab reset was logged; both resets were ReleasedOutOfReach(Right), i.e. the cube was released more than 0.85 m from the tray), and pause/menu while holding (no ApplicationPaused or SystemFocusLostWhileSelected reset logged; grabs did work again after each resume).
- Other observations: frequent HMDUnmounted/HMDMounted flips during use (proximity sensor), and a FirstPersonLocomotor "ground could not be found" warning from the comprehensive rig's PlayerController at startup; neither investigated.

Gate B acceptance not declared: tracking-loss-while-holding and menu-while-holding remain unverified. No code changes in this step.

## 2026-11-13 — "Cannot regrab after snap-back" report; reach diagnostics build installed

User held the cube, moved the hand out of view, the cube returned to the tray, and they then could not grab it again. Log (same process, 16:00:34–16:01:39): right pinch Select, then RecoveryReset reason=ReleasedOutOfReach(Right) at 16:00:42 with the hand still reported tracked, so the TrackingLostDuringGrab path was not exercised; the release happened before any tracked/connected flag dropped. For the following ~57 s both hands were tracked, no Select occurred, and the closest wrist-to-cube sample was 0.336 m (successful grabs earlier had wrist distances of roughly 0.18–0.30 m). No errors. Bug versus insufficient reach is undetermined from wrist distance at 2 s sampling; reading HandGrabInteractor/Grabbable source showed no evident stuck state. Of the two earlier out-of-reach resets, one was followed by a successful grab only after a pause/resume and the other by no further grab attempt in the log.

Design gap noted (not changed): tracking loss is only detected if the hand is invalid/disconnected at the Unselect instant; a release that precedes the flag drop within 0.85 m of the tray leaves the cube where it was released.

Diagnostics only (no behaviour change) in GateBCubeDiagnostics.cs: index/thumb tip distance to the cube, pinching flag, interactor state, candidate/interactable presence on each Tracking line; a State line on every interactor state change; 0.5 s sampling while a tracked wrist is within 0.45 m. Batch-mode Android build succeeded (0 errors, 4 warnings; Logs/brickbloom-gateb-reachdiag-build.log); APK SHA256 3cdbf09a4083ad7bebefe8bd0f6d82926edf0218d453fa63f8d38ee87598308e installed with adb install -r and installed hash verified equal. Edit Mode tests not rerun. Reproduction on headset pending. Not committed.

## 2026-11-13 — Regrab-after-snap-back analysed with reach diagnostics: no stuck state found

Run on APK 3cdbf09a… (Logs/brickbloom-gateb-regrab.log, process started 16:08:26). User held the cube and moved the hand behind the back. 0 Unity errors/exceptions.

- Successful grabs in this run entered Hover with index/thumb tips about 0.08–0.11 m from the cube centre and Select at about 0.05–0.11 m (tray grab at 16:08:44: tip 0.065–0.082 m, wrist about (0.00,0.88–0.91,0.31–0.36)).
- 16:10:15 Select (Right, pinch) then 16:10:16 RecoveryReset reason=ReleasedOutOfReach(Right); the interactor went Select->Hover->Normal with the hand still tracked. Again no TrackingLostDuringGrab: the release precedes any tracked/connected flag change, so that branch remains unexercised on hardware. Earlier releases in this run (16:08:57, 16:09:09, 16:10:10) were within 0.85 m and left the cube where released.
- After the reset the cube was at the tray (0,0.78,0.55). Over the next ~27 s there were 10+ pinch gestures from both hands, but the closest index-tip distance to the cube was 0.153 m and pinches happened at 0.19–0.36 m, with wrists clustered near (0.0,0.92,0.24), i.e. about 7–12 cm short in Z of the successful tray grab. The interactor never reported candidate=True, so it never entered Hover; it kept cycling Normal/Disabled on each mid-air pinch (other interactors in the group taking the gesture). This is the SDK behaving as configured for a hand out of near-grab range, not a latched state from the reset.
- Conclusion: the "cannot regrab" case in this run is the pinch landing short of the cube, not a defect in grab state. Why the user under-reaches after a snap-back is not established; likely contributors are that the cube has no in-range feedback or depth cue (unlit flat colour, no highlight on hover). A render-versus-transform offset is not indicated (the same process grabbed at the tray with 6–8 cm tip distance) but is not strictly excluded.

Open items: tracking-loss-while-holding and menu-while-holding still unverified; no code change in this step; nothing committed.

## 2026-11-13 — Grab colour feedback and post-release tracking handling added; installed, hardware test pending

Finding that motivated the change: in the 16:10 run the grab ended 1.1 s after Select while the hand was still moving behind the user (pinching=False, tracked=True), so the runtime drops the pinch before reporting any tracking loss; waiting behind the back happens after the piece has already been released.

- New Assets/BrickBloom/XR/GrabStateColorFeedback.cs on CubeRoot: MaterialPropertyBlock tint of the Visual renderer from HandGrabInteractable state (Hover yellow, Select green, otherwise the material colour). Serialized references; logs an error and disables itself if they are missing.
- GateBCubeDiagnostics: each Unselect logs a ReleaseTracking line (tracked, connected, hand/index/thumb confidence). Release with low hand, index or thumb confidence resets to the tray (LowConfidenceAtRelease); a hand reported invalid/disconnected within 0.5 s after a normal release also resets (TrackingLostAfterRelease, serialized window). Neither is counted as a completed cycle. This is a heuristic: whether confidence actually drops in the behind-the-back case, and whether it false-triggers on ordinary releases, is unknown until the next headset log.
- XRFoundationSetup: ConfigureGrabFeedback helper (also called from ConfigureGrabbableCube), narrow batch entry point ConfigureGateBFeedback, and CheckScene now requires the feedback component.
- Scene changed through the Editor API only (ConfigureGateBFeedback in batch mode, preflight passed). Compared with a pre-change copy the scene gained 18 lines (component reference, the new component, the new serialized float) and lost none; Unity also reordered some existing prefab-override entries without changing their content.
- No BrickBloom.Core change; Edit Mode tests not rerun. Batch-mode Android build succeeded (Logs/brickbloom-gateb-feedback-build.log); APK 90,180,771 bytes, SHA256 e29161838e211f8595bf924ccb709cef63d2bf39c57d7aa180239941524175c8, installed with adb install -r and installed hash verified equal. Not verified on the headset yet. Not committed.

## 2026-11-13 — Headset run of colour feedback and release-tracking heuristic (user performed)

APK e2916183…; logs Logs/brickbloom-gateb-feedback.log and -feedback-2.log (process 28275, 17:05–17:13). 0 Unity errors/exceptions.

- 17:05:28–17:05:46: about ten pinches with fingertips 0.15–0.25 m from the cube, no Hover, no grab (hand short of the cube along the line of sight). From 17:11:45 the user reached further: 16 Select in total, each preceded by Normal->Hover at 0.045–0.112 m index-tip distance. User reports seeing the cube turn yellow and then being able to grab; colour feedback is thereby user-confirmed for Hover (Select colour not separately confirmed).
- Release handling: 11 normal releases logged tracked/connected/hand/index/thumb confidence all True and left the cube in place (no false reset in this sample; rightCycles reached 11). One release at 17:12:55.6 logged indexConfident=False, thumbConfident=False with tracked=True, and produced RecoveryReset reason=LowConfidenceAtRelease(Right); in the preceding 1.5 s the wrist pose was frozen at (0.226,0.928,-0.371), i.e. behind the user. The cube returned to the tray and was grabbed again 4.2 s later.
- A different behind-the-back excursion (17:12:28, wrist Z about -0.32) kept the grab alive and the cube was released normally in front at 17:12:29.8.
- The runtime never reported tracked=False or connected=False on an active hand in this run (180 samples), so TrackingLostDuringGrab and TrackingLostAfterRelease remain unexercised. Observed behaviour when the hand is hidden: pose freezes, tracked stays True, finger confidence drops, and the pinch is eventually released.

Gate B status: grab/release cycles, colour feedback and low-confidence recovery observed on hardware; palm coverage is thin (2 per hand earlier), left hand not exercised on this build, menu-while-holding not evidenced, strict tracking-lost flag path not reachable so far. Acceptance not declared. Nothing committed.

## 2026-11-13 — Left hand, palm grabs and tracking-lost path observed on headset (user performed)

Same APK e2916183…, process 28275, 17:17:45–17:20:21 (Logs/brickbloom-gateb-feedback-2.log). 0 Unity errors/exceptions.

- 18 Select in this window: Left 3 pinch + 7 palm, Right 5 pinch + 3 palm. Counters ended leftCycles=7, rightCycles=19 for the process; every normal release logged tracked/connected and hand/index/thumb confidence True and left the cube in place.
- TrackingLostDuringGrab(Left) fired 3 times (17:19:07, 17:19:35, 17:19:54): ReleaseTracking showed tracked=False, connected=False, and the cube returned to the tray each time. User report for these: while holding the cube they touched a controller, the cube turned back to blue and the virtual hand was replaced by a virtual controller. So the strict tracking-lost branch is reached when the runtime switches that hand from hand tracking to the controller, not by hiding the hand. After the first two resets the cube was grabbed again from the tray (17:19:32, 17:19:49).
- Focus loss at 17:19:55.15 came 0.4 s after the third reset, so the cube was already released; SystemFocusLostWhileSelected/ApplicationPaused-while-held were still not hit as such.

Gate B evidence now on hardware: 20+ cycles per hand (earlier run), pinch and palm on both hands, colour feedback (Hover user-confirmed), low-confidence recovery (1 hit, 0 false resets observed), tracking-lost recovery (3 hits via controller takeover), pause/resume and relaunch recovery. Not evidenced: focus loss while a hand is still holding the cube. Acceptance left for the user to declare. Nothing committed.

## 2026-11-13 — Gate B hardware acceptance (user decision)

User accepted Gate B after reviewing the log findings above. Recorded in docs/validation/gate-b-hardware.json with what was and was not evidenced (focus loss while still holding, and the Select colour, are not separately confirmed). The agent did not perform the hardware test; it analysed ADB logcat captures of the user's runs on the installed APK e2916183… whose hash matches the local build.

How to grab, as established on hardware: bring the fingertips to the cube until it turns yellow (about 8–11 cm from its centre), then close thumb and index (pinch) or curl middle and ring fingers around it (palm); it turns green while held. There is no distance grab. After launch or resume wait for hand tracking (up to about 12 s observed). Picking up a controller while holding returns the cube to the tray.

Commit scope: Gate B implementation and this session's changes only (XRFoundationSetup.cs, XRFoundation.unity, ComprehensiveInteractors prefab, Assets/BrickBloom/XR scripts, gate-b build/hardware JSON, PROGRESS.md). Left uncommitted as before: URP global settings, Graphics/Project/ShaderGraph/URPProject settings, TutorialInfo URP.png, android-meta/android-xr JSON, OVRBuildConfig, StreamingAssets.meta, SceneTemplateSettings.json. Not pushed.

## 2026-11-13 — Phase 3 step 1: blueprint-driven brick workbench (hardware test pending)

Branch feature/offline-brick-workbench from the Gate B commit 9b24644.

- Assets: Kenney Brick Kit 1.0 (CC0) bevel-hq-brick-2x2, bevel-hq-brick-2x4 and bevel-hq-plate-4x8 FBX copied to Assets/BrickBloom/Models/Kenney with the licence text. Models are visuals only. Measured at import and fitted by the setup tool: scale 0.3019, 2x2 footprint 0.0480 x 0.0477 m, 2x4 0.0477 x 0.0960 m, body bottom at -0.0145 m (half the 0.029 m layer), stud tops 0.0054 m above the body; two plates form the 8x8 board with the plate top at Y = 0.
- Core (no engine reference): SupplyLayout gives deterministic supply positions beside the board in the board-local frame.
- New BrickBloom.Presentation assembly (Unity, no Meta): BlueprintLoader (malformed JSON and validation failures reported, never thrown), BoardPose conversions, BrickPiece (identity and supply pose), BrickWorkbench (loads bridge-001, creates the BrickBoard, registers and spawns one physical piece per slot in bottom-up order), WorkbenchPlacer (places the workbench 0.42 m ahead of and 0.42 m below the head once tracking is valid, minimum height 0.55 m).
- Meta adapter (Assets/BrickBloom/XR): BrickGrabAdapter per piece reuses the Gate B release ordering and returns the piece to supply on tracking loss, low finger confidence, out-of-reach release, pointer cancel, pause or focus loss; raises Released(piece, kind) for the snap step. GrabStateColorFeedback gained a highlight-shell mode (translucent box, yellow in range, green held) because tinting is invisible on yellow and white bricks.
- Editor: BrickWorkbenchSetup.Configure (batch entry) creates Simple Lit colour materials, the transparent highlight material, BrickPiece_2x2/2x4 prefabs (padded BoxCollider separate from the mesh, kinematic Rigidbody, SDK grab components, one grab point, no two-hand transformer) and adds BrickWorkbench, BoardOrigin, BoardVisual and a shadowless KeyLight to XRFoundation; flat ambient light. The Gate B cube's WorkbenchRoot is kept but inactive; CheckScene now locates the cube including inactive objects.
- Not in this step: snapping on release, ghost targets, rotate button, HUD, reset. Released pieces stay where the user lets go.

Evidence: Edit Mode 60/60 passed, 0 failed/skipped (47 existing + 13 new; Logs/brickbloom-workbench-editmode.xml). Batch configure succeeded with preflight passed. Android build succeeded; APK 90,185,806 bytes, SHA256 c34d80a7df24656c4fc28be4b2eed5f7d30658b3dd7b010d2f719dd408a352b6, installed with adb install -r, installed hash verified equal. Build record saved as docs/validation/workbench-step1-build.json; BuildApk still writes to gate-b-build.json, which was restored to its committed Gate B content (the output name should be parameterised). Nothing verified on the headset yet. Not committed.

## 2026-11-13 — Step 1 workbench observed on headset (user performed)

APK c34d80a7…, log Logs/brickbloom-workbench-step1.log (18:56–18:58). 0 Unity errors/exceptions.

- Workbench: Ready with blueprint bridge-001, 6 slots, 6 pieces; placed 0.68 s later at (0.121,0.797,0.336) for a head at (0.036,1.217,-0.075), i.e. 0.42 m below eye height. User: reachable, slightly low but acceptable.
- Visuals (user report): size fine, no broken or missing materials; translucent highlight and green held state seen on every piece.
- Grabs: all six pieces selected (18 Select, right-hand pinch only), 20 Hover events, 17 UserRelease, 1 TrackingLostDuringGrab which returned the piece to supply (user confirms the return when touching a controller).
- User observation: bricks pass through each other. Expected in this step: pieces are kinematic and nothing resolves piece-to-piece contact; the specification makes snapping, not physics, decide placement.

Not covered here: left hand and palm on brick pieces, pause/resume with pieces, low-confidence release on pieces. Not committed.

## 2026-11-13 — Phase 3 step 2: snap on release, yaw-derived turns, ghost targets (hardware test pending)

User decisions: derive quarter turns from the piece's actual orientation at release (rotate button deferred, may be added later as an aid); keep the current workbench height.

- Core: BrickRules.NearestTurns(yawDegrees) rounds a yaw about board up to 0..3 and rejects non-finite input.
- BrickWorkbench.TryPlaceReleased is the release transaction: requires the piece's up axis within 60 degrees of board up, takes turns from yaw at that instant, calls SnapResolver.TrySnap with the piece ID and board-local body centre, then applies the committed slot centre and canonical rotation and raises PlacementChanged. Logs Snap / SnapRejected (Tilted, NoEligibleTarget) and Completed.
- BrickPiece gained Placed state (PlaceAt, ReturnToTray is a no-op once placed). BrickGrabAdapter: a user release either snaps or returns the piece to supply (no more floating pieces); tracking loss, low confidence, cancel, pause and focus loss return to supply without attempting a snap; placed pieces have their SDK interactables disabled on the next Update, outside the SDK release callbacks. The post-release tracking watch and out-of-reach rule were removed as redundant with return-on-reject.
- BrickGhostRenderer (brought forward from step 3 so targets are visible): one translucent box per slot in the slot colour, shown only while the slot is empty and fully supported.
- Editor: BrickWorkbenchSetup.ConfigureSnapping adds the ghost material, prefab and renderer; BuildApk takes -brickbloomBuildEvidence <file>.json (default build-latest.json) so gate records are no longer overwritten.
- Not yet: reset, HUD, completion feedback, rotate button, stale-callback generation guard (no reset exists to race with).

Evidence: Edit Mode 75/75 passed, 0 failed/skipped (Logs/brickbloom-snapping-editmode.xml). Batch configure succeeded, preflight passed; scene gained 15 lines and lost none versus the pre-change copy. Android build succeeded (docs/validation/workbench-step2-build.json); APK 90,187,675 bytes, SHA256 84eff92f7fb4ca7431f30d45a43b2eff43eefa2ee2440d83571d8842aabe2ba4, installed with adb install -r, installed hash verified equal. Not verified on the headset. Not committed.

## 2026-11-13 — Step 2 on headset: bridge completed; locomotion disabled; snap preview added (retest pending)

Run on APK 84eff92f… (Logs/brickbloom-workbench-step2.log, 19:20–19:23), user performed. 0 Unity errors/exceptions.

- All six pieces snapped and Completed was logged 2 min 22 s after start (b02, b01, b03, b04, b05, b06; left and right hand pinches). 20 releases were rejected with NoEligibleTarget and returned to supply. Most rejections with correct piece/orientation were vertical: released 4–9 cm above the target centre against the 3.2 cm rule (e.g. p04 at turns 3 with 2.9 cm horizontal error but 4.2 cm vertical). Several were upper-layer pieces tried before their support was complete (rule working). Yaw-derived turns resolved correctly (turns 3 accepted for the turns-1 slots by parity). User: rotation is sometimes awkward; wrong-colour rejection confirmed as intended.
- User report: "the whole board sank". The log shows the workbench was placed once and never moved. Read-only scene audit (LocomotionAudit.Report) found the comprehensive rig's hand microgesture locomotion active on both hands (teleport, step, turn) plus the Locomotor/FirstPersonLocomotor that moves the camera rig; a hand gesture moving the rig is the probable cause (not confirmed by a locomotion log line).
- Fix: LocomotionAudit.Disable deactivated 12 objects through the Editor API (all locomotion gesture sources on hands and controllers, BodyTeleportInteractor and the Locomotor object) and verified none remain active; preflight passed. Scene diff versus the pre-change copy: active-state overrides only.
- Added snap preview: BrickPiece.IsHeld (set by the grab adapter), BrickWorkbench.PreviewTargetSlotId (read-only Resolve with the same upright/yaw reading as release), and BrickGhostRenderer brightens the ghost a held piece would snap to (alpha 0.35 -> 0.85). No rule or tolerance change; the vertical tolerance question is with the user.

Evidence: Edit Mode 75/75 passed. Android build succeeded; APK SHA256 fc353102220b48e7cd7ccb588030f74e6ddf3c73cb96b4defc70f769e3616345 installed and installed hash verified equal. Locomotion fix and preview not yet verified on the headset. Not committed.

## 2026-11-13 — Step 2 retest on headset (user performed)

APK fc353102…, Logs/brickbloom-workbench-step2b.log (capture started after the app was already running, so the Ready/Placed lines are not in it). 0 Unity errors/exceptions; no FirstPersonLocomotor warning appears, consistent with the Locomotor being inactive.

- 6 Snapped, 3 Rejected, Completed 6/6 logged at 19:33:37. No Select was logged on any piece after it snapped.
- User confirms the negative cases behave as intended: release far from a ghost returns to supply; red piece on the blue ghost returns; second-layer piece without rotation returns; placed pieces cannot be picked up. User: "smoother now" with the snap preview.
- Rejections dropped from 20 (previous run) to 3 for one full build. The 3.2 cm vertical tolerance is unchanged; the user did not ask to widen it.
- The user did not mention the board sinking again in this run; not explicitly confirmed as fixed.
- Follow-up from the user: the board did not sink again after locomotion was disabled. Step 2 committed on feature/offline-brick-workbench (not pushed).

## 2026-11-13 — Phase 3 step 3: reset, progress and completion feedback (not yet on headset)

Requested by the user before going remote; the headset was disconnected, so nothing in this step is hardware verified.

- Core: DwellTimer (hold-to-confirm, fires once per continuous touch, rearms when the touch ends).
- Presentation: BrickPiece.ResetToTray raises ResetRequested first, then clears placed/held and applies the supply pose. BrickWorkbench.ResetBuild resets every piece, then BrickBoard.Reset, then PlacementChanged. BuildProgressDisplay shows one pip per slot behind the board (grey, green when placed, gold with a 4 s bouncing wave on completion).
- XR: BrickGrabAdapter handles ResetRequested (CancelIfHeld("Reset"), clears the pending lock, re-enables the grab interactables). ResetDwellButton fires ResetBuild when an index fingertip stays on the cap for 1 s; hands that are selecting something or have invalid tracking are ignored. Logs [BrickBloom Reset] TouchBegin/Fired and [BrickBloom Workbench] Reset/Celebration.
- Editor: BrickWorkbenchSetup.ConfigureSessionControls adds ProgressPip/ResetButton materials, the ProgressPip prefab, BuildProgressDisplay and a ResetButton (base, cap, legacy TextMesh "RESET" label) left of the board on the near side. The scene diff is additive only (0 removed lines); 4 HandGrabInteractors are referenced.

Evidence: Edit Mode 78/78 passed (3 new: DwellTimer x2, piece reset). Android build succeeded (docs/validation/workbench-step3-build.json). APK not installed: no device connected. Not committed.

Unverified on hardware: button reachability and accidental triggering, the 1 s dwell feel, whether the TextMesh label renders under URP on Quest, pip visibility behind the build, reset while a piece is held, and grabbing pieces again after reset.

## 2026-11-16 — Step 3 on headset (user performed) and fixes

APK 709bc23b…, Logs/brickbloom-workbench-step3.log, 0 exceptions. User report: button and RESET label visible; pips visible and not hidden by the build; completion feedback clear enough; 1 s dwell feels right; pieces can be grabbed and rebuilt after reset; no accidental resets. Log shows 5 resets (cleared 0, 0, 1, 6, 1) and 9 snaps.

- Bug (user report + log): reset while holding a piece did not return the held piece. Log 17:47:24 shows Release Cancelled reason=Reset for p02, but HandGrabInteractor.ForceRelease only sets an unselect override that takes effect on the interactor's next update, so the SDK kept writing the transform after the supply pose was applied and the later Unselect was ignored. Fix: BrickGrabAdapter keeps _forcedReleasePending and re-applies ReturnToTray when the forced Unselect arrives or the Grabbable has no selecting points (logs ForcedReleaseCompleted). The same path serves pause/focus-loss cancels.
- User: the button was a little hard to reach down to. BrickWorkbenchSetup.RaiseResetButton raises the base to 4 cm (cap top 5.2 cm) and extends the touch zone to 4 cm above the cap, 7 cm square. Scene diff: 8 changed lines.

Evidence: Edit Mode 78/78 passed. Android build succeeded; APK SHA256 5425a483a784809f8e1e1e5df2638a9e3bafa257930e94ab5a962ade9fb729b7 installed and installed hash verified equal. The two fixes are not yet verified on the headset. Not committed.

## 2026-11-16 — Step 3 retest (user performed) and button height

APK 5425a483…, Logs/brickbloom-workbench-step3b.log (capture ended early when ADB restarted for the next build), 0 exceptions.

- User: reset while holding a piece now returns everything and the piece can be grabbed again; nothing else broke. Log 18:22:22 shows ForcedReleaseCompleted | piece=p03 | source=Unselect.
- User: a real-world obstacle sits at the current button height and the cabled headset makes moving awkward; asked for the button to be higher. RaiseResetButton base height changed from 4 cm to 8 cm (cap top 9.2 cm, touch zone up to 13.2 cm).

Evidence: Android build succeeded; APK SHA256 331aaf308c96750508cd6c0ac0860874c9a0717e9ac232bd516da34d6332f417 installed and installed hash verified equal. The new height is not yet verified on the headset. Not committed.

## 2026-11-17 — Step 3 final button settings

- User decided against the 8 cm height (it was never tried on the headset) and asked for the previous 4 cm button with a slightly shorter hold. RaiseResetButton now sets base height 4 cm (cap top 5.2 cm, touch zone up to 9.2 cm) and dwell 0.8 s (was 1 s); the ResetDwellButton default is 0.8 s as well. Scene diff: 6 changed lines.

Evidence: Edit Mode 78/78 passed. Android build succeeded (docs/validation/workbench-step3-build.json). APK SHA256 6c94305ac22aa9bb1d587a58f99fe63eedf077cb7a8c928455b09ab288186f22 installed and installed hash verified equal. The 4 cm height was used in the 2026-11-16 retest; the 0.8 s dwell is not yet verified on the headset. Committed at the user's request.

## 2026-11-17 — Submission story draft

Updated docs/PROJECT_STORY.md into a submission-ready Markdown layout with a project snapshot and screenshot insertion guidance. The angle remains hand-practice motions played as a six-brick desk build; no project screenshots are currently available. No gameplay, scene, or package change. The draft states that the app is a game and not a medical device. Not submitted.

Reorganized the story under the Devpost prompts (inspiration, gameplay, implementation, challenges, accomplishments, lessons, and next steps), incorporating the verified three-level Quest build and distinguishing the built release APK from headset validation. Kept the story under the 500-word target and retained the non-medical disclaimer. Documentation only; not submitted.

## 2026-11-17 — Levels, instruction sign and release build (not yet on headset)

Requested by the user for the competition submission (deadline 2026-11-19 04:00 HKT).

- Levels: BrickWorkbench takes later blueprints, exposes LevelIndex/LevelCount/LevelChanged and TryAdvanceLevel (only when the build is complete, so no held piece is destroyed; wraps to level 1). New blueprints pyramid-002 (7 bricks, 4 layers) and castle-003 (9 bricks, 3 layers). BrickGhostRenderer and BuildProgressDisplay rebuild on LevelChanged.
- ResetDwellButton gains a NextLevel action with a visual root that is hidden until the build is complete. Log tag is now [BrickBloom Button] TouchBegin/Fired | action=….
- LevelSignDisplay: English how-to-play text while building, "complete / Hold NEXT" afterwards.
- Editor: BrickWorkbenchSetup.ConfigureLevels adds the blueprints, a green NEXT button 10 cm behind RESET and the LevelSign (dark panel + TextMesh) behind the board. Scene diff is additive (0 removed lines).
- Release: ReleaseBuild.BuildReleaseApk builds a non-development APK as com.brickbloom.mr signed with Keystore/brickbloom-release.keystore (git-ignored, kept by the user); player settings are changed in memory and restored. One line the build left in ProjectSettings.asset (AndroidKeystoreName) was restored to its previous value.

Evidence: Edit Mode 82/82 passed (4 new cases: three blueprints buildable bottom-up from the supply, sign text). Development build succeeded (docs/validation/workbench-levels-build.json); APK SHA256 643a0cba36b3216a55b37c15b1d564f2b1473029d5212e344ee6b7b89937956b installed and installed hash verified equal. Release build succeeded: Builds/Quest/BrickBloomMR-release.apk, SHA256 c3fb6788cf02ddf436e8a28e2f5dd698c05b0cb67b46328b7757bb28a3d2c3b8, package com.brickbloom.mr, versionCode 1, arm64, no debuggable flag, signer CN=BrickBloom. The release APK has not been installed or uploaded.

Unverified on hardware: level switching, NEXT button reach and visibility, sign readability and position, the far supply row of the 9-brick level, the 0.8 s dwell. Not committed.

## 2026-11-17 — Levels on headset (user performed)

APK 643a0cba…, Logs/brickbloom-levels.log, 0 exceptions. Log shows all three levels completed (6/6, 7/7, 9/9), NEXT fired after each completion (1→2→3→1) and a reset afterwards.

- User: sign text is readable; level switching replaces pieces, ghosts and pips; the far supply row of the 9-brick level is reachable; 0.8 s dwell is fine; no accidental RESET when reaching for NEXT.
- Open: the user reports NEXT is visible from the start. Code and log only show it being usable after completion; not reproduced or explained yet. It does not affect play.

Committed at the user's request. The release APK is still not installed or uploaded.
