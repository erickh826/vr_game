using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BrickBloom.Presentation
{
    // Loads one blueprint, owns the core board for the session and spawns one physical piece per target slot
    // into the supply area beside the board.
    [DisallowMultipleComponent]
    public sealed class BrickWorkbench : MonoBehaviour
    {
        [SerializeField] private TextAsset _blueprintJson;
        [SerializeField] private Transform _boardOrigin;
        [SerializeField] private BrickPiece _twoByTwoPrefab;
        [SerializeField] private BrickPiece _twoByFourPrefab;
        [SerializeField] private Material _redMaterial;
        [SerializeField] private Material _whiteMaterial;
        [SerializeField] private Material _blueMaterial;
        [SerializeField] private Material _yellowMaterial;

        // A released piece must be roughly upright: its up axis within 60 degrees of board up.
        private const float MinimumUpDot = 0.5f;

        private readonly List<BrickPiece> _pieces = new List<BrickPiece>();
        private readonly SnapResolver _resolver = new SnapResolver();

        public BrickBoard Board { get; private set; }
        public Transform BoardOrigin => _boardOrigin;
        public IReadOnlyList<BrickPiece> Pieces => _pieces;

        // Raised after a placement has been committed and the piece pose applied.
        public event Action PlacementChanged;

        public void Configure(TextAsset blueprintJson, Transform boardOrigin, BrickPiece twoByTwoPrefab,
            BrickPiece twoByFourPrefab, Material red, Material white, Material blue, Material yellow)
        {
            _blueprintJson = blueprintJson;
            _boardOrigin = boardOrigin;
            _twoByTwoPrefab = twoByTwoPrefab;
            _twoByFourPrefab = twoByFourPrefab;
            _redMaterial = red;
            _whiteMaterial = white;
            _blueMaterial = blue;
            _yellowMaterial = yellow;
        }

        private void Start()
        {
            if (_blueprintJson == null || _boardOrigin == null || _twoByTwoPrefab == null || _twoByFourPrefab == null)
            {
                Fail("missing blueprint, board origin or piece prefab reference");
                return;
            }

            if (!BlueprintLoader.TryLoad(_blueprintJson.text, out var blueprint, out var error))
            {
                Fail(error);
                return;
            }

            Board = new BrickBoard(blueprint);
            // Bottom-up order keeps supply positions stable regardless of JSON array order.
            var ordered = blueprint.bricks.OrderBy(s => s.y).ThenBy(s => s.id, StringComparer.Ordinal).ToArray();
            for (int i = 0; i < ordered.Length; i++)
            {
                var slot = ordered[i];
                string pieceId = "p" + (i + 1).ToString("00");
                var prefab = slot.kind == BrickKinds.TwoByFour ? _twoByFourPrefab : _twoByTwoPrefab;
                var material = MaterialFor(slot.color);
                if (material == null || !Board.TryRegisterPiece(pieceId, slot.kind, slot.color))
                {
                    Fail($"could not register piece {pieceId} ({slot.kind}/{slot.color})");
                    return;
                }

                var piece = Instantiate(prefab, _boardOrigin);
                piece.Initialize(this, pieceId, slot.kind, slot.color, material,
                    BoardPose.ToLocalPosition(SupplyLayout.Center(i, blueprint.boardWidth)), Quaternion.identity);
                _pieces.Add(piece);
            }

            Debug.Log($"[BrickBloom Workbench] Ready | blueprint={blueprint.id} | slots={Board.TotalCount} | pieces={_pieces.Count}");
        }

        // Release transaction for a piece the user let go of. The caller has already ended the XR selection.
        // Turns come from the piece's actual yaw about board up at the moment of release.
        public bool TryPlaceReleased(BrickPiece piece)
        {
            if (Board == null || piece == null || piece.IsPlaced || piece.Workbench != this) return false;
            Vector3 local = piece.transform.localPosition;
            if (!TryReadPose(piece, out int turns))
            {
                Debug.Log($"[BrickBloom Workbench] SnapRejected | piece={piece.PieceId} | reason=Tilted | local=({local.x:F3},{local.y:F3},{local.z:F3})");
                return false;
            }

            if (!_resolver.TrySnap(Board, piece.PieceId, turns, BoardPose.ToBoardPoint(local), out var target))
            {
                Debug.Log($"[BrickBloom Workbench] SnapRejected | piece={piece.PieceId} | reason=NoEligibleTarget | turns={turns} | local=({local.x:F3},{local.y:F3},{local.z:F3})");
                return false;
            }

            piece.PlaceAt(BoardPose.ToLocalPosition(SnapResolver.Center(target)), BoardPose.ToLocalRotation(target.turns));
            Debug.Log($"[BrickBloom Workbench] Snap | piece={piece.PieceId} | slot={target.id} | turns={turns} | placed={Board.PlacedCount}/{Board.TotalCount} | releasedAt=({local.x:F3},{local.y:F3},{local.z:F3})");
            PlacementChanged?.Invoke();
            if (Board.IsComplete) Debug.Log($"[BrickBloom Workbench] Completed | placed={Board.PlacedCount}/{Board.TotalCount}");
            return true;
        }

        // Sends every piece back to the supply area and clears the board. Each piece releases its XR grab
        // before its transform is written.
        public void ResetBuild(string reason)
        {
            if (Board == null) return;
            int cleared = Board.PlacedCount;
            foreach (var piece in _pieces)
            {
                if (piece != null) piece.ResetToTray();
            }

            Board.Reset();
            Debug.Log($"[BrickBloom Workbench] Reset | reason={reason} | cleared={cleared} | placed={Board.PlacedCount}/{Board.TotalCount}");
            PlacementChanged?.Invoke();
        }

        // Read-only preview of where a held piece would snap if released now; null when it would be rejected.
        // Never reserves the slot: release resolves availability again.
        public string PreviewTargetSlotId(BrickPiece piece)
        {
            if (Board == null || piece == null || piece.IsPlaced || piece.Workbench != this) return null;
            if (!TryReadPose(piece, out int turns)) return null;
            var target = _resolver.Resolve(Board, piece.PieceId, turns, BoardPose.ToBoardPoint(piece.transform.localPosition));
            return target?.id;
        }

        private static bool TryReadPose(BrickPiece piece, out int turns)
        {
            Quaternion rotation = piece.transform.localRotation;
            Vector3 up = rotation * Vector3.up;
            Vector3 forward = rotation * Vector3.forward;
            turns = 0;
            if (up.y < MinimumUpDot) return false;
            turns = BrickRules.NearestTurns(Mathf.Atan2(forward.x, forward.z) * Mathf.Rad2Deg);
            return true;
        }

        private Material MaterialFor(string color)
        {
            switch (color)
            {
                case BrickColors.Red: return _redMaterial;
                case BrickColors.White: return _whiteMaterial;
                case BrickColors.Blue: return _blueMaterial;
                case BrickColors.Yellow: return _yellowMaterial;
                default: return null;
            }
        }

        private void Fail(string reason)
        {
            Debug.LogError("[BrickBloom Workbench] Disabled: " + reason, this);
            foreach (var piece in _pieces)
            {
                if (piece != null) Destroy(piece.gameObject);
            }

            _pieces.Clear();
            Board = null;
            enabled = false;
        }
    }
}
