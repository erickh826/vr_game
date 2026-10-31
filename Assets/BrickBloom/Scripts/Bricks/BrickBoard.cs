using System;
using System.Collections.Generic;
using System.Linq;
namespace BrickBloom
{
    // Session-owned model. Consumers cannot mutate its target or occupancy state.
    public sealed class BrickBoard
    {
        private readonly BrickSlot[] slots;
        private readonly HashSet<string> occupied = new HashSet<string>(StringComparer.Ordinal);
        public int Width { get; }
        public int Depth { get; }
        public int TotalCount => slots.Length;
        public int PlacedCount => occupied.Count;
        public bool IsComplete => PlacedCount == TotalCount;
        public IReadOnlyList<BrickSlot> Slots => Array.AsReadOnly(slots.Select(s => s.Copy()).ToArray());

        public BrickBoard(Blueprint blueprint)
        {
            var errors = BlueprintValidator.Validate(blueprint);
            if (errors.Count != 0) throw new ArgumentException(string.Join("; ", errors), nameof(blueprint));
            Width = blueprint.boardWidth; Depth = blueprint.boardDepth;
            slots = blueprint.bricks.Select(s => s.Copy()).ToArray();
        }
        public bool IsOccupied(string id) => id != null && occupied.Contains(id);
        public bool CanPlace(string id, string kind, string color, int turns)
        {
            var slot = slots.FirstOrDefault(s => s.id == id);
            return slot != null && !occupied.Contains(id) && BrickRules.Matches(slot, kind, color, turns) &&
                BrickRules.Supported(slot, slots.Where(s => occupied.Contains(s.id)));
        }
        // Logical placement only. Use SnapResolver.TrySnap for proximity-validated release.
        public bool TryPlace(string id, string kind, string color, int turns) =>
            CanPlace(id, kind, color, turns) && occupied.Add(id);
        public void Reset() => occupied.Clear();
    }
}
