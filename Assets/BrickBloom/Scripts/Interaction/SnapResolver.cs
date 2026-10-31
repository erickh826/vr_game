using System;
namespace BrickBloom
{
    public readonly struct BoardPoint
    {
        public double X { get; }
        public double Y { get; }
        public double Z { get; }
        public BoardPoint(double x, double y, double z) { X = x; Y = y; Z = z; }
        public bool IsFinite => Finite(X) && Finite(Y) && Finite(Z);
        private static bool Finite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);
    }

    public sealed class SnapResolver
    {
        public double HorizontalTolerance { get; }
        public double VerticalTolerance { get; }
        public SnapResolver(double horizontalTolerance = 0.035, double verticalTolerance = 0.032)
        {
            if (!new BoardPoint(horizontalTolerance, verticalTolerance, 0).IsFinite ||
                horizontalTolerance < 0 || verticalTolerance < 0) throw new ArgumentOutOfRangeException();
            HorizontalTolerance = horizontalTolerance; VerticalTolerance = verticalTolerance;
        }
        // Center of body; board origin is its lower X/Z corner at the top of the board.
        public static BoardPoint Center(BrickSlot slot) => new BoardPoint(
            (slot.x + BrickDefinition.Width(slot) / 2.0) * BrickDefinition.StudPitch,
            (slot.y + 0.5) * BrickDefinition.LayerHeight,
            (slot.z + BrickDefinition.Depth(slot) / 2.0) * BrickDefinition.StudPitch);

        public BrickSlot Resolve(BrickBoard board, string pieceId, int turns, BoardPoint position)
        {
            if (board == null || !position.IsFinite || !board.TryGetPiece(pieceId, out _) ||
                board.TryGetSlotForPiece(pieceId, out _)) return null;
            BrickSlot best = null;
            double bestDistance = double.PositiveInfinity;
            foreach (var slot in board.Slots)
            {
                if (!board.CanPlace(pieceId, slot.id, turns)) continue;
                var target = Center(slot);
                double dx = position.X - target.X, dy = position.Y - target.Y, dz = position.Z - target.Z;
                double horizontal = dx * dx + dz * dz;
                if (horizontal > HorizontalTolerance * HorizontalTolerance || Math.Abs(dy) > VerticalTolerance) continue;
                double distance = horizontal + dy * dy;
                if (distance < bestDistance || (distance == bestDistance &&
                    (best == null || string.CompareOrdinal(slot.id, best.id) < 0)))
                { best = slot; bestDistance = distance; }
            }
            return best;
        }
        public bool TrySnap(BrickBoard board, string pieceId, int turns, BoardPoint position, out BrickSlot target)
        {
            target = Resolve(board, pieceId, turns, position);
            if (target != null && board.TryPlace(pieceId, target.id, turns)) return true;
            target = null;
            return false;
        }
    }
}
