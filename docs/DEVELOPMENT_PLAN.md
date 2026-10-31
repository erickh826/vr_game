# Development plan

1. Foundation: audit project, preserve baseline, implement independent grid/matching/validation/session occupancy and test fixture. Gate: Edit Mode tests pass in installed Unity.
2. XR foundation (next): execute [Phase 2 hardware-first plan](PHASE_2_XR_FOUNDATION.md). Compatibility has been audited against official docs and registry metadata; packages have not been installed. Keep Unity 6000.0.85f1 and URP 17.0.4. Gate A: Android passthrough APK launches on Quest 3. Gate B: hand-tracked cube grabbing works with controllers off. Do not delay these gates for further core refactoring.
3. Offline gameplay: Unity BlueprintLoader and BuildSession presentation, primitive visual factory, ghost rendering and supply. Core piece ownership already exists; register each physical piece and route actual release through TrySnap. Integrate verified callbacks, cancellation and a hands-only Rotate Button for 90-degree turns. Convert body-center world pose to board-local pose. Disable XR transform ownership before final snap. Add HUD and reset. Gate: six-brick completion and reset with hands only. Wrist Yaw Rotation remains experimental until tested on Quest 3.
4. Polish: magnetic preview, sounds, celebration, hints, hand loss/recovery and performance/usability passes. Gate: repeated headset sessions without stuck selections or duplicate placement; measure frame time on hardware.
5. AI: optional FastAPI service, constrained intent and procedural generation. Gate: generated blueprints pass the same validator and offline mode remains intact.

## Validation workflow
Use Unity 6000.0.85f1. Window > General > Test Runner > EditMode > Run All. Tests use a separate Editor-only assembly. Command line (close other Editors for this project first):

```powershell
& 'C:\Program Files\Unity\Hub\Editor\6000.0.85f1\Editor\Unity.exe' -batchmode -nographics -projectPath 'D:\self\project\metavr\My project (3)' -runTests -testPlatform EditMode -testResults 'Logs/brickbloom-editmode.xml' -logFile 'Logs/brickbloom-editmode.log'
```

Do not add -quit to the test invocation; allow the test runner to exit. Report actual XML totals and compiler errors. Android build and device testing are separate gates.
