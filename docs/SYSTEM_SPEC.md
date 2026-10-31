# BrickBloom MR system specification

## Product and target
Hands-first offline Mixed Reality construction puzzle for standalone Meta Quest 3, Unity URP and C#. Intended product name BrickBloomMR. First playable: a six-brick translucent Ghost Model completed at a real desk with no controllers. Competition target: Meta VR Start Developer Competition 2026 (from project brief; submission requirements not yet verified).

## MVP requirements
Passthrough, hand tracking, hands-only UI, repositionable workbench, 8x8 board, 2x2 and 2x4 bricks in red/white/blue/yellow. Grab, move, rotate in 90-degree increments, magnetic preview, release, valid placement feedback, invalid rejection, bottom-up support, progress, reset, celebration, simple audio and visual feedback. Entire loop must work offline.

## Data and rules
Schema 1 JSON: schemaVersion, id, boardWidth, boardDepth, bricks. Each brick: unique id, kind, color, integer x/y/z and turns 0..3. JSON uses exact lowercase values. Coordinates specify minimum X/Z footprint corner after rotation; Y is one brick layer. At turns 0 a 2x4 footprint is 2 along X and 4 along Z; odd turns swap dimensions. Bounds apply to the whole footprint. There is no arbitrary maximum Y; unsupported layers are rejected.

Stud pitch 0.024 m, layer height 0.029 m. Board origin is its lower X/Z corner on the board top. Pose is body center (studs excluded), so Y center is (layer + 0.5) * layer height. Rendering/hand adapters convert world poses to this local frame; use unit scale for the workbench hierarchy.

Require every footprint cell to have ground or support in the preceding layer. This conservative MVP assumption excludes cantilevers. Validator checks schema, IDs, kind/color, bounds, overlap, rotation and support using sorted bottom-up construction; input array order is not a required play order. Collision-free hand approach paths are outside this logical validation.

Snap matches exact kind/color and equivalent rotation (2x2 all valid turns; 2x4 same parity). Only empty, currently supported slots qualify. Horizontal Euclidean radius <= 0.035 m and absolute vertical delta <= 0.032 m. Choose smallest 3D squared distance; exact ties use ordinal slot ID. Preview is read-only; TrySnap resolves again on release and commits occupancy. BrickBoard.TryPlace is a low-level logical operation without a distance check. Integration must use TrySnap for player releases.

## Architecture and ownership
Implemented: serializable Blueprint/BrickSlot DTOs, BrickDefinition, BrickRules, BlueprintValidator, BrickBoard session state and SnapResolver/BoardPoint. All production code currently lives in BrickBloom.Core, with no Unity or Meta references. Board snapshots mutable DTOs and returns defensive copies. Per-piece ownership/identity belongs to the upcoming BuildSession; adapters must not allow one piece to fill multiple slots.

Planned: GameBootstrap, BuildSession, BlueprintLoader; BrickPiece and BrickFactory; BrickGrabAdapter, BrickRotationController and SnapPreviewController; GhostRenderer and BrickVisualFactory; BuildHUD, HintController and CompletionController. Use a separate Unity-facing assembly for these components and a separate Meta adapter assembly when packages are chosen. Loader must handle malformed JSON and call validation before constructing a board. The fixture is currently deserialized with JsonUtility in tests only.

Future release transaction: drop XR selection/transform ownership, resolve current target, commit once, freeze piece and set exact target pose, hide matching ghost, update HUD/feedback. Invalid releases return to supply; reset clears state and restores supply/ghosts. Implement and test this integration before calling gameplay functional.

Primitive cubes and cylinders; separate BoxCollider interaction geometry; URP color materials and transparent ghost materials. No Blender dependency. Scene objects, materials, rig and visuals are not created by this foundation.

## Deferred AI
Only after playable offline loop: Quest -> FastAPI/Pydantic -> LLM constrained Build Intent -> procedural generator -> validator -> Quest. Never request arbitrary meshes. Retain offline fallback.
