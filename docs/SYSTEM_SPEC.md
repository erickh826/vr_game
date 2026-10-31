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
Implemented: serializable Blueprint/BrickSlot DTOs, BrickDefinition, BrickRules, BlueprintValidator, BrickBoard session state and SnapResolver/BoardPoint. All production code currently lives in BrickBloom.Core, with no Unity or Meta references. Board snapshots mutable DTOs and returns defensive copies. BrickKinds and BrickColors centralize schema values without changing JSON. PieceDefinition is immutable inventory data.

BrickBoard owns the physical-piece-to-target-slot relationship. Register each physical piece once with TryRegisterPiece(pieceId, kind, color). IDs are case-sensitive and distinct from blueprint slot IDs; blank IDs, invalid definitions and duplicate registration fail without overwriting inventory. A successful placement creates both piece-to-slot and slot-to-piece entries. One physical piece can own at most one slot and one slot at most one piece. Unknown pieces/slots, duplicate releases, conflicting ownership and invalid matches fail without mutations. Release callers cannot supply alternate kind/color values to impersonate a different brick.

API: CanPlace/TryPlace(pieceId, slotId, turns); Resolve/TrySnap(board, pieceId, turns, boardLocalCenter, ...). The previous identity-free overloads have been removed deliberately: no bypass of ownership is retained. TryGetPiece returns immutable data; ownership queries return strings only. Slots, preview candidates and successful snap targets remain defensive copies. Resolve never claims or reserves a slot; release resolves current availability again.

Completed is a synchronous Action event after the final placement commits both maps. It fires once on completion, not on polling, duplicate callbacks or invalid placement. Reset clears both ownership maps and the completion latch, emits no completion event, and retains registered inventory so the same physical pieces can be reused. A new blueprint/inventory needs a new board. Consumers must run mutations on the main thread, unsubscribe when disposed, avoid throwing in event subscribers and queue presentation/reset work after the release transaction. This is not a concurrent or multiplayer ownership authority.

Planned: GameBootstrap, BuildSession, BlueprintLoader; BrickPiece and BrickFactory; BrickGrabAdapter, BrickRotationController and SnapPreviewController; GhostRenderer and BrickVisualFactory; BuildHUD, HintController and CompletionController. Use a separate Unity-facing assembly for these components and a separate Meta adapter assembly when packages are chosen. Loader must handle malformed JSON and call validation before constructing a board. The fixture is currently deserialized with JsonUtility in tests only.

Future release transaction: capture the final valid local center and discrete turns, mark the grab sequence consumed, release XR selection/transform ownership, then TrySnap with that physical piece ID. On success freeze the piece at the returned target center and canonical rotation, hide its ghost and update feedback. On failure return to its tray pose. Apply completion visuals only after the successful snap pose is applied. Reset first cancels active grabs and invalidates pending callback sequences, then clears core ownership and restores pieces/ghosts. A core board cannot distinguish an old callback after Reset from a legitimate new placement: the adapter must discard stale callbacks using a session generation plus grab sequence. Integration is still to be implemented and tested on device.

## Transform ownership contract (planned Unity/Meta adapter)

Exactly one writer controls a physical piece transform at any time. The pure C# board never writes transforms. Keep the board/workbench hierarchy at unit scale. Workbench relocation is allowed only when no piece is held; placed pieces retain exact board-local poses as their parent moves. No Rigidbody simulation determines valid placement.

| State | Sole transform writer | Entry, allowed actions and exit |
| --- | --- | --- |
| InTray | BrickPiece/tray presenter | Apply serialized tray-local pose; no XR selection or velocity. A valid hand selection starts a fresh grab sequence and transfers control to Held. |
| Held | BrickGrabAdapter through one configured SDK transformer | One hand owns a piece; disable two-hand scale/rotation for MVP. Track translation; board-up orientation follows discrete turns. Rotate Button updates turns through this same writer, never through a competing Update script. |
| SnapPreview | Same writer as Held | Physical brick stays under hand control. SnapPreviewController writes only a separate preview visual at Resolve's target; it never moves the held root or changes ownership. Candidate may change or vanish. Leaving range returns to Held; release enters Released. |
| Released | BrickGrabAdapter release transaction | Accept one release per live grab sequence only. Detach SDK selection and stop its transformer before writing the final pose. Freeze body, resolve/commit once; success -> Placed, rejection -> InTray. Never let a late SDK update overwrite the snap. |
| Placed | BrickPiece board-pose presenter | No hand selection, SDK transform updates, gravity or throw velocity. Exact board-local center/rotation are authoritative; the piece remains attached to the repositionable board. Only reset returns it to InTray. |
| TrackingLost | Recovery handler within BrickGrabAdapter | From Held/SnapPreview, invalidate the grab sequence, suppress synthetic release/snap, cancel SDK selection, hide preview and freeze at the last valid pose. After cancellation, return unplaced piece to tray. Reacquisition requires a fresh selection; never auto-snap on tracking recovery. Already placed pieces remain Placed. |

Scene unload, application pause/focus loss and reset follow the same cancellation path as TrackingLost. Recovery must not clear existing committed placements. Test cancellation ordering against the installed SDK's actual events; these state names are our contract, not assumed Meta APIs.

## Rotation input

The initial MVP uses a hands-only Rotate Button: each accepted poke increments turns by one modulo four for the selected unplaced piece. Debounce one activation per press. Keep the body aligned to board up and apply rotation around its center; do not derive discrete turns from incidental hand orientation. The first cube smoke test can use the standard SDK transformer; add this constrained transformer only after the hardware grab gate passes.

Wrist Yaw Rotation is an experimental alternative, disabled by default and outside the first hardware milestone. It needs real Quest 3 testing for jitter, angular thresholds/hysteresis, occlusion, comfort, left/right hand parity and tracking recovery before promotion. It must not run concurrently with Rotate Button as a second transform writer.

## Snap Preview allocation review

No allocation optimization was introduced before profiling. Every valid Resolve currently reads Slots, allocating an array, a read-only wrapper, one BrickSlot copy per target and LINQ iterator state. CanPlace uses a capturing FirstOrDefault predicate and a Where support iterator/delegate per eligible candidate; repeated support enumeration can create further enumerators. Interface enumeration can also allocate. These are potential per-frame GC sources, not measured byte counts. Registration dictionaries and the constructor snapshot are primarily startup allocations. Immutable PieceDefinition lookups do not expose mutable storage.

After hardware gameplay works, capture Unity Profiler GC.Alloc call stacks for one and two active hand previews at the device's actual frame rate. Reuse the SnapResolver component instance; avoid per-frame HUD string formatting. Only if measured cost matters, consider an internal read-only candidate traversal that preserves the public defensive-copy boundary. Do not expose slots or replace defensive copies with shared mutable DTOs to save allocations.

Primitive cubes and cylinders; separate BoxCollider interaction geometry; URP color materials and transparent ghost materials. No Blender dependency. Scene objects, materials, rig and visuals are not created by this foundation.

## Deferred AI
Only after playable offline loop: Quest -> FastAPI/Pydantic -> LLM constrained Build Intent -> procedural generator -> validator -> Quest. Never request arbitrary meshes. Retain offline fallback.
