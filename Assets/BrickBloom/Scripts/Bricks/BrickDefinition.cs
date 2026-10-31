namespace BrickBloom
{
    public static class BrickDefinition
    {
        public const double StudPitch = 0.024;
        public const double LayerHeight = 0.029;
        public static bool IsKind(string kind) => kind == "2x2" || kind == "2x4";
        public static bool IsColor(string color) =>
            color == "red" || color == "white" || color == "blue" || color == "yellow";
        public static int Width(BrickSlot slot) => slot.kind == "2x4" && slot.turns % 2 == 1 ? 4 : 2;
        public static int Depth(BrickSlot slot) => slot.kind == "2x4" && slot.turns % 2 == 0 ? 4 : 2;
    }
}
