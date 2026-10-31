# Phase 2 — XR foundation: passthrough and hand-grab hardware proof

Audit date: 2026-10-31 (Hong Kong). This is an executable plan, not a claim of XR installation or hardware validation. No package, scene, renderer asset or project setting was changed in this review.

Update 2026-11-01: Step 1 package installation and Editor tests have now been executed. See [actual resolved versions, test evidence and SDK import side effects](XR_PACKAGE_VALIDATION.md). The tables below retain the original audit/proposal; the committed packages-lock.json is authoritative for installed versions. Android profile/configuration and hardware gates remain pending.

## Current readiness

| Area | Observed state | Action before hardware build |
| --- | --- | --- |
| Editor | Unity 6000.0.85f1 installed and compiles core/tests | Retain this Editor; no upgrade needed to meet documented minimum |
| Rendering | URP 17.0.4; global PC pipeline, Android quality 0 uses Mobile_RPAsset/Mobile_Renderer | Keep URP version; configure a new Quest-specific asset through Editor and assign to the Quest profile/quality |
| Mobile renderer | Forward; HDR on, terrain holes on, MSAA disabled, render scale 0.8; depth/opaque textures off | Proposed baseline: HDR/terrain holes/post processing off, intermediate texture Auto, MSAA 4x, render scale 1.0; measure on headset, do not rewrite original assets |
| Android | IL2CPP, ARM64, Linear color; manual graphics list Vulkan then OpenGLES3 | Preserve IL2CPP/ARM64/Linear; use Vulkan-only for a reproducible first Quest build |
| Android SDK | Minimum 23, target Automatic; template app ID | Raise minimum per Meta setup validation; use explicit target for reproducibility and a dedicated development app ID |
| Tools | Android module, SDK, NDK, OpenJDK present; SDK platforms 34, 35, 36, 37.0 listed | Validate SDK/JDK/NDK in Preferences > External Tools and build; folder existence does not prove Gradle success |
| XR | No Meta Core, Interaction, OpenXR provider or XR Management packages | Install the reviewed set below in the existing project, then inspect resolved dependencies |
| Scene | Only template SampleScene enabled | Create Assets/BrickBloom/Scenes/XRFoundation.unity; preserve SampleScene and existing settings |
| Hardware | No headset test or connection verification performed in this task | Developer-mode Quest 3, USB debugging authorization and on-device hands required |

