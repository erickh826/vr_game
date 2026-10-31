namespace BrickBloom
{
    public static class BrickDefinition
    {
        public const double StudPitch = 0.024;
        public const double LayerHeight = 0.029;
        public static bool IsKind(string kind) => kind == BrickKinds.TwoByTwo || kind == BrickKinds.TwoByFour;
        public static bool IsColor(string color) =>
            color == BrickColors.Red || color == BrickColors.White || color == BrickColors.Blue || color == BrickColors.Yellow;
        public static int Width(BrickSlot slot) => slot.kind == BrickKinds.TwoByFour && slot.turns % 2 == 1 ? 4 : 2;
        public static int Depth(BrickSlot slot) => slot.kind == BrickKinds.TwoByFour && slot.turns % 2 == 0 ? 4 : 2;
    }
}
