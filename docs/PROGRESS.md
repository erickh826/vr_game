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