The PC and Android render paths differ, so an Editor preview cannot certify headset rendering. Unity recommends transparent camera backgrounds, Vulkan, and reduced URP overhead for Quest passthrough. The render baseline above adapts those recommendations; MSAA/render scale are proposed starting values, not profiled results. [Unity Quest project setup](https://docs.unity.cn/Packages/com.unity.xr.meta-openxr@2.0/manual/project-setup.html).

## Proposed package configuration

Use Unity OpenXR as the provider with Meta Core's OVRCameraRig/passthrough and Meta Interaction SDK's OVR integration. Do not combine this with a second XR Origin or AR Foundation camera rig. The OVR naming does not require the deprecated Oculus XR provider. Meta recommends OpenXR 1.15.1, Unity 6 and Meta SDK v74 or newer; its setup floor is 6000.0.66f2. Current Interaction SDK documentation also lists that Editor minimum. Our 6000.0.85f1 meets it. [Meta provider guidance](https://developers.meta.com/vr/documentation/unity/unity-xr-plugin/), [Interaction requirements](https://developers.meta.com/vr/documentation/unity/unity-isdk-packages-and-requirements/), [OpenXR compatibility](https://developers.meta.com/vr/documentation/unity/unity-and-openxr-compatibility/).

| Package | Proposed pin | Evidence / role |
| --- | --- | --- |
| com.unity.xr.openxr | 1.15.1 | Meta's documented recommended provider; registry minimum Unity 2021.3 |
| com.unity.xr.management | 4.4.0 | OpenXR 1.15.1 dependency; provider settings and initialization |
| com.meta.xr.sdk.core | 207.0.1 | Registry minimum 6000.0.66f2; camera rig and passthrough |
| com.meta.xr.sdk.interaction | 207.0.1 | Interaction Essentials runtime and editors |
| com.meta.xr.sdk.interaction.ovr | 207.0.1 | Exact Core/Essentials dependencies both 207.0.1; OVR integration |
| com.unity.xr.hands | 1.7.2 | Core 207.0.1's declared dependency; allow UPM to resolve |

Official registry records were fetched read-only from packages.unity.com. Selected versions, Editor requirements, dependency maps and package hashes are saved in [XR_PACKAGE_EVIDENCE.json](XR_PACKAGE_EVIDENCE.json). These are documentation/metadata-compatible candidates, not an installed, compiled or hardware-certified combination. The registry exposes newer OpenXR versions, but this plan selects Meta's explicit recommendation rather than automatically choosing latest. [Core registry](https://packages.unity.com/com.meta.xr.sdk.core), [Interaction bridge registry](https://packages.unity.com/com.meta.xr.sdk.interaction.ovr), [OpenXR registry](https://packages.unity.com/com.unity.xr.openxr).

Keep URP 17.0.4, Input System 1.20.0 and UGUI 2.0.0. Essentials metadata declares UGUI 1.0.0 and TextMeshPro 3.0.6; inspect actual Unity 6 dependency resolution and compilation before accepting the lockfile. Do not force a UGUI downgrade or manually copy TMP files. No exact URP-patch certification was found; the Editor/package requirements and URP guidance support this choice, while import/build are still required gates.

Do not add com.unity.xr.oculus. Do not add com.unity.xr.meta-openxr merely because its name contains Meta: it supplies Unity's AR Foundation/OpenXR extensions and is a separate package from Meta XR Core. The initial OVR passthrough cube does not require AR Foundation, MRUK, depth occlusion, scene meshing, camera pixel access, Platform services or All-in-One. If an installed package validator requires an additional dependency, inspect and document that specific requirement before extending the set. [Meta compatibility and rig choices](https://developers.meta.com/vr/documentation/unity/unity-and-openxr-compatibility/).

## Work order and acceptance gates

### 1. Resolve packages and create a reproducible Android build profile

Work in the existing project on a focused XR branch; preserve the four unrelated dirty files recorded in PROGRESS.md. Record the manifest/lockfile diff separately from settings. In Window > Package Management > Package Manager, use Add package by name with the explicit versions above (Core, Essentials, OVR bridge and OpenXR); accept necessary dependencies only after review. Compile and run all Edit Mode tests. Stop on dependency/compiler errors rather than upgrading Unity to conceal them.

File > Build Profiles > Android > Switch Platform (Unity 6.0 may not show the newer Meta Quest platform). Create a dedicated development profile. Player > Other Settings: IL2CPP, ARM64, Linear color, Vulkan; use a development ID such as com.brickbloom.mr.dev. In XR Plug-in Management > Android enable OpenXR and Initialize XR on Startup; enable the Meta XR feature group. Enable the standalone provider only if Link testing is needed. Review Project Validation and Window > Meta > Tools > Project Setup Tool issues individually, inspect proposed settings changes, and resolve required Android items. Avoid indiscriminate Fix All against the existing project. [Meta manual setup](https://developers.meta.com/vr/documentation/unity/unity-tutorial-hello-world-manual-steps/).

Start with minimum API 32 / target API 34 for this development APK, as Meta's current first-party creation guidance suggests; confirm the installed SDK validator agrees and recheck publishing requirements separately before release. Android 34 is installed. Do not leave target selection to the highest installed preview SDK. [Meta first-party Android setup guidance](https://github.com/meta-quest/agentic-tools/blob/main/skills/hz-new-project-creation/SKILL.md).

**Gate:** package resolution is recorded, Console is free of compiler errors, core tests pass and Android project validation has no required errors. Commit only reviewed package/settings changes.

### 2. Build passthrough before adding gameplay code

Create a new empty XRFoundation scene with no template camera. Use Hierarchy > Interaction SDK > Add OVR Comprehensive Interaction Rig. Let the wizard create or reference exactly one OVRCameraRig and create the interaction rig as its child. Store an editable prefab copy under Assets/BrickBloom/Prefabs/XR. Set OVRManager > Quest Features > General > Hand Tracking Support to Hands Only. Do not add a separate XRI rig. [Meta comprehensive rig setup](https://developers.meta.com/vr/documentation/unity/unity-isdk-add-comprehensive-interaction-rig/).

Set OVRManager Tracking Origin Type to Stage, Passthrough Support to Supported and Enable Passthrough on. Add OVRPassthroughLayer using the documented Core setup (or Passthrough Building Block); configure it behind virtual content. CenterEyeAnchor camera: Background Type Solid Color, RGBA (0,0,0,0), no post processing. New scene lighting: no skybox. Verify the installed Inspector labels and underlay controls before setting them; do not script assumed SDK fields. [Meta passthrough tutorial](https://developers.meta.com/vr/documentation/unity/unity-passthrough-tutorial/).

Create a simple workbench root at unit scale and a visible colored 8 cm cube, placed within seated reach using serialized test poses. Create a Quest-specific URP asset/renderer via Editor; use the baseline above and assign the new asset to the intended Android quality/profile. Leave existing PC/mobile assets intact. Disable costly lighting/shadows for this first visual test. Add only XRFoundation to the development profile's scene override; preserve the global sample scene list where profile overrides allow it.

Build a Development APK to Builds/Quest/BrickBloomMR-XRFoundation.apk and install via Build And Run on a USB-authorized Quest 3. Record application ID, commit, package lock, Android SDK, headset OS version and build log. Start the app on the headset with controllers put away.

**Gate A:** standalone APK launches, both eyes render the cube over live passthrough, head tracking is correct, no black/opaque background or launch crash. Test the APK directly; Link or simulator success does not satisfy this gate. If this fails, fix deployment/rendering before writing snapping adapters.

### 3. Add hand-tracked cube grabbing using SDK setup tools

Use a unit-scale CubeRoot with a separate visual child and one root BoxCollider sized to 0.08 m. Add Rigidbody as required by Interaction SDK; disable gravity and use kinematic transform movement, with no throwing or dynamic physics. Right-click CubeRoot > Interaction SDK > Add Grab Interaction. Set Add Required Interactors to Hands and Supported Grab Types to Pinch and Palm; use the existing collider, then Create. Inspect wizard-generated references/components and verify the installed SDK respects the kinematic/no-throw configuration. Limit the prototype to one hand selecting the cube at a time; disable two-hand scale/rotation. [Creating grabbable objects](https://developers.meta.com/vr/documentation/unity/unity-isdk-create-grabbable-object/), [Grab wizard options](https://developers.meta.com/vr/documentation/unity/unity-isdk-grab-quick-action/).

Rebuild immediately. Do not require a board, blueprint renderer, custom mesh, rotation gesture or architecture changes for this test. A small recovery/reset presenter may return the cube to its serialized tray pose after cancellation; use verified installed SDK selection events, not guessed callbacks. Log select/release/cancel transitions without per-frame logging.

**Gate B:** both hands independently pinch/palm-grab, move and release the cube for at least 20 cycles each, with controllers off. No scaling, falling, stuck selection or post-release transform overwrite. Cover/leave tracking view while holding, recover, open/close the system menu and resume; the cube remains recoverable. Capture device video and logs. Record observed failures instead of claiming usability from Editor tests.

### 4. Bridge to the puzzle only after Gates A and B

Add a Unity-facing assembly and a Meta adapter assembly; leave BrickBloom.Core unchanged unless integration reveals a concrete rule defect. Create one stable physical piece ID per scene piece and register it once. Implement adapter grab sequence/session generation guards and the transform contract in SYSTEM_SPEC.md. Route valid release into TrySnap; use the successful returned slot to lock the transform. Verify duplicate callbacks produce one placement and one completion notification. Reset cancels interactions before clearing core state.

Add the hands-only Rotate Button, then a single ghost target, then the six-brick model. Add a workbench reposition control with no simultaneous active brick grab. Wrist Yaw Rotation remains disabled/experimental. Profile Snap Preview allocations on device after the loop works; do not block the first APK on GC optimization, AI, scene understanding or polish.

**Phase 2 exit evidence:** APK path/hash, successful Android build log, exact lockfile, Quest 3 OS, headset test checklist and recording. Until these exist, readiness remains documentation-reviewed rather than hardware-verified.
