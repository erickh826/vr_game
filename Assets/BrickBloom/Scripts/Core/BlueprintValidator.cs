using System;
using System.Collections.Generic;
using System.Linq;
namespace BrickBloom
{
    public static class BlueprintValidator
    {
        public static IReadOnlyList<string> Validate(Blueprint blueprint)
        {
            var errors = new List<string>();
            if (blueprint == null) { errors.Add("Blueprint is missing."); return errors; }
            if (blueprint.schemaVersion != 1) errors.Add("Unsupported schemaVersion.");
            if (string.IsNullOrWhiteSpace(blueprint.id)) errors.Add("Blueprint id is required.");
            if (blueprint.boardWidth <= 0 || blueprint.boardDepth <= 0) errors.Add("Board dimensions must be positive.");
            if (blueprint.bricks == null || blueprint.bricks.Length == 0)
            { errors.Add("At least one brick is required."); return errors; }
            var ids = new HashSet<string>(StringComparer.Ordinal);
            foreach (var slot in blueprint.bricks)
            {
                if (slot == null) { errors.Add("Null brick entry."); continue; }
                if (string.IsNullOrWhiteSpace(slot.id) || !ids.Add(slot.id)) errors.Add("Missing or duplicate brick id.");
                if (!BrickDefinition.IsKind(slot.kind)) errors.Add($"{slot.id}: invalid kind.");
                if (!BrickDefinition.IsColor(slot.color)) errors.Add($"{slot.id}: invalid color.");
                if (!BrickRules.ValidTurns(slot.turns)) errors.Add($"{slot.id}: invalid turns.");
                if (slot.x < 0 || slot.z < 0 || slot.y < 0 ||
                    (long)slot.x + BrickDefinition.Width(slot) > blueprint.boardWidth ||
                    (long)slot.z + BrickDefinition.Depth(slot) > blueprint.boardDepth)
                    errors.Add($"{slot.id}: out of bounds.");
            }
            if (errors.Count != 0) return errors;
            var slots = blueprint.bricks;
            for (int i = 0; i < slots.Length; i++)
                for (int j = i + 1; j < slots.Length; j++)
                    if (BrickRules.Overlaps(slots[i], slots[j])) errors.Add($"{slots[i].id}/{slots[j].id}: overlap.");
            var built = new List<BrickSlot>();
            foreach (var slot in slots.OrderBy(s => s.y).ThenBy(s => s.id, StringComparer.Ordinal))
            {
                if (!BrickRules.Supported(slot, built))
                    errors.Add($"{slot.id}: unsupported; no bottom-up build sequence.");
                else built.Add(slot);
            }
            return errors;
        }
    }
}
