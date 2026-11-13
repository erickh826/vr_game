using System;
using UnityEngine;

namespace BrickBloom.Presentation
{
    // Parses schema 1 blueprint JSON and validates it before any board is constructed.
    public static class BlueprintLoader
    {
        public static bool TryLoad(string json, out Blueprint blueprint, out string error)
        {
            blueprint = null;
            if (string.IsNullOrWhiteSpace(json))
            {
                error = "Blueprint JSON is empty.";
                return false;
            }

            Blueprint parsed;
            try
            {
                parsed = JsonUtility.FromJson<Blueprint>(json);
            }
            catch (ArgumentException ex)
            {
                error = "Blueprint JSON is malformed: " + ex.Message;
                return false;
            }

            var errors = BlueprintValidator.Validate(parsed);
            if (errors.Count != 0)
            {
                error = "Blueprint is invalid: " + string.Join("; ", errors);
                return false;
            }

            blueprint = parsed;
            error = null;
            return true;
        }
    }
}
