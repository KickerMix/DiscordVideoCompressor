using System.Collections.Generic;

namespace DiscordVideoCompressor
{
    public partial class Form1
    {
        internal int SelectAudioBitrate(long targetBitrate)
        {
            return ConversionAlgorithms.SelectAudioBitrate(targetBitrate);
        }

        internal string BuildAudioFilter(string audioBitDepth)
        {
            return FilterGraphBuilder.BuildAudioFilter(audioBitDepth);
        }

        internal string BuildVideoFilter(string resolution)
        {
            return FilterGraphBuilder.BuildVideoFilter(resolution);
        }

        internal string BuildSpeedFilterComplex(double duration, string resolution, string audioBitDepth, int outputAudioSampleRate, int inputAudioSampleRate, int videoFps, string extraVideoFilter, bool includeAudio, out string videoOutLabel, out string audioOutLabel)
        {
            return FilterGraphBuilder.BuildSpeedFilterComplex(duration, resolution, audioBitDepth, outputAudioSampleRate, inputAudioSampleRate, videoFps, extraVideoFilter, includeAudio, out videoOutLabel, out audioOutLabel);
        }

        internal string BuildDatamoshFilterComplex(string resolution, string audioBitDepth, int outputAudioSampleRate, int videoFps, List<DatamoshSegment> segments, bool includeAudio, out string videoOutLabel, out string audioOutLabel)
        {
            return FilterGraphBuilder.BuildDatamoshFilterComplex(resolution, audioBitDepth, outputAudioSampleRate, videoFps, segments, includeAudio, out videoOutLabel, out audioOutLabel);
        }

        internal List<DatamoshSegment> BuildDatamoshSegments(double duration, int seed, double jumpLengthSeconds, double glitchChance)
        {
            return ConversionAlgorithms.BuildDatamoshSegments(duration, seed, jumpLengthSeconds, glitchChance);
        }
    }
}
