# BrickBloom MR engineering rules

Unity project: this directory. Product name: BrickBloomMR. Do not rename or move it without explicit approval.

- Preserve existing assets, scenes, prefab references, .meta GUIDs and user changes. Never recreate this project.
- Inspect before editing; do not blindly edit serialized Unity files. Prefer Editor APIs for future scene generation.
- Unity 6000.0.85f1, URP 17.0.4. Verify installed versions and official compatibility documentation before any SDK/package changes. Never invent Meta APIs.
- Keep gameplay rules in the no-engine-reference BrickBloom.Core assembly. Keep Meta adapters separate; snap logic must remain testable without hands or controllers.
- Coordinates are board-local integers; X/Z minimum footprint corner, Y layer. Stud pitch 0.024 m; layer height 0.029 m. Body-center poses drive snapping.
- MVP: offline, 8x8, six bricks, 2x2/2x4, red/white/blue/yellow, hands only. No AI until offline core loop works.
- Require full footprint support from placed bricks one layer below. Rotation equivalence: square any valid turn; rectangle same parity. Match exact kind/color, unoccupied target, nearest eligible target within 35 mm horizontal and 32 mm vertical.
- Use deterministic placement; release XR ownership before applying/locking snap transforms. Rendering meshes and interaction colliders are separate.
- Small focused components, serialized scene dependencies, no unnecessary singletons or packages. Handle invalid data explicitly.
- Add NUnit Edit Mode tests for rule changes. Run available checks; distinguish compilation, Editor tests, Android build and actual headset verification.
- Update docs/PROGRESS.md per task. Make focused commits of owned files only. Never commit Library, Temp, Obj, logs or builds; never push without permission.
- Preserve existing unrelated working-tree modifications. No hardware or compilation claims without evidence.
