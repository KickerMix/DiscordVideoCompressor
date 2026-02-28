using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

namespace DiscordVideoCompressor
{
    internal sealed class FfmpegConversionService
    {
        private const string FfmpegResourceName = "DiscordVideoCompressor.Resources.ffmpeg.exe";
        private readonly Action<string, double, double, bool, double> setProgressStage;
        private readonly Action<double, double?> updateProgress;
        private readonly Func<string, string> buildVideoFilter;
        private readonly Func<string, string> buildAudioFilter;
        private readonly SpeedFilterBuilder buildSpeedFilterComplex;
        private readonly DatamoshFilterBuilder buildDatamoshFilterComplex;
        private readonly Func<double, int, double, double, List<DatamoshSegment>> buildDatamoshSegments;
        private readonly Func<long, int> selectAudioBitrate;
        private readonly ConversionText text;

        private Process ffmpegProcess;
        private string extractedFfmpegPath;

        internal delegate string SpeedFilterBuilder(double duration, string resolution, string audioBitDepth, int outputAudioSampleRate, int inputAudioSampleRate, int videoFps, string extraVideoFilter, bool includeAudio, out string videoOutLabel, out string audioOutLabel);
        internal delegate string DatamoshFilterBuilder(string resolution, string audioBitDepth, int outputAudioSampleRate, int videoFps, List<DatamoshSegment> segments, out string videoOutLabel, out string audioOutLabel);

        public FfmpegConversionService(
            Action<string, double, double, bool, double> setProgressStage,
            Action<double, double?> updateProgress,
            Func<string, string> buildVideoFilter,
            Func<string, string> buildAudioFilter,
            SpeedFilterBuilder buildSpeedFilterComplex,
            DatamoshFilterBuilder buildDatamoshFilterComplex,
            Func<double, int, double, double, List<DatamoshSegment>> buildDatamoshSegments,
            Func<long, int> selectAudioBitrate,
            ConversionText text)
        {
            this.setProgressStage = setProgressStage;
            this.updateProgress = updateProgress;
            this.buildVideoFilter = buildVideoFilter;
            this.buildAudioFilter = buildAudioFilter;
            this.buildSpeedFilterComplex = buildSpeedFilterComplex;
            this.buildDatamoshFilterComplex = buildDatamoshFilterComplex;
            this.buildDatamoshSegments = buildDatamoshSegments;
            this.selectAudioBitrate = selectAudioBitrate;
            this.text = text;
        }

        public string ConvertFile(string sourcePath, ConversionOptions options, CancellationToken token, out string warningMessage)
        {
            warningMessage = null;

            string outputFormat = options.OutputFormat.Trim().ToLowerInvariant();
            string outputFile = Path.Combine(Path.GetDirectoryName(sourcePath), Path.GetFileNameWithoutExtension(sourcePath) + "_cnvrtd" + $".{outputFormat}");
            string ffmpegPath = GetFfmpegPath();

            double duration = 0;
            int inputAudioSampleRate = options.AudioSampleRate;
            using (var probeProcess = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = ffmpegPath,
                    Arguments = $"-i \"{sourcePath}\" -hide_banner",
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            })
            {
                probeProcess.Start();
                string output = probeProcess.StandardError.ReadToEnd();
                probeProcess.WaitForExit();

                Match match = Regex.Match(output, @"Duration:\s(\d+):(\d+):(\d+\.?\d*)");
                if (!match.Success)
                {
                    throw new InvalidOperationException(text.DurationError + "\n" + output);
                }

                int hours = int.Parse(match.Groups[1].Value);
                int minutes = int.Parse(match.Groups[2].Value);
                if (!double.TryParse(match.Groups[3].Value.Replace(",", "."), NumberStyles.Float, CultureInfo.InvariantCulture, out double seconds))
                {
                    throw new InvalidOperationException(text.TimeConversionError);
                }

                duration = hours * 3600 + minutes * 60 + seconds;

                Match audioMatch = Regex.Match(output, @"Audio:\s*([^,\s]+).*?(\d+)\s*Hz");
                if (audioMatch.Success && int.TryParse(audioMatch.Groups[2].Value, out int parsedInputSampleRate) && parsedInputSampleRate > 0)
                {
                    inputAudioSampleRate = parsedInputSampleRate;
                }
            }

