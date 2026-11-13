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

        private readonly List<BrickPiece> _pieces = new List<BrickPiece>();

        public BrickBoard Board { get; private set; }
        public Transform BoardOrigin => _boardOrigin;
        public IReadOnlyList<BrickPiece> Pieces => _pieces;

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
                piece.Initialize(pieceId, slot.kind, slot.color, material,
                    BoardPose.ToLocalPosition(SupplyLayout.Center(i, blueprint.boardWidth)), Quaternion.identity);
                _pieces.Add(piece);
            }

            Debug.Log($"[BrickBloom Workbench] Ready | blueprint={blueprint.id} | slots={Board.TotalCount} | pieces={_pieces.Count}");
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
