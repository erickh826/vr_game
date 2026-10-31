using System;
namespace BrickBloom
{
    // Serialization DTO only. BrickBoard takes an immutable snapshot before use.
    [Serializable]
    public sealed class Blueprint
    {
        public int schemaVersion;
        public string id;
        public int boardWidth;
        public int boardDepth;
        public BrickSlot[] bricks;
    }
}
