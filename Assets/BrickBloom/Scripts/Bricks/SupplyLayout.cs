using System;
namespace BrickBloom
{
    // Deterministic supply (tray) positions beside the board, in the same board-local frame as SnapResolver.Center.
    // Every supply piece rests at layer 0 with turns 0, so its footprint is at most 2 studs along X and 4 along Z.
    public static class SupplyLayout
    {
        public const int Columns = 3;
        public const double Margin = 0.06;
        public const double ColumnSpacing = 0.072;
        public const double RowSpacing = 0.12;

        public static BoardPoint Center(int index, int boardWidth)
        {
            if (index < 0) throw new ArgumentOutOfRangeException(nameof(index));
            if (boardWidth <= 0) throw new ArgumentOutOfRangeException(nameof(boardWidth));
            int column = index % Columns, row = index / Columns;
            return new BoardPoint(
                boardWidth * BrickDefinition.StudPitch + Margin + BrickDefinition.StudPitch + column * ColumnSpacing,
                0.5 * BrickDefinition.LayerHeight,
                2 * BrickDefinition.StudPitch + row * RowSpacing);
        }
    }
}