            if (duration <= 0)
            {
                throw new InvalidOperationException(text.VideoDurationError);
            }

            setProgressStage(text.ProgressStagePrepare, 0.0, 0.0, false, duration);

            int datamoshSeed = Environment.TickCount;
            List<DatamoshSegment> glitchSegments = options.EnableGlitchEffect
                ? buildDatamoshSegments(duration, datamoshSeed, options.GlitchJumpSeconds, options.GlitchChance)
                : null;
            int datamoshPostSeed = datamoshSeed ^ 0x4f1bbc;

            long targetBitrate = (long)((options.TargetSizeBytes * 8) / duration);
            int audioBitrate = selectAudioBitrate(targetBitrate);
            long videoBitrate = targetBitrate - audioBitrate;
            ConversionValidationError validationError = ConversionValidation.ValidateRequest(File.Exists(sourcePath), options, outputFormat, videoBitrate);
            if (validationError != ConversionValidationError.None)
            {
                throw new InvalidOperationException(MapValidationError(validationError));
            }

            string codecVideo;
            string codecAudio;
            if (outputFormat == "webm")
            {
                codecVideo = "libvpx-vp9";
                codecAudio = "libopus";
            }
            else if (outputFormat == "mp4")
            {
                codecVideo = "libx264";
                codecAudio = "aac";
            }
            else
            {
                throw new InvalidOperationException(text.InvalidFileFormat);
            }

            string videoFilter = buildVideoFilter(options.VideoResolution);
            string videoFilterArg = string.IsNullOrWhiteSpace(videoFilter) ? string.Empty : $" -vf \"{videoFilter}\"";
            string audioFilter = buildAudioFilter(options.AudioBitDepth);
            string audioFilterArg = string.IsNullOrWhiteSpace(audioFilter) ? string.Empty : $" -af \"{audioFilter}\"";
            string fpsArg = options.VideoFps > 0 ? $" -r {options.VideoFps}" : string.Empty;
            string pixelFormatArg = outputFormat == "mp4" ? " -pix_fmt yuv420p" : string.Empty;

            bool conversionSuccess = false;
            long currentVideoBitrate = videoBitrate;
            int pass = 0;
            int maxPasses = 3;
            string createdFile = null;

