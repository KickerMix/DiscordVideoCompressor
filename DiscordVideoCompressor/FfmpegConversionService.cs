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
        private readonly Func<string> ffmpegPathProvider;
        private readonly object processSync = new object();

        private Process ffmpegProcess;

        internal delegate string SpeedFilterBuilder(double duration, string resolution, string audioBitDepth, int outputAudioSampleRate, int inputAudioSampleRate, int videoFps, string extraVideoFilter, bool includeAudio, out string videoOutLabel, out string audioOutLabel);
        internal delegate string DatamoshFilterBuilder(string resolution, string audioBitDepth, int outputAudioSampleRate, int videoFps, List<DatamoshSegment> segments, bool includeAudio, out string videoOutLabel, out string audioOutLabel);

        public FfmpegConversionService(
            Action<string, double, double, bool, double> setProgressStage,
            Action<double, double?> updateProgress,
            Func<string, string> buildVideoFilter,
            Func<string, string> buildAudioFilter,
            SpeedFilterBuilder buildSpeedFilterComplex,
            DatamoshFilterBuilder buildDatamoshFilterComplex,
            Func<double, int, double, double, List<DatamoshSegment>> buildDatamoshSegments,
            Func<long, int> selectAudioBitrate,
            ConversionText text,
            Func<string> ffmpegPathProvider = null)
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
            this.ffmpegPathProvider = ffmpegPathProvider ?? (() => FfmpegBinaryManager.GetOrExtract(typeof(FfmpegConversionService).Assembly, FfmpegResourceName));
        }

        public string ConvertFile(string sourcePath, ConversionOptions options, CancellationToken token, out string warningMessage)
        {
            warningMessage = null;

            string outputFormat = options.OutputFormat.Trim().ToLowerInvariant();
            string outputFile = Path.Combine(Path.GetDirectoryName(sourcePath), Path.GetFileNameWithoutExtension(sourcePath) + "_cnvrtd" + $".{outputFormat}");
            string ffmpegPath = GetFfmpegPath();

            token.ThrowIfCancellationRequested();
            RunFfmpegProcess(ffmpegPath, $"-hide_banner -i \"{sourcePath}\"", token, false, 0, out StringBuilder probeOutput);
            if (!MediaProbe.TryParse(probeOutput.ToString(), options.AudioSampleRate, out MediaProbeResult media))
            {
                throw new InvalidOperationException(text.DurationError + "\n" + probeOutput);
            }

            double duration = media.Duration;
            int inputAudioSampleRate = media.AudioSampleRate;
            bool hasAudio = media.HasAudio;

            setProgressStage(text.ProgressStagePrepare, 0.0, 0.0, false, duration);

            int datamoshSeed = Environment.TickCount;
            List<DatamoshSegment> glitchSegments = options.EnableGlitchEffect
                ? buildDatamoshSegments(duration, datamoshSeed, options.GlitchJumpSeconds, options.GlitchChance)
                : null;
            int datamoshPostSeed = datamoshSeed ^ 0x4f1bbc;

            long targetBitrate = (long)(options.TargetSizeBytes * 8.0 / duration);
            int audioBitrate = hasAudio ? selectAudioBitrate(targetBitrate) : 0;
            long videoBitrate = ConversionAlgorithms.CalculateVideoBitrate(options.TargetSizeBytes, duration, audioBitrate);
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

            int outputAudioSampleRate = ConversionAlgorithms.NormalizeAudioSampleRate(outputFormat, options.AudioSampleRate);

            string videoFilter = buildVideoFilter(options.VideoResolution);
            string videoFilterArg = string.IsNullOrWhiteSpace(videoFilter) ? string.Empty : $" -vf \"{videoFilter}\"";
            string audioFilter = buildAudioFilter(options.AudioBitDepth);
            string audioFilterArg = string.IsNullOrWhiteSpace(audioFilter) ? string.Empty : $" -af \"{audioFilter}\"";
            string fpsArg = options.VideoFps > 0 ? $" -r {options.VideoFps}" : string.Empty;
            string pixelFormatArg = outputFormat == "mp4" ? " -pix_fmt yuv420p" : string.Empty;
            string videoSpeedArgs = outputFormat == "mp4" ? " -preset veryfast" : " -deadline good -cpu-used 4";
            string audioEncodeArgs = hasAudio
                ? $" -b:a {audioBitrate} -c:a {codecAudio}{audioFilterArg} -ar {outputAudioSampleRate}"
                : " -an";

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
                    string outputFileTemp = Path.Combine(
                        Path.GetDirectoryName(outputFile),
                        $".{Path.GetFileNameWithoutExtension(outputFile)}.{Guid.NewGuid():N}.tmp.{outputFormat}");
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
                            string glitchFilter = buildDatamoshFilterComplex(options.VideoResolution, options.AudioBitDepth, outputAudioSampleRate, options.VideoFps, segments, hasAudio, out string glitchVLabel, out string glitchALabel);
                            string glitchScriptPath = WriteFilterScript(glitchFilter, tempFiles);
                            string glitchRateArgs = outputFormat == "mp4" ? $" -maxrate {currentVideoBitrate} -bufsize {currentVideoBitrate * 2}" : string.Empty;
                            string glitchMapArgs = BuildMapArgs(glitchVLabel, glitchALabel, hasAudio);
                            string glitchArgs = $"-i \"{sourcePath}\" -filter_complex_script \"{glitchScriptPath}\"{glitchMapArgs} -c:v {codecVideo} -b:v {currentVideoBitrate}{glitchRateArgs}{audioEncodeArgs}{videoSpeedArgs}{pixelFormatArg} -y \"{glitchIntermediate}\"";
                            setProgressStage(text.ProgressStageGlitch, 0.0, 0.5, true, duration);
                            RunCheckedFfmpeg(ffmpegPath, glitchArgs, token, duration);

                            string speedFilter = buildSpeedFilterComplex(duration, options.VideoResolution, options.AudioBitDepth, outputAudioSampleRate, outputAudioSampleRate, options.VideoFps, null, hasAudio, out string vOutLabel, out string aOutLabel);
                            string speedScriptPath = WriteFilterScript(speedFilter, tempFiles);
                            string speedRateArgs = outputFormat == "mp4" ? $" -maxrate {currentVideoBitrate} -bufsize {currentVideoBitrate * 2}" : string.Empty;
                            string speedMapArgs = BuildMapArgs(vOutLabel, aOutLabel, hasAudio);
                            pass1Args = $"-i \"{glitchIntermediate}\" -filter_complex_script \"{speedScriptPath}\"{speedMapArgs} -c:v {codecVideo} -b:v {currentVideoBitrate}{speedRateArgs}{audioEncodeArgs}{videoSpeedArgs}{pixelFormatArg} -y \"{outputFileTemp}\"";
                            pass2Args = null;
                            pass1StageName = text.ProgressStageSpeed;
                            pass1StageStart = 0.5;
                            pass1StageSpan = 0.5;
                        }
                        else if (options.EnableSpeedEffect)
                        {
                            string filterComplex = buildSpeedFilterComplex(duration, options.VideoResolution, options.AudioBitDepth, outputAudioSampleRate, inputAudioSampleRate, options.VideoFps, null, hasAudio, out string vOutLabel, out string aOutLabel);
                            string filterScriptPath = WriteFilterScript(filterComplex, tempFiles);
                            string rateControlArgs = outputFormat == "mp4" ? $" -maxrate {currentVideoBitrate} -bufsize {currentVideoBitrate * 2}" : string.Empty;
                            string mapArgs = BuildMapArgs(vOutLabel, aOutLabel, hasAudio);
                            pass1Args = $"-i \"{sourcePath}\" -filter_complex_script \"{filterScriptPath}\"{mapArgs} -c:v {codecVideo} -b:v {currentVideoBitrate}{rateControlArgs}{audioEncodeArgs}{videoSpeedArgs}{pixelFormatArg} -y \"{outputFileTemp}\"";
                            pass2Args = null;
                            pass1StageName = text.ProgressStageSpeed;
                        }
                        else if (options.EnableDatamosh && options.EnableGlitchEffect)
                        {
                            string tempDir = Path.Combine(Path.GetTempPath(), "DiscordVideoCompressor");
                            Directory.CreateDirectory(tempDir);
                            datamoshIntermediate = Path.Combine(tempDir, $"datamosh_glitch_{Guid.NewGuid():N}.mp4");

                            if (!RunTrueDatamoshPipeline(ffmpegPath, sourcePath, datamoshIntermediate, currentVideoBitrate, audioBitrate, outputAudioSampleRate, options.AudioBitDepth, options.VideoResolution, options.VideoFps, duration, hasAudio, 0.0, 0.6, token, out StringBuilder datamoshOutput, out bool datamoshHasAudio))
                            {
                                throw new InvalidOperationException(text.ConversionErrorPrefix + datamoshOutput);
                            }

                            if (!datamoshHasAudio)
                            {
                                warningMessage = text.DatamoshAudioMissing;
                            }

                            List<DatamoshSegment> segments = glitchSegments ?? buildDatamoshSegments(duration, datamoshPostSeed, options.GlitchJumpSeconds, options.GlitchChance);
                            string filterComplex = buildDatamoshFilterComplex(options.VideoResolution, options.AudioBitDepth, outputAudioSampleRate, options.VideoFps, segments, datamoshHasAudio, out string vOutLabel, out string aOutLabel);
                            string filterScriptPath = WriteFilterScript(filterComplex, tempFiles);
                            string rateControlArgs = outputFormat == "mp4" ? $" -maxrate {currentVideoBitrate} -bufsize {currentVideoBitrate * 2}" : string.Empty;
                            string mapArgs = BuildMapArgs(vOutLabel, aOutLabel, datamoshHasAudio);
                            pass1Args = $"-i \"{datamoshIntermediate}\" -filter_complex_script \"{filterScriptPath}\"{mapArgs} -c:v {codecVideo} -b:v {currentVideoBitrate}{rateControlArgs}{audioEncodeArgs}{videoSpeedArgs}{pixelFormatArg} -y \"{outputFileTemp}\"";
                            pass2Args = null;
                            pass1StageName = text.ProgressStageGlitch;
                            pass1StageStart = 0.6;
                            pass1StageSpan = 0.4;
                        }
                        else if (options.EnableDatamosh)
                        {
                            if (!RunTrueDatamoshPipeline(ffmpegPath, sourcePath, outputFileTemp, currentVideoBitrate, audioBitrate, outputAudioSampleRate, options.AudioBitDepth, options.VideoResolution, options.VideoFps, duration, hasAudio, 0.0, 1.0, token, out StringBuilder datamoshOutput, out bool datamoshHasAudio))
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
                            string filterComplex = buildDatamoshFilterComplex(options.VideoResolution, options.AudioBitDepth, outputAudioSampleRate, options.VideoFps, segments, hasAudio, out string vOutLabel, out string aOutLabel);
                            string filterScriptPath = WriteFilterScript(filterComplex, tempFiles);
                            string rateControlArgs = outputFormat == "mp4" ? $" -maxrate {currentVideoBitrate} -bufsize {currentVideoBitrate * 2}" : string.Empty;
                            string mapArgs = BuildMapArgs(vOutLabel, aOutLabel, hasAudio);
                            pass1Args = $"-i \"{sourcePath}\" -filter_complex_script \"{filterScriptPath}\"{mapArgs} -c:v {codecVideo} -b:v {currentVideoBitrate}{rateControlArgs}{audioEncodeArgs}{videoSpeedArgs}{pixelFormatArg} -y \"{outputFileTemp}\"";
                            pass2Args = null;
                            pass1StageName = text.ProgressStageGlitch;
                        }
                        else
                        {
                            passLogFileBase = CreatePassLogFileBase();
                            pass1Args = $"-i \"{sourcePath}\" -c:v {codecVideo} -b:v {currentVideoBitrate}{fpsArg}{videoFilterArg}{videoSpeedArgs}{pixelFormatArg} -pass 1 -passlogfile \"{passLogFileBase}\" -an -f null NUL";
                            pass2Args = $"-i \"{sourcePath}\" -c:v {codecVideo} -b:v {currentVideoBitrate}{fpsArg}{videoFilterArg} -pass 2 -passlogfile \"{passLogFileBase}\"{audioEncodeArgs}{videoSpeedArgs}{pixelFormatArg} -y \"{outputFileTemp}\"";
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
                            if (currentVideoBitrate <= 0)
                            {
                                throw new InvalidOperationException(text.BitrateError);
                            }
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

                        TryDeleteFile(outputFileTemp);
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
            Process process;
            lock (processSync)
            {
                process = ffmpegProcess;
            }

            if (process == null)
            {
                return;
            }

            try
            {
                if (!process.HasExited)
                {
                    process.Kill(true);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Error killing ffmpeg: " + ex.Message);
            }
        }

        private bool RunTrueDatamoshPipeline(string ffmpegPath, string inputFile, string outputFileTemp, long videoBitrate, int audioBitrate, int audioSampleRate, string audioBitDepth, string videoResolution, int videoFps, double duration, bool inputHasAudio, double stageStart, double stageSpan, CancellationToken token, out StringBuilder outputLog, out bool outputHasAudio)
        {
            outputLog = new StringBuilder();
            outputHasAudio = inputHasAudio;
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

                setProgressStage(text.ProgressStageDatamoshTransform, stageStart + stageSpan * 0.45, stageSpan * 0.1, false, duration);
                if (!DatamoshTransformer.Transform(rawVideoPath, moshVideoPath, token, out string transformError))
                {
                    outputLog.Append(transformError);
                    return false;
                }

                token.ThrowIfCancellationRequested();

                string inputFpsArg = videoFps > 0 ? $" -r {videoFps}" : string.Empty;
                string audioFilter = buildAudioFilter(audioBitDepth);
                string audioFilterArg = string.IsNullOrWhiteSpace(audioFilter) ? string.Empty : $" -af \"{audioFilter}\"";
                string rateControlArgs = $" -maxrate {videoBitrate} -bufsize {videoBitrate * 2}";
                string audioMapArgs = inputHasAudio ? " -map 1:a:0" : string.Empty;
                string audioEncodeArgs = inputHasAudio
                    ? $" -c:a aac -b:a {audioBitrate}{audioFilterArg} -ar {audioSampleRate} -shortest"
                    : " -an";
                string remuxArgs = $"{inputFpsArg} -fflags +genpts -i \"{moshVideoPath}\" -i \"{inputFile}\" -map 0:v:0{audioMapArgs} -c:v libx264 -b:v {videoBitrate}{rateControlArgs} -preset veryfast -pix_fmt yuv420p -bf 0{audioEncodeArgs} -movflags +faststart -y \"{outputFileTemp}\"";
                setProgressStage(text.ProgressStageDatamoshRemux, stageStart + stageSpan * 0.55, stageSpan * 0.45, true, duration);
                if (!RunFfmpegProcess(ffmpegPath, remuxArgs, token, true, duration, out StringBuilder remuxOutput))
                {
                    outputLog.Append(remuxOutput);
                    return false;
                }

                return true;
            }
            finally
            {
                TryDeleteFile(rawVideoPath);
                TryDeleteFile(moshVideoPath);
            }
        }

        private string GetFfmpegPath() => ffmpegPathProvider();

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
            using var process = new Process
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
                lock (processSync)
                {
                    ffmpegProcess = process;
                }

                process.Start();
                Regex timeRegex = captureProgress ? new Regex(@"time=(\d+):(\d+):(\d+\.?\d*)") : null;
                Regex speedRegex = captureProgress ? new Regex(@"speed=([0-9]+(?:[\.,][0-9]+)?)x") : null;
                string stdErrLine;

                while ((stdErrLine = process.StandardError.ReadLine()) != null)
                {
                    token.ThrowIfCancellationRequested();
                    AppendBoundedOutput(stdErrOutput, stdErrLine);

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

                process.WaitForExit();
                token.ThrowIfCancellationRequested();
                return process.ExitCode == 0;
            }
            catch (OperationCanceledException)
            {
                Cancel();
                throw;
            }
            finally
            {
                lock (processSync)
                {
                    if (ReferenceEquals(ffmpegProcess, process))
                    {
                        ffmpegProcess = null;
                    }
                }
            }
        }

        private static void AppendBoundedOutput(StringBuilder output, string line)
        {
            const int maxCharacters = 128 * 1024;
            output.AppendLine(line);
            if (output.Length > maxCharacters)
            {
                output.Remove(0, output.Length - maxCharacters);
            }
        }

        private static string BuildMapArgs(string videoLabel, string audioLabel, bool includeAudio)
        {
            string result = $" -map \"{videoLabel}\"";
            if (includeAudio && !string.IsNullOrWhiteSpace(audioLabel))
            {
                result += $" -map \"{audioLabel}\"";
            }

            return result;
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
            string destination = GetIndexedOutputPath(outputFile);
            int attempts = 0;

            while (attempts < 100)
            {
                try
                {
                    File.Move(tempFile, destination);
                    return destination;
                }
                catch (IOException)
                {
                    attempts++;
                    destination = GetIndexedOutputPath(outputFile);
                }
            }

            throw new IOException("Unable to create a unique output file name.");
        }

        private string GetIndexedOutputPath(string outputFile)
        {
            if (!File.Exists(outputFile))
            {
                return outputFile;
            }

            string directory = Path.GetDirectoryName(outputFile);
            string baseName = Path.GetFileNameWithoutExtension(outputFile);
            string extension = Path.GetExtension(outputFile);

            int index = 1;
            while (true)
            {
                string candidate = Path.Combine(directory, $"{baseName}_{index}{extension}");
                if (!File.Exists(candidate))
                {
                    return candidate;
                }

                index++;
            }
        }
    }
}
