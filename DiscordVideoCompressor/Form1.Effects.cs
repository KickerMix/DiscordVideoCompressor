using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

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

        private string BuildAudioPostFilter(string audioBitDepth)
        {
            var filters = new List<string>();

            if (!string.IsNullOrWhiteSpace(audioBitDepth))
            {
                filters.Add($"aformat=sample_fmts={audioBitDepth}");
            }

            filters.Add("anlmdn");
            return string.Join(",", filters);
        }

        internal bool RunTrueDatamoshPipeline(string ffmpegPath, string inputFile, string outputFileTemp, long videoBitrate, int audioBitrate, int audioSampleRate, string audioBitDepth, string videoResolution, int videoFps, double duration, int seed, double stageStart, double stageSpan, CancellationToken token, out StringBuilder outputLog, out bool outputHasAudio)
        {
            throw new NotSupportedException("Datamosh pipeline moved to FfmpegConversionService.");
        }

        private bool ProbeHasAudio(string ffmpegPath, string filePath, out string probeLog)
        {
            probeLog = string.Empty;

            try
            {
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = ffmpegPath,
                        Arguments = $"-i \"{filePath}\" -hide_banner",
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };

                process.Start();
                string output = process.StandardError.ReadToEnd();
                process.WaitForExit();
                probeLog = output;
                return Regex.IsMatch(output, @"Audio:\s", RegexOptions.IgnoreCase);
            }
            catch (Exception ex)
            {
                probeLog = "Probe failed: " + ex.Message;
                return false;
            }
        }

        private int GetDatamoshGop(int videoFps)
        {
            int fps = videoFps > 0 ? videoFps : 60;
            int gop = fps;
            if (gop < 15)
            {
                gop = 15;
            }

            if (gop > 120)
            {
                gop = 120;
            }

            return gop;
        }

        private bool ApplyTrueDatamosh(string inputPath, string outputPath, List<DatamoshWindow> windows, int videoFps, int seed, bool maxAggressive, CancellationToken token, out string error)
        {
            error = string.Empty;
            byte[] data;

            try
            {
                data = File.ReadAllBytes(inputPath);
            }
            catch (Exception ex)
            {
                error = "Failed to read datamosh source: " + ex.Message;
                return false;
            }

            List<NalStart> nalStarts = FindNalStartCodes(data);
            if (nalStarts.Count == 0)
            {
                error = "Datamosh failed: no NAL units found.";
                return false;
            }

            int fps = videoFps > 0 ? videoFps : 60;
            var rng = new Random(seed);
            int frameIndex = 0;
            int idrIndex = 0;
            int windowIndex = 0;
            int convertedIdr = 0;
            var idrHeaders = new List<int>();

            for (int i = 0; i < nalStarts.Count; i++)
            {
                token.ThrowIfCancellationRequested();

                NalStart current = nalStarts[i];
                int nextStart = i + 1 < nalStarts.Count ? nalStarts[i + 1].Index : data.Length;
                int headerIndex = current.Index + current.Length;
                if (headerIndex >= nextStart)
                {
                    continue;
                }

                byte nalHeader = data[headerIndex];
                int nalType = nalHeader & 0x1F;

                if (nalType == 1 || nalType == 5)
                {
                    double timeSeconds = frameIndex / (double)fps;
                    while (windowIndex < windows.Count && timeSeconds > windows[windowIndex].End)
                    {
                        windowIndex++;
                    }

                    bool inWindow = windowIndex < windows.Count && timeSeconds >= windows[windowIndex].Start && timeSeconds <= windows[windowIndex].End;
                    if (maxAggressive)
                    {
                        inWindow = true;
                    }

                    if (nalType == 5)
                    {
                        if (idrIndex > 0)
                        {
                            idrHeaders.Add(headerIndex);
                        }

                        bool shouldMosh = idrIndex > 0 && inWindow;
                        if (shouldMosh)
                        {
                            data[headerIndex] = (byte)((nalHeader & 0xE0) | 0x01);
                            convertedIdr++;
                        }

                        idrIndex++;
                    }

                    frameIndex++;
                }
            }

            if (convertedIdr == 0 && idrHeaders.Count > 0)
            {
                int pick = idrHeaders[rng.Next(idrHeaders.Count)];
                data[pick] = (byte)((data[pick] & 0xE0) | 0x01);
            }

            try
            {
                File.WriteAllBytes(outputPath, data);
                return true;
            }
            catch (Exception ex)
            {
                error = "Failed to write datamosh output: " + ex.Message;
                return false;
            }
        }

        private List<DatamoshWindow> BuildDatamoshWindows(double duration, int seed)
        {
            var windows = new List<DatamoshWindow>();
            if (duration <= 0)
            {
                return windows;
            }

            var rng = new Random(seed);
            double current = 0;

            while (current < duration)
            {
                double gap = 0.1 + rng.NextDouble() * 0.4;
                current += gap;
                if (current >= duration)
                {
                    break;
                }

                double length = 0.9 + rng.NextDouble() * 1.3;
                double end = Math.Min(duration, current + length);
                windows.Add(new DatamoshWindow(current, end));
                current = end;
            }

            if (windows.Count == 0 && duration > 0.2)
            {
                windows.Add(new DatamoshWindow(0.1, Math.Min(duration, 0.7)));
            }

            return windows;
        }

        private struct NalStart
        {
            public NalStart(int index, int length)
            {
                Index = index;
                Length = length;
            }

            public int Index { get; }

            public int Length { get; }
        }

        private List<NalStart> FindNalStartCodes(byte[] data)
        {
            var starts = new List<NalStart>();
            for (int i = 0; i < data.Length - 3; i++)
            {
                if (data[i] == 0x00 && data[i + 1] == 0x00 && data[i + 2] == 0x01)
                {
                    starts.Add(new NalStart(i, 3));
                    i += 2;
                    continue;
                }

                if (i < data.Length - 4 && data[i] == 0x00 && data[i + 1] == 0x00 && data[i + 2] == 0x00 && data[i + 3] == 0x01)
                {
                    starts.Add(new NalStart(i, 4));
                    i += 3;
                }
            }

            return starts;
        }

        private string BuildDatamoshVideoFilter(int seed)
        {
            return $"random=frames=30:seed={seed},tmix=frames=5:weights='1 1 1 1 1',tblend=all_mode=average";
        }

        private string BuildGlitchAudioFilter(int seed, int outputAudioSampleRate)
        {
            var rng = new Random(seed);
            int baseRate = outputAudioSampleRate > 0 ? outputAudioSampleRate : 44100;
            int glitchRate = Math.Max(4000, Math.Min(8000, baseRate / 2));
            if (glitchRate >= baseRate)
            {
                glitchRate = Math.Max(4000, baseRate / 2);
            }

            int highpass = 200 + rng.Next(0, 400);
            int lowpass = 3000 + rng.Next(0, 2500);
            double volume = 1.2 + rng.NextDouble() * 0.5;

            return $"aresample={glitchRate},aresample={baseRate},highpass=f={highpass},lowpass=f={lowpass},volume={volume.ToString(CultureInfo.InvariantCulture)}";
        }

        internal string BuildDatamoshFilterComplex(string resolution, string audioBitDepth, int outputAudioSampleRate, int videoFps, List<DatamoshSegment> segments, out string videoOutLabel, out string audioOutLabel)
        {
            return FilterGraphBuilder.BuildDatamoshFilterComplex(resolution, audioBitDepth, outputAudioSampleRate, videoFps, segments, out videoOutLabel, out audioOutLabel);
        }

        internal List<DatamoshSegment> BuildDatamoshSegments(double duration, int seed, double jumpLengthSeconds, double glitchChance)
        {
            return ConversionAlgorithms.BuildDatamoshSegments(duration, seed, jumpLengthSeconds, glitchChance);
        }

        private bool TryParseResolution(string resolution, out int width, out int height)
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
