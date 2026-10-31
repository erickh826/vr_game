using System;
using System.Collections.Generic;
using System.Linq;
namespace BrickBloom
{
    // Session-owned, single-threaded model. Call from the Unity main thread.
    public sealed class BrickBoard
    {
        private readonly BrickSlot[] slots;
        private readonly Dictionary<string, PieceDefinition> pieces = new Dictionary<string, PieceDefinition>(StringComparer.Ordinal);
        private readonly Dictionary<string, string> pieceToSlot = new Dictionary<string, string>(StringComparer.Ordinal);
        private readonly Dictionary<string, string> slotToPiece = new Dictionary<string, string>(StringComparer.Ordinal);
        private bool completionReported;
        // Raised after ownership is committed, once per completed build, rearmed by Reset.
        // Subscribers must not throw; presentation should queue work rather than reset here.
        public event Action Completed;
        public int Width { get; }
        public int Depth { get; }
        public int TotalCount => slots.Length;
        public int PlacedCount => slotToPiece.Count;
        public bool IsComplete => PlacedCount == TotalCount;
        public IReadOnlyList<BrickSlot> Slots => Array.AsReadOnly(slots.Select(s => s.Copy()).ToArray());

        public BrickBoard(Blueprint blueprint)
        {
            var errors = BlueprintValidator.Validate(blueprint);
            if (errors.Count != 0) throw new ArgumentException(string.Join("; ", errors), nameof(blueprint));
            Width = blueprint.boardWidth; Depth = blueprint.boardDepth;
            slots = blueprint.bricks.Select(s => s.Copy()).ToArray();
        }
        public bool TryRegisterPiece(string pieceId, string kind, string color)
        {
            if (string.IsNullOrWhiteSpace(pieceId) || !BrickDefinition.IsKind(kind) ||
                !BrickDefinition.IsColor(color) || pieces.ContainsKey(pieceId)) return false;
            pieces.Add(pieceId, new PieceDefinition(pieceId, kind, color));
            return true;
        }
        public bool TryGetPiece(string pieceId, out PieceDefinition piece)
        {
            piece = null;
            return pieceId != null && pieces.TryGetValue(pieceId, out piece);
        }
        public bool TryGetSlotForPiece(string pieceId, out string slotId)
        {
            slotId = null;
            return pieceId != null && pieceToSlot.TryGetValue(pieceId, out slotId);
        }
        public bool TryGetPieceForSlot(string slotId, out string pieceId)
        {
            pieceId = null;
            return slotId != null && slotToPiece.TryGetValue(slotId, out pieceId);
        }
        public bool IsOccupied(string id) => id != null && slotToPiece.ContainsKey(id);
        public bool CanPlace(string pieceId, string slotId, int turns)
        {
            if (!TryGetPiece(pieceId, out var piece) || pieceToSlot.ContainsKey(pieceId)) return false;
            var slot = slots.FirstOrDefault(s => s.id == slotId);
            return slot != null && !slotToPiece.ContainsKey(slotId) &&
                BrickRules.Matches(slot, piece.Kind, piece.Color, turns) &&
                BrickRules.Supported(slot, slots.Where(s => slotToPiece.ContainsKey(s.id)));
        }
        // Logical placement only. Use SnapResolver.TrySnap for proximity-validated release.
        public bool TryPlace(string pieceId, string slotId, int turns)
        {
            if (!CanPlace(pieceId, slotId, turns)) return false;
            pieceToSlot.Add(pieceId, slotId);
            slotToPiece.Add(slotId, pieceId);
            if (IsComplete && !completionReported)
            {
                completionReported = true;
                Completed?.Invoke();
            }
            return true;
        }
        public void Reset()
        {
            pieceToSlot.Clear();
            slotToPiece.Clear();
            completionReported = false;
        }
    }
}
