using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace DiscordVideoCompressor
{
    internal static class FilterGraphBuilder
    {
        public static string BuildAudioFilter(string audioBitDepth)
        {
            var filters = new List<string>();
            if (!string.IsNullOrWhiteSpace(audioBitDepth))
            {
                filters.Add($"aformat=sample_fmts={audioBitDepth}");
            }

            filters.Add("anlmdn");
            return string.Join(",", filters);
        }

        public static string BuildVideoFilter(string resolution)
        {
            if (!TryParseResolution(resolution, out int width, out int height))
            {
                return string.Empty;
            }

            width = (width / 2) * 2;
            height = (height / 2) * 2;
            if (width < 2 || height < 2)
            {
                return string.Empty;
            }

            return $"scale={width}:{height}:force_original_aspect_ratio=decrease,pad={width}:{height}:(ow-iw)/2:(oh-ih)/2";
        }

        public static string BuildSpeedFilterComplex(double duration, string resolution, string audioBitDepth, int outputAudioSampleRate, int inputAudioSampleRate, int videoFps, string extraVideoFilter, bool includeAudio, out string videoOutLabel, out string audioOutLabel)
        {
            List<SpeedSegment> segments = ConversionAlgorithms.BuildSpeedSegments(duration);
            var builder = new StringBuilder();
            string scaleFilter = BuildVideoFilter(resolution);

            for (int i = 0; i < segments.Count; i++)
            {
                SpeedSegment segment = segments[i];
                int adjustedRate = (int)Math.Round(inputAudioSampleRate * segment.Speed);
                adjustedRate = Math.Max(1000, Math.Min(192000, adjustedRate));
                double effectiveSpeed = inputAudioSampleRate > 0 ? adjustedRate / (double)inputAudioSampleRate : segment.Speed;

                builder.AppendFormat(CultureInfo.InvariantCulture, "[0:v]trim=start={0}:end={1},setpts=PTS-STARTPTS,setpts=PTS/{2}[v{3}];", segment.Start, segment.End, effectiveSpeed, i);
                if (includeAudio)
                {
                    builder.AppendFormat(CultureInfo.InvariantCulture, "[0:a]atrim=start={0}:end={1},asetpts=PTS-STARTPTS,asetrate={2},aresample={3}[a{4}];", segment.Start, segment.End, adjustedRate, outputAudioSampleRate, i);
                }
            }

            for (int i = 0; i < segments.Count; i++)
            {
                builder.AppendFormat("[v{0}]", i);
                if (includeAudio)
                {
                    builder.AppendFormat("[a{0}]", i);
                }
            }

            builder.AppendFormat(includeAudio ? "concat=n={0}:v=1:a=1[vtmp][atmp];" : "concat=n={0}:v=1:a=0[vtmp];", segments.Count);

            var videoPostFilters = new List<string>();
            if (!string.IsNullOrWhiteSpace(scaleFilter))
            {
                videoPostFilters.Add(scaleFilter);
            }

            if (videoFps > 0)
            {
                videoPostFilters.Add($"fps=fps={videoFps}");
            }

            if (!string.IsNullOrWhiteSpace(extraVideoFilter))
            {
                videoPostFilters.Add(extraVideoFilter);
            }

            builder.Append(videoPostFilters.Count > 0
                ? $"[vtmp]{string.Join(",", videoPostFilters)}[vout];"
                : "[vtmp]null[vout];");

            if (includeAudio)
            {
                builder.AppendFormat("[atmp]{0}[aout]", BuildAudioPostFilter(audioBitDepth));
                audioOutLabel = "[aout]";
            }
            else
            {
                audioOutLabel = null;
            }

            videoOutLabel = "[vout]";
            return builder.ToString().TrimEnd(';');
        }

        public static string BuildDatamoshFilterComplex(string resolution, string audioBitDepth, int outputAudioSampleRate, int videoFps, List<DatamoshSegment> segments, out string videoOutLabel, out string audioOutLabel)
        {
            if (segments == null || segments.Count == 0)
            {
                videoOutLabel = "[vout]";
                audioOutLabel = "[aout]";
                return "[0:v]null[vout];[0:a]anull[aout]";
            }

            var builder = new StringBuilder();
            string scaleFilter = BuildVideoFilter(resolution);

            for (int i = 0; i < segments.Count; i++)
            {
                DatamoshSegment segment = segments[i];
                string videoFilters = $"trim=start={segment.SourceStart.ToString(CultureInfo.InvariantCulture)}:end={(segment.SourceStart + (segment.End - segment.Start)).ToString(CultureInfo.InvariantCulture)},setpts=PTS-STARTPTS";
                if (segment.ApplyEffect)
                {
                    videoFilters += "," + BuildDatamoshVideoFilter(segment.Seed);
                }

                builder.AppendFormat(CultureInfo.InvariantCulture, "[0:v]{0}[v{1}];", videoFilters, i);
                string audioFilters = $"atrim=start={segment.SourceStart.ToString(CultureInfo.InvariantCulture)}:end={(segment.SourceStart + (segment.End - segment.Start)).ToString(CultureInfo.InvariantCulture)},asetpts=PTS-STARTPTS";
                builder.AppendFormat(CultureInfo.InvariantCulture, "[0:a]{0}[a{1}];", audioFilters, i);
            }

            for (int i = 0; i < segments.Count; i++)
            {
                builder.AppendFormat("[v{0}][a{0}]", i);
            }

            builder.AppendFormat("concat=n={0}:v=1:a=1[vtmp][atmp];", segments.Count);
            var videoPostFilters = new List<string>();
            if (!string.IsNullOrWhiteSpace(scaleFilter))
            {
                videoPostFilters.Add(scaleFilter);
            }

            if (videoFps > 0)
            {
                videoPostFilters.Add($"fps=fps={videoFps}");
            }

            builder.Append(videoPostFilters.Count > 0
                ? $"[vtmp]{string.Join(",", videoPostFilters)}[vout];"
                : "[vtmp]null[vout];");

            builder.AppendFormat("[atmp]{0}[aout]", BuildAudioPostFilter(audioBitDepth));
            videoOutLabel = "[vout]";
            audioOutLabel = "[aout]";
            return builder.ToString().TrimEnd(';');
        }

        private static string BuildAudioPostFilter(string audioBitDepth)
        {
            var filters = new List<string>();
            if (!string.IsNullOrWhiteSpace(audioBitDepth))
            {
                filters.Add($"aformat=sample_fmts={audioBitDepth}");
            }

            filters.Add("anlmdn");
            return string.Join(",", filters);
        }

        private static string BuildDatamoshVideoFilter(int seed)
        {
            return $"random=frames=30:seed={seed},tmix=frames=5:weights='1 1 1 1 1',tblend=all_mode=average";
        }

        private static bool TryParseResolution(string resolution, out int width, out int height)
        {
            width = 0;
            height = 0;
            if (string.IsNullOrWhiteSpace(resolution))
            {
                return false;
            }

            string[] parts = resolution.ToLowerInvariant().Split('x');
            if (parts.Length != 2)
            {
                return false;
            }

            return int.TryParse(parts[0], out width) && int.TryParse(parts[1], out height);
        }
    }
}
