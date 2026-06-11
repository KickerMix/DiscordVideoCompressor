using System;
using System.Collections.Generic;

namespace DiscordVideoCompressor
{
    internal static class ConversionAlgorithms
    {
        private const double ContainerSafetyFactor = 0.98;

        public static int SelectAudioBitrate(long targetBitrate)
        {
            int[] options = { 32000, 48000, 64000, 96000, 128000 };
            long desired = Math.Max(32000, Math.Min(128000, targetBitrate / 6));
            int closest = options[0];

            foreach (int option in options)
            {
                if (Math.Abs(option - desired) < Math.Abs(closest - desired))
                {
                    closest = option;
                }
            }

            return closest;
        }

        public static long CalculateVideoBitrate(long targetSizeBytes, double durationSeconds, int audioBitrate)
        {
            if (targetSizeBytes <= 0 || durationSeconds <= 0)
            {
                return 0;
            }

            double totalBitrate = targetSizeBytes * 8.0 / durationSeconds * ContainerSafetyFactor;
            if (double.IsNaN(totalBitrate) || double.IsInfinity(totalBitrate) || totalBitrate > long.MaxValue)
            {
                return 0;
            }

            return Math.Max(0, (long)totalBitrate - Math.Max(0, audioBitrate));
        }

        public static int NormalizeAudioSampleRate(string outputFormat, int requestedSampleRate)
        {
            int fallback = requestedSampleRate > 0 ? requestedSampleRate : 44100;
            if (!string.Equals(outputFormat, "webm", StringComparison.OrdinalIgnoreCase))
            {
                return fallback;
            }

            int[] opusSampleRates = { 8000, 12000, 16000, 24000, 48000 };
            int closest = opusSampleRates[0];
            foreach (int sampleRate in opusSampleRates)
            {
                if (Math.Abs(sampleRate - fallback) < Math.Abs(closest - fallback))
                {
                    closest = sampleRate;
                }
            }

            return closest;
        }

        public static List<SpeedSegment> BuildSpeedSegments(double duration)
        {
            var segments = new List<SpeedSegment>();
            if (duration <= 0)
            {
                return segments;
            }

            const double minSpeed = 0.6;
            const double maxSpeed = 1.4;
            const double cycles = 2.0;

            double period = duration / cycles;
            if (period <= 0.0)
            {
                return segments;
            }

            int segmentCount = (int)Math.Round(duration * 2);
            if (segmentCount < 16)
            {
                segmentCount = 16;
            }

            if (segmentCount > 80)
            {
                segmentCount = 80;
            }

            double step = duration / segmentCount;
            double center = (minSpeed + maxSpeed) / 2.0;
            double amplitude = (maxSpeed - minSpeed) / 2.0;

            for (int i = 0; i < segmentCount; i++)
            {
                double start = i * step;
                double end = Math.Min(duration, (i + 1) * step);
                double mid = (start + end) / 2.0;
                double speed = center + amplitude * Math.Sin(2.0 * Math.PI * mid / period);

                if (speed < 0.1)
                {
                    speed = 0.1;
                }

                segments.Add(new SpeedSegment(start, end, speed));
            }

            return segments;
        }

        public static List<DatamoshSegment> BuildDatamoshSegments(double duration, int seed, double jumpLengthSeconds, double glitchChance)
        {
            var segments = new List<DatamoshSegment>();
            if (duration <= 0)
            {
                return segments;
            }

            var rng = new Random(seed);
            double baseLength = jumpLengthSeconds <= 0 ? duration / 25.0 : jumpLengthSeconds;
            if (baseLength < 0.2)
            {
                baseLength = 0.2;
            }

            if (baseLength > duration)
            {
                baseLength = duration;
            }

            double chance = Math.Max(0.0, Math.Min(1.0, glitchChance));
            double current = 0;

            while (current < duration)
            {
                double jitter = 0.8 + rng.NextDouble() * 0.9;
                double length = Math.Min(duration - current, baseLength * jitter);
                if (length < 0.1)
                {
                    length = Math.Min(duration - current, 0.1);
                }

                bool applyEffect = rng.NextDouble() < chance;
                int segmentSeed = rng.Next(1, int.MaxValue);
                double sourceStart = current;
                if (applyEffect)
                {
                    double maxStart = Math.Max(0.0, duration - length);
                    sourceStart = rng.NextDouble() * maxStart;
                    if (Math.Abs(sourceStart - current) < Math.Min(0.2, length * 0.2))
                    {
                        double offset = Math.Min(length, maxStart);
                        sourceStart = Math.Min(maxStart, sourceStart + offset);
                    }
                }

                segments.Add(new DatamoshSegment(current, current + length, sourceStart, applyEffect, segmentSeed));
                current += length;
            }

            bool hasEffect = false;
            for (int i = 0; i < segments.Count; i++)
            {
                if (segments[i].ApplyEffect)
                {
                    hasEffect = true;
                    break;
                }
            }

            if (!hasEffect && segments.Count > 0 && chance > 0.0)
            {
                int index = rng.Next(segments.Count);
                DatamoshSegment segment = segments[index];
                double length = segment.End - segment.Start;
                double maxStart = Math.Max(0.0, duration - length);
                double sourceStart = rng.NextDouble() * maxStart;
                if (Math.Abs(sourceStart - segment.Start) < Math.Min(0.2, length * 0.2))
                {
                    double offset = Math.Min(length, maxStart);
                    sourceStart = Math.Min(maxStart, sourceStart + offset);
                }

                segments[index] = new DatamoshSegment(segment.Start, segment.End, sourceStart, true, rng.Next(1, int.MaxValue));
            }

            return segments;
        }
    }
}