            try
            {
                do
                {
                    token.ThrowIfCancellationRequested();
                    pass++;
                    if (pass > maxPasses)
                    {
                        throw new InvalidOperationException(text.FileConversionError);
                    }

                    string passLogFileBase = null;
                    string outputFileTemp = Path.Combine(Path.GetDirectoryName(outputFile), Path.GetFileNameWithoutExtension(outputFile) + ".tmp." + outputFormat);
                    string datamoshIntermediate = null;
                    string glitchIntermediate = null;
                    var tempFiles = new List<string>();

                    try
                    {
                        if (File.Exists(outputFileTemp))
                        {
                            File.Delete(outputFileTemp);
                        }

                        string pass1Args;
                        string pass2Args;
                        string pass1StageName = null;
                        double pass1StageStart = 0;
                        double pass1StageSpan = 1;
                        string pass2StageName = null;
                        double pass2StageStart = 0.5;
                        double pass2StageSpan = 0.5;

                        if (options.EnableSpeedEffect && options.EnableGlitchEffect)
                        {
                            string tempDir = Path.Combine(Path.GetTempPath(), "DiscordVideoCompressor");
                            Directory.CreateDirectory(tempDir);
                            glitchIntermediate = Path.Combine(tempDir, $"glitch_speed_{Guid.NewGuid():N}.{outputFormat}");

                            List<DatamoshSegment> segments = glitchSegments ?? buildDatamoshSegments(duration, datamoshSeed, options.GlitchJumpSeconds, options.GlitchChance);
                            string glitchFilter = buildDatamoshFilterComplex(options.VideoResolution, options.AudioBitDepth, options.AudioSampleRate, options.VideoFps, segments, out string glitchVLabel, out string glitchALabel);
                            string glitchScriptPath = WriteFilterScript(glitchFilter, tempFiles);
                            string glitchRateArgs = outputFormat == "mp4" ? $" -maxrate {currentVideoBitrate} -bufsize {currentVideoBitrate * 2}" : string.Empty;
                            string glitchArgs = $"-i \"{sourcePath}\" -filter_complex_script \"{glitchScriptPath}\" -map \"{glitchVLabel}\" -map \"{glitchALabel}\" -c:v {codecVideo} -b:v {currentVideoBitrate}{glitchRateArgs} -b:a {audioBitrate} -c:a {codecAudio} -ar {options.AudioSampleRate} -preset veryfast{pixelFormatArg} -y \"{glitchIntermediate}\"";
                            setProgressStage(text.ProgressStageGlitch, 0.0, 0.5, true, duration);
                            RunCheckedFfmpeg(ffmpegPath, glitchArgs, token, duration);

                            string speedFilter = buildSpeedFilterComplex(duration, options.VideoResolution, options.AudioBitDepth, options.AudioSampleRate, options.AudioSampleRate, options.VideoFps, null, true, out string vOutLabel, out string aOutLabel);
                            string speedScriptPath = WriteFilterScript(speedFilter, tempFiles);
                            string speedRateArgs = outputFormat == "mp4" ? $" -maxrate {currentVideoBitrate} -bufsize {currentVideoBitrate * 2}" : string.Empty;
                            pass1Args = $"-i \"{glitchIntermediate}\" -filter_complex_script \"{speedScriptPath}\" -map \"{vOutLabel}\" -map \"{aOutLabel}\" -c:v {codecVideo} -b:v {currentVideoBitrate}{speedRateArgs} -b:a {audioBitrate} -c:a {codecAudio} -ar {options.AudioSampleRate} -preset veryfast{pixelFormatArg} -y \"{outputFileTemp}\"";
                            pass2Args = null;
                            pass1StageName = text.ProgressStageSpeed;
                            pass1StageStart = 0.5;
                            pass1StageSpan = 0.5;
                        }
                        else if (options.EnableSpeedEffect)
                        {
                            string filterComplex = buildSpeedFilterComplex(duration, options.VideoResolution, options.AudioBitDepth, options.AudioSampleRate, inputAudioSampleRate, options.VideoFps, null, true, out string vOutLabel, out string aOutLabel);
                            string filterScriptPath = WriteFilterScript(filterComplex, tempFiles);
                            string rateControlArgs = outputFormat == "mp4" ? $" -maxrate {currentVideoBitrate} -bufsize {currentVideoBitrate * 2}" : string.Empty;
                            pass1Args = $"-i \"{sourcePath}\" -filter_complex_script \"{filterScriptPath}\" -map \"{vOutLabel}\" -map \"{aOutLabel}\" -c:v {codecVideo} -b:v {currentVideoBitrate}{rateControlArgs} -b:a {audioBitrate} -c:a {codecAudio} -ar {options.AudioSampleRate} -preset veryfast{pixelFormatArg} -y \"{outputFileTemp}\"";
                            pass2Args = null;
                            pass1StageName = text.ProgressStageSpeed;
                        }
                        else if (options.EnableDatamosh && options.EnableGlitchEffect)
                        {
                            string tempDir = Path.Combine(Path.GetTempPath(), "DiscordVideoCompressor");
                            Directory.CreateDirectory(tempDir);
                            datamoshIntermediate = Path.Combine(tempDir, $"datamosh_glitch_{Guid.NewGuid():N}.mp4");

                            if (!RunTrueDatamoshPipeline(ffmpegPath, sourcePath, datamoshIntermediate, currentVideoBitrate, audioBitrate, options.AudioSampleRate, options.AudioBitDepth, options.VideoResolution, options.VideoFps, duration, datamoshSeed, 0.0, 0.6, token, out StringBuilder datamoshOutput, out bool datamoshHasAudio))
                            {
                                throw new InvalidOperationException(text.ConversionErrorPrefix + datamoshOutput);
                            }

                            if (!datamoshHasAudio)
                            {
                                warningMessage = text.DatamoshAudioMissing;
                            }

                            List<DatamoshSegment> segments = glitchSegments ?? buildDatamoshSegments(duration, datamoshPostSeed, options.GlitchJumpSeconds, options.GlitchChance);
                            string filterComplex = buildDatamoshFilterComplex(options.VideoResolution, options.AudioBitDepth, options.AudioSampleRate, options.VideoFps, segments, out string vOutLabel, out string aOutLabel);
                            string filterScriptPath = WriteFilterScript(filterComplex, tempFiles);
                            string rateControlArgs = outputFormat == "mp4" ? $" -maxrate {currentVideoBitrate} -bufsize {currentVideoBitrate * 2}" : string.Empty;
                            pass1Args = $"-i \"{datamoshIntermediate}\" -filter_complex_script \"{filterScriptPath}\" -map \"{vOutLabel}\" -map \"{aOutLabel}\" -c:v {codecVideo} -b:v {currentVideoBitrate}{rateControlArgs} -b:a {audioBitrate} -c:a {codecAudio} -ar {options.AudioSampleRate} -preset veryfast{pixelFormatArg} -y \"{outputFileTemp}\"";
                            pass2Args = null;
                            pass1StageName = text.ProgressStageGlitch;
                            pass1StageStart = 0.6;
                            pass1StageSpan = 0.4;
                        }
                        else if (options.EnableDatamosh)
                        {
                            if (!RunTrueDatamoshPipeline(ffmpegPath, sourcePath, outputFileTemp, currentVideoBitrate, audioBitrate, options.AudioSampleRate, options.AudioBitDepth, options.VideoResolution, options.VideoFps, duration, datamoshSeed, 0.0, 1.0, token, out StringBuilder datamoshOutput, out bool datamoshHasAudio))
                            {
                                throw new InvalidOperationException(text.ConversionErrorPrefix + datamoshOutput);
                            }

                            if (!datamoshHasAudio)
                            {
                                warningMessage = text.DatamoshAudioMissing;
                            }

                            pass1Args = null;
                            pass2Args = null;
                        }
                        else if (options.EnableGlitchEffect)
                        {
                            List<DatamoshSegment> segments = glitchSegments ?? buildDatamoshSegments(duration, datamoshPostSeed, options.GlitchJumpSeconds, options.GlitchChance);
                            string filterComplex = buildDatamoshFilterComplex(options.VideoResolution, options.AudioBitDepth, options.AudioSampleRate, options.VideoFps, segments, out string vOutLabel, out string aOutLabel);
                            string filterScriptPath = WriteFilterScript(filterComplex, tempFiles);
                            string rateControlArgs = outputFormat == "mp4" ? $" -maxrate {currentVideoBitrate} -bufsize {currentVideoBitrate * 2}" : string.Empty;
                            pass1Args = $"-i \"{sourcePath}\" -filter_complex_script \"{filterScriptPath}\" -map \"{vOutLabel}\" -map \"{aOutLabel}\" -c:v {codecVideo} -b:v {currentVideoBitrate}{rateControlArgs} -b:a {audioBitrate} -c:a {codecAudio} -ar {options.AudioSampleRate} -preset veryfast{pixelFormatArg} -y \"{outputFileTemp}\"";
                            pass2Args = null;
                            pass1StageName = text.ProgressStageGlitch;
                        }
                        else
                        {
                            passLogFileBase = CreatePassLogFileBase();
                            pass1Args = $"-i \"{sourcePath}\" -c:v {codecVideo} -b:v {currentVideoBitrate}{fpsArg}{videoFilterArg} -preset veryfast{pixelFormatArg} -pass 1 -passlogfile \"{passLogFileBase}\" -an -f null NUL";
                            pass2Args = $"-i \"{sourcePath}\" -c:v {codecVideo} -b:v {currentVideoBitrate}{fpsArg}{videoFilterArg} -pass 2 -passlogfile \"{passLogFileBase}\" -b:a {audioBitrate} -c:a {codecAudio}{audioFilterArg} -ar {options.AudioSampleRate} -preset veryfast{pixelFormatArg} -y \"{outputFileTemp}\"";
                            pass1StageName = text.ProgressStagePass1;
                            pass1StageSpan = 0.5;
                            pass2StageName = text.ProgressStagePass2;
                            pass2StageStart = 0.5;
                            pass2StageSpan = 0.5;
                        }

                        if (!string.IsNullOrWhiteSpace(pass1Args))
                        {
                            if (!string.IsNullOrWhiteSpace(pass1StageName))
                            {
                                setProgressStage(pass1StageName, pass1StageStart, pass1StageSpan, true, duration);
                            }

                            RunCheckedFfmpeg(ffmpegPath, pass1Args, token, duration);
                        }

                        if (!string.IsNullOrWhiteSpace(pass2Args))
                        {
                            if (!string.IsNullOrWhiteSpace(pass2StageName))
                            {
                                setProgressStage(pass2StageName, pass2StageStart, pass2StageSpan, true, duration);
                            }

                            RunCheckedFfmpeg(ffmpegPath, pass2Args, token, duration);
                        }

                        FileInfo fileInfo = new FileInfo(outputFileTemp);
                        if (fileInfo.Length <= options.TargetSizeBytes)
                        {
                            createdFile = FinalizeOutputFile(outputFileTemp, outputFile);
                            conversionSuccess = true;
                        }
                        else
                        {
                            double sizeRatio = (double)options.TargetSizeBytes / Math.Max(1L, fileInfo.Length);
                            currentVideoBitrate = (long)(currentVideoBitrate * sizeRatio * 0.95);
                        }
                    }
                    finally
                    {
                        CleanupPassLogFiles(passLogFileBase);
                        TryDeleteFile(datamoshIntermediate);
                        TryDeleteFile(glitchIntermediate);
                        foreach (string tempFile in tempFiles)
                        {
                            TryDeleteFile(tempFile);
                        }
                    }
                }
                while (!conversionSuccess);
            }
            finally
            {
                Cancel();
            }

