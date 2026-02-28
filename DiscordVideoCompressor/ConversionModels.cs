namespace DiscordVideoCompressor
{
    internal sealed class SpeedSegment
    {
        public SpeedSegment(double start, double end, double speed)
        {
            Start = start;
            End = end;
            Speed = speed;
        }

        public double Start { get; }

        public double End { get; }

        public double Speed { get; }
    }

    internal sealed class DatamoshSegment
    {
        public DatamoshSegment(double start, double end, double sourceStart, bool applyEffect, int seed)
        {
            Start = start;
            End = end;
            SourceStart = sourceStart;
            ApplyEffect = applyEffect;
            Seed = seed;
        }

        public double Start { get; }

        public double End { get; }

        public double SourceStart { get; }

        public bool ApplyEffect { get; }

        public int Seed { get; }
    }

    internal sealed class DatamoshWindow
    {
        public DatamoshWindow(double start, double end)
        {
            Start = start;
            End = end;
        }

        public double Start { get; }

        public double End { get; }
    }
}
