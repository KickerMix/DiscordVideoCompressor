namespace DiscordVideoCompressor
{
    internal sealed class ConversionOptions
    {
        public long TargetSizeBytes { get; init; }

        public string OutputFormat { get; init; }

        public int AudioSampleRate { get; init; }

        public string AudioBitDepth { get; init; }

        public string VideoResolution { get; init; }

        public int VideoFps { get; init; }

        public bool EnableSpeedEffect { get; init; }

        public bool EnableDatamosh { get; init; }

        public bool EnableGlitchEffect { get; init; }

        public double GlitchJumpSeconds { get; init; }

        public double GlitchChance { get; init; }
    }
}