            return createdFile;
        }

        public void Cancel()
        {
            if (ffmpegProcess == null)
            {
                return;
            }

            try
            {
                if (!ffmpegProcess.HasExited)
                {
                    ffmpegProcess.Kill();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Error killing ffmpeg: " + ex.Message);
            }
        }

        private bool RunTrueDatamoshPipeline(string ffmpegPath, string inputFile, string outputFileTemp, long videoBitrate, int audioBitrate, int audioSampleRate, string audioBitDepth, string videoResolution, int videoFps, double duration, int seed, double stageStart, double stageSpan, CancellationToken token, out StringBuilder outputLog, out bool outputHasAudio)
        {
            outputLog = new StringBuilder();
            outputHasAudio = true;
            string tempDir = Path.Combine(Path.GetTempPath(), "DiscordVideoCompressor");
            Directory.CreateDirectory(tempDir);

            string rawVideoPath = Path.Combine(tempDir, $"datamosh_{Guid.NewGuid():N}.h264");
            string moshVideoPath = Path.Combine(tempDir, $"datamosh_{Guid.NewGuid():N}_mosh.h264");

            try
            {
                token.ThrowIfCancellationRequested();

                int gop = videoFps > 0 ? Math.Min(120, Math.Max(15, videoFps)) : 60;
                string videoFilter = buildVideoFilter(videoResolution);
                string videoFilterArg = string.IsNullOrWhiteSpace(videoFilter) ? string.Empty : $" -vf \"{videoFilter}\"";
                string fpsArg = videoFps > 0 ? $" -r {videoFps}" : string.Empty;
                string x264Params = $"keyint={gop}:min-keyint=1:scenecut=40:open-gop=0";

                string videoArgs = $"-i \"{inputFile}\" -c:v libx264 -b:v {videoBitrate}{videoFilterArg}{fpsArg} -preset veryfast -pix_fmt yuv420p -bf 0 -x264-params \"{x264Params}\" -an -f h264 -y \"{rawVideoPath}\"";
                setProgressStage(text.ProgressStageDatamoshEncode, stageStart, stageSpan * 0.45, true, duration);
                if (!RunFfmpegProcess(ffmpegPath, videoArgs, token, true, duration, out StringBuilder videoOutput))
                {
                    outputLog.Append(videoOutput);
                    return false;
                }

                token.ThrowIfCancellationRequested();

                // Datamosh byte-level transform stays in the form helper for now.
                if (!ApplyDatamoshTransform(rawVideoPath, moshVideoPath, duration, videoFps, seed, token, out string transformError))
                {
                    outputLog.Append(transformError);
                    return false;
                }

                token.ThrowIfCancellationRequested();

                string inputFpsArg = videoFps > 0 ? $" -r {videoFps}" : string.Empty;
                string audioFilter = buildAudioFilter(audioBitDepth);
                string audioFilterArg = string.IsNullOrWhiteSpace(audioFilter) ? string.Empty : $" -af \"{audioFilter}\"";
                string rateControlArgs = $" -maxrate {videoBitrate} -bufsize {videoBitrate * 2}";
                string remuxArgs = $"{inputFpsArg} -fflags +genpts -i \"{moshVideoPath}\" -i \"{inputFile}\" -map 0:v:0 -map 1:a:0? -c:v libx264 -b:v {videoBitrate}{rateControlArgs} -preset veryfast -pix_fmt yuv420p -bf 0 -c:a aac -b:a {audioBitrate}{audioFilterArg} -ar {audioSampleRate} -shortest -movflags +faststart -y \"{outputFileTemp}\"";
                setProgressStage(text.ProgressStageDatamoshRemux, stageStart + stageSpan * 0.55, stageSpan * 0.45, true, duration);
                if (!RunFfmpegProcess(ffmpegPath, remuxArgs, token, true, duration, out StringBuilder remuxOutput))
                {
                    outputLog.Append(remuxOutput);
                    return false;
                }

                outputHasAudio = ProbeHasAudio(ffmpegPath, outputFileTemp);
                return true;
            }
            finally
            {
                TryDeleteFile(rawVideoPath);
                TryDeleteFile(moshVideoPath);
            }
        }

        private bool ApplyDatamoshTransform(string inputPath, string outputPath, double duration, int videoFps, int seed, CancellationToken token, out string error)
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

            List<(int Index, int Length)> nalStarts = FindNalStartCodes(data);
            if (nalStarts.Count == 0)
            {
                error = "Datamosh failed: no NAL units found.";
                return false;
            }

            int fps = videoFps > 0 ? videoFps : 60;
            var rng = new Random(seed);
            int frameIndex = 0;
            int idrIndex = 0;
            int convertedIdr = 0;
            var idrHeaders = new List<int>();

            setProgressStage(text.ProgressStageDatamoshTransform, 0.45, 0.1, false, duration);

            for (int i = 0; i < nalStarts.Count; i++)
            {
                token.ThrowIfCancellationRequested();

                (int Index, int Length) current = nalStarts[i];
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
                    if (nalType == 5)
                    {
                        if (idrIndex > 0)
                        {
                            idrHeaders.Add(headerIndex);
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

        private bool ProbeHasAudio(string ffmpegPath, string filePath)
        {
            try
            {
                using var process = new Process
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
                return Regex.IsMatch(output, @"Audio:\s", RegexOptions.IgnoreCase);
            }
            catch
            {
                return false;
            }
        }

        private List<(int Index, int Length)> FindNalStartCodes(byte[] data)
        {
            var starts = new List<(int Index, int Length)>();
            for (int i = 0; i < data.Length - 3; i++)
            {
                if (data[i] == 0x00 && data[i + 1] == 0x00 && data[i + 2] == 0x01)
                {
                    starts.Add((i, 3));
                    i += 2;
                    continue;
                }

                if (i < data.Length - 4 && data[i] == 0x00 && data[i + 1] == 0x00 && data[i + 2] == 0x00 && data[i + 3] == 0x01)
                {
                    starts.Add((i, 4));
                    i += 3;
                }
            }

            return starts;
        }

        private string ExtractFfmpeg()
        {
            if (!string.IsNullOrEmpty(extractedFfmpegPath) && File.Exists(extractedFfmpegPath))
            {
                return extractedFfmpegPath;
            }

            string tempDir = Path.Combine(Path.GetTempPath(), "DiscordVideoCompressor");
            string ffmpegPath = Path.Combine(tempDir, $"ffmpeg_{Guid.NewGuid():N}.exe");
            Directory.CreateDirectory(tempDir);
            using (Stream resourceStream = Assembly.GetExecutingAssembly().GetManifestResourceStream(FfmpegResourceName)
                ?? throw new InvalidOperationException($"Embedded ffmpeg resource '{FfmpegResourceName}' was not found."))
            using (FileStream outputStream = new FileStream(ffmpegPath, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                resourceStream.CopyTo(outputStream);
                outputStream.Flush();
            }

            ValidateExtractedFfmpeg(ffmpegPath);
            extractedFfmpegPath = ffmpegPath;
            return ffmpegPath;
        }

        private string GetFfmpegPath() => ExtractFfmpeg();

        private void ValidateExtractedFfmpeg(string ffmpegPath)
        {
            using FileStream stream = new FileStream(ffmpegPath, FileMode.Open, FileAccess.Read, FileShare.Read);
            if (stream.Length < 2)
            {
                throw new InvalidOperationException("Embedded ffmpeg resource is invalid or incomplete.");
            }

            int firstByte = stream.ReadByte();
            int secondByte = stream.ReadByte();
            if (firstByte != 'M' || secondByte != 'Z')
            {
                throw new InvalidOperationException("Embedded ffmpeg resource is invalid. Rebuild the application with a valid ffmpeg.exe payload.");
            }
        }

        private string MapValidationError(ConversionValidationError error)
        {
            return error switch
            {
                ConversionValidationError.MissingFile => text.FileNotFound,
                ConversionValidationError.DatamoshSpeedConflict => text.DatamoshSpeedConflict,
                ConversionValidationError.DatamoshMp4Only => text.DatamoshMp4Only,
                ConversionValidationError.BitrateTooLow => text.BitrateError,
                ConversionValidationError.InvalidOutputFormat => text.InvalidFileFormat,
                _ => text.ConversionErrorPrefix
            };
        }

        private void RunCheckedFfmpeg(string ffmpegPath, string arguments, CancellationToken token, double duration)
        {
            if (!RunFfmpegProcess(ffmpegPath, arguments, token, true, duration, out StringBuilder output))
            {
                throw new InvalidOperationException(text.ConversionErrorPrefix + output);
            }
        }

        private bool RunFfmpegProcess(string ffmpegPath, string arguments, CancellationToken token, bool captureProgress, double duration, out StringBuilder stdErrOutput)
        {
            stdErrOutput = new StringBuilder();
            ffmpegProcess = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = ffmpegPath,
                    Arguments = arguments,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            try
            {
                ffmpegProcess.Start();
                Regex timeRegex = captureProgress ? new Regex(@"time=(\d+):(\d+):(\d+\.?\d*)") : null;
                Regex speedRegex = captureProgress ? new Regex(@"speed=([0-9]+(?:[\.,][0-9]+)?)x") : null;
                string stdErrLine;

                while ((stdErrLine = ffmpegProcess.StandardError.ReadLine()) != null)
                {
                    token.ThrowIfCancellationRequested();
                    stdErrOutput.AppendLine(stdErrLine);

                    if (captureProgress)
                    {
                        double? speedValue = null;
                        if (speedRegex != null)
                        {
                            Match speedMatch = speedRegex.Match(stdErrLine);
                            if (speedMatch.Success && double.TryParse(speedMatch.Groups[1].Value.Replace(",", "."), NumberStyles.Float, CultureInfo.InvariantCulture, out double parsedSpeed))
                            {
                                speedValue = parsedSpeed;
                            }
                        }

                        Match timeMatch = timeRegex.Match(stdErrLine);
                        if (timeMatch.Success)
                        {
                            int hoursCurrent = int.Parse(timeMatch.Groups[1].Value);
                            int minutesCurrent = int.Parse(timeMatch.Groups[2].Value);
                            double secondsCurrent = double.TryParse(timeMatch.Groups[3].Value.Replace(",", "."), NumberStyles.Float, CultureInfo.InvariantCulture, out double parsedSeconds)
                                ? parsedSeconds
                                : 0;
                            double currentDuration = hoursCurrent * 3600 + minutesCurrent * 60 + secondsCurrent;
                            updateProgress(currentDuration, speedValue);
                        }
                    }
                }

                ffmpegProcess.WaitForExit();
                return ffmpegProcess.ExitCode == 0;
            }
            catch (OperationCanceledException)
            {
                Cancel();
                throw;
            }
            finally
            {
                ffmpegProcess?.Dispose();
                ffmpegProcess = null;
            }
        }

        private void TryDeleteFile(string path)
        {
            if (!string.IsNullOrWhiteSpace(path) && File.Exists(path))
            {
                try
                {
                    File.Delete(path);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("Error deleting temp file: " + ex.Message);
                }
            }
        }

        private string WriteFilterScript(string filterComplex, List<string> tempFiles)
        {
            if (string.IsNullOrWhiteSpace(filterComplex))
            {
                return null;
            }

            string tempDir = Path.Combine(Path.GetTempPath(), "DiscordVideoCompressor");
            Directory.CreateDirectory(tempDir);
            string scriptPath = Path.Combine(tempDir, $"ffmpeg_filter_{Guid.NewGuid():N}.txt");
            File.WriteAllText(scriptPath, filterComplex, new UTF8Encoding(false));
            tempFiles?.Add(scriptPath);
            return scriptPath;
        }

        private string CreatePassLogFileBase()
        {
            string tempDir = Path.Combine(Path.GetTempPath(), "DiscordVideoCompressor");
            Directory.CreateDirectory(tempDir);
            return Path.Combine(tempDir, $"ffmpeg2pass_{Guid.NewGuid():N}");
        }

        private void CleanupPassLogFiles(string passLogFileBase)
        {
            if (string.IsNullOrWhiteSpace(passLogFileBase))
            {
                return;
            }

            string directory = Path.GetDirectoryName(passLogFileBase);
            string baseName = Path.GetFileName(passLogFileBase);
            if (string.IsNullOrEmpty(directory) || string.IsNullOrEmpty(baseName))
            {
                return;
            }

            foreach (string file in Directory.GetFiles(directory, baseName + "*"))
            {
                TryDeleteFile(file);
            }
        }

        private string FinalizeOutputFile(string tempFile, string outputFile)
        {
            try
            {
                if (File.Exists(outputFile))
                {
                    File.Delete(outputFile);
                }

                File.Move(tempFile, outputFile);
                return outputFile;
            }
            catch (IOException)
            {
                string directory = Path.GetDirectoryName(outputFile);
                string baseName = Path.GetFileNameWithoutExtension(outputFile);
                string extension = Path.GetExtension(outputFile);
                string suffix = DateTime.Now.ToString("yyyyMMdd_HHmmss", CultureInfo.InvariantCulture);
                string alternate = Path.Combine(directory, baseName + "_" + suffix + extension);
                File.Move(tempFile, alternate);
                return alternate;
            }
        }
    }
}
