using System.Collections.Generic;
namespace BrickBloom
{
    public static class BrickRules
    {
        public static bool ValidTurns(int turns) => turns >= 0 && turns <= 3;
        public static bool EquivalentRotation(string kind, int a, int b) =>
            BrickDefinition.IsKind(kind) && ValidTurns(a) && ValidTurns(b) &&
            (kind == "2x2" || a % 2 == b % 2);

        public static bool Matches(BrickSlot target, string kind, string color, int turns) =>
            target != null && target.kind == kind && target.color == color &&
            EquivalentRotation(kind, target.turns, turns);

        public static bool Covers(BrickSlot slot, int x, int z) =>
            x >= slot.x && (long)x < (long)slot.x + BrickDefinition.Width(slot) &&
            z >= slot.z && (long)z < (long)slot.z + BrickDefinition.Depth(slot);

        public static bool Overlaps(BrickSlot a, BrickSlot b) => a.y == b.y &&
            a.x < (long)b.x + BrickDefinition.Width(b) && b.x < (long)a.x + BrickDefinition.Width(a) &&
            a.z < (long)b.z + BrickDefinition.Depth(b) && b.z < (long)a.z + BrickDefinition.Depth(a);

        // Every footprint cell must rest on ground or a placed brick one layer below.
        public static bool Supported(BrickSlot slot, IEnumerable<BrickSlot> placed)
        {
            if (slot.y == 0) return true;
            if (slot.y < 0) return false;
            for (int dx = 0; dx < BrickDefinition.Width(slot); dx++)
                for (int dz = 0; dz < BrickDefinition.Depth(slot); dz++)
                {
                    bool found = false;
                    foreach (var support in placed)
                        if (support.y == slot.y - 1 && Covers(support, slot.x + dx, slot.z + dz))
                        { found = true; break; }
                    if (!found) return false;
                }
            return true;
        }
    }
}
