using System;
namespace BrickBloom
{
    [Serializable]
    public sealed class BrickSlot
    {
        public string id;
        public string kind;
        public string color;
        public int x, y, z, turns;

        public BrickSlot Copy() => (BrickSlot)MemberwiseClone();
    }
}
