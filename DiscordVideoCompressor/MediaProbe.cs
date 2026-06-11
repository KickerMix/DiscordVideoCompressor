using System;
using System.Globalization;
using System.Text.RegularExpressions;

namespace DiscordVideoCompressor
{
    internal sealed class MediaProbeResult
    {
        public MediaProbeResult(double duration, bool hasAudio, int audioSampleRate)
        {
            Duration = duration;
            HasAudio = hasAudio;
            AudioSampleRate = audioSampleRate;
        }

        public double Duration { get; }

        public bool HasAudio { get; }

        public int AudioSampleRate { get; }
    }

    internal static class MediaProbe
    {
        private static readonly Regex DurationRegex = new Regex(
            @"Duration:\s*(\d+):(\d+):(\d+(?:[\.,]\d+)?)",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);

        private static readonly Regex AudioRegex = new Regex(
            @"Audio:\s*([^,\s]+).*?(\d+)\s*Hz",
            RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

        public static bool TryParse(string output, int defaultAudioSampleRate, out MediaProbeResult result)
        {
            result = null;
            if (string.IsNullOrWhiteSpace(output))
            {
                return false;
            }

            Match durationMatch = DurationRegex.Match(output);
            if (!durationMatch.Success ||
                !int.TryParse(durationMatch.Groups[1].Value, NumberStyles.None, CultureInfo.InvariantCulture, out int hours) ||
                !int.TryParse(durationMatch.Groups[2].Value, NumberStyles.None, CultureInfo.InvariantCulture, out int minutes) ||
                !double.TryParse(durationMatch.Groups[3].Value.Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out double seconds))
            {
                return false;
            }

            double duration = hours * 3600.0 + minutes * 60.0 + seconds;
            if (duration <= 0)
            {
                return false;
            }

            Match audioMatch = AudioRegex.Match(output);
            bool hasAudio = audioMatch.Success;
            int audioSampleRate = defaultAudioSampleRate;
            if (hasAudio &&
                int.TryParse(audioMatch.Groups[2].Value, NumberStyles.None, CultureInfo.InvariantCulture, out int parsedSampleRate) &&
                parsedSampleRate > 0)
            {
                audioSampleRate = parsedSampleRate;
            }

            result = new MediaProbeResult(duration, hasAudio, audioSampleRate);
            return true;
        }
    }
}
