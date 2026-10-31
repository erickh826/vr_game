namespace BrickBloom
{
    // Physical inventory identity, distinct from a blueprint target slot ID.
    public sealed class PieceDefinition
    {
        public string Id { get; }
        public string Kind { get; }
        public string Color { get; }

        internal PieceDefinition(string id, string kind, string color)
        {
            Id = id;
            Kind = kind;
            Color = color;
        }
    }
}
