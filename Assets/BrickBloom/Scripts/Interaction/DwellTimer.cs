using System;
namespace BrickBloom
{
    // Hold-to-confirm timer for touch buttons. Fires once per continuous touch; the touch must end to rearm.
    public sealed class DwellTimer
    {
        private double held;
        public double Duration { get; }
        // True after firing until the touch ends.
        public bool IsLatched { get; private set; }
        public double Progress => IsLatched ? 1.0 : Math.Min(1.0, held / Duration);

        public DwellTimer(double durationSeconds)
        {
            if (double.IsNaN(durationSeconds) || double.IsInfinity(durationSeconds) || durationSeconds <= 0.0)
                throw new ArgumentOutOfRangeException(nameof(durationSeconds));
            Duration = durationSeconds;
        }

        // Returns true exactly once, on the tick where the continuous touch reaches the duration.
        public bool Tick(bool touching, double deltaSeconds)
        {
            if (!touching)
            {
                Clear();
                return false;
            }

            if (IsLatched) return false;
            if (!double.IsNaN(deltaSeconds) && !double.IsInfinity(deltaSeconds) && deltaSeconds > 0.0) held += deltaSeconds;
            if (held < Duration) return false;
            IsLatched = true;
            return true;
        }

        public void Clear()
        {
            held = 0.0;
            IsLatched = false;
        }
    }
}
