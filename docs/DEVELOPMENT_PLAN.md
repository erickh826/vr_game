# Development plan

1. Foundation: audit project, preserve baseline, implement independent grid/matching/validation/session occupancy and test fixture. Gate: Edit Mode tests pass in installed Unity.
2. XR foundation (next): verify official Meta compatibility with Unity 6000.0.85f1 and URP 17.0.4; select pinned Meta XR/Interaction SDK and XR provider versions; assess project changes before installation. Create a new BrickBloom scene through Unity Editor while preserving SampleScene. Configure Android build profile, application identifier, passthrough, hands and repositionable workbench. Gate: device build, stable passthrough and hand interaction on Quest 3.
3. Offline gameplay: Unity BlueprintLoader, BuildSession with piece ownership, primitive visual factory, ghost rendering and supply. Integrate verified grab/release callbacks and 90-degree rotation. Convert body-center world pose to board-local pose. Disable XR transform ownership before final snap. Add HUD and reset. Gate: six-brick completion and reset with hands only.
4. Polish: magnetic preview, sounds, celebration, hints, hand loss/recovery and performance/usability passes. Gate: repeated headset sessions without stuck selections or duplicate placement; measure frame time on hardware.
5. AI: optional FastAPI service, constrained intent and procedural generation. Gate: generated blueprints pass the same validator and offline mode remains intact.

## Validation workflow
Use Unity 6000.0.85f1. Window > General > Test Runner > EditMode > Run All. Tests use a separate Editor-only assembly. Command line (close other Editors for this project first):

```powershell
& 'C:\Program Files\Unity\Hub\Editor\6000.0.85f1\Editor\Unity.exe' -batchmode -nographics -projectPath 'D:\self\project\metavr\My project (3)' -runTests -testPlatform EditMode -testResults 'Logs/brickbloom-editmode.xml' -logFile 'Logs/brickbloom-editmode.log'
```

Do not add -quit to the test invocation; allow the test runner to exit. Report actual XML totals and compiler errors. Android build and device testing are separate gates.
