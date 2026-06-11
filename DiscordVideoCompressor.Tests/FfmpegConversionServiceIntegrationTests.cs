using System.Diagnostics;
using System.Globalization;

namespace DiscordVideoCompressor.Tests;

public sealed class FfmpegConversionServiceIntegrationTests : IDisposable
{
    private readonly string tempDirectory = Path.Combine(Path.GetTempPath(), "DiscordVideoCompressor.Tests", Guid.NewGuid().ToString("N"));
    private readonly string ffmpegPath;

    public FfmpegConversionServiceIntegrationTests()
    {
        Directory.CreateDirectory(tempDirectory);
        ffmpegPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "DiscordVideoCompressor", "Resources", "ffmpeg.exe"));
        Assert.True(FfmpegBinaryManager.IsValidExecutable(ffmpegPath), $"ffmpeg test payload not found: {ffmpegPath}");
    }

    [Theory]
    [InlineData("mp4", true)]
    [InlineData("mp4", false)]
    [InlineData("webm", true)]
    [InlineData("webm", false)]
    public void ConvertFile_ProducesOutputWithinTargetSize(string format, bool withAudio)
    {
        string input = CreateInput(withAudio, 1.5);
        var options = CreateOptions(format);

        string output = CreateService().ConvertFile(input, options, CancellationToken.None, out string warning);

        Assert.True(File.Exists(output));
        Assert.True(new FileInfo(output).Length <= options.TargetSizeBytes);
        Assert.Null(warning);
        Assert.Empty(Directory.GetFiles(tempDirectory, ".*.tmp.*"));
    }

    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    public void ConvertFile_EffectsSupportSilentVideo(bool speed, bool glitch)
    {
        string input = CreateInput(false, 1.5);
        ConversionOptions options = CreateOptions("mp4", speed, glitch);

        string output = CreateService().ConvertFile(input, options, CancellationToken.None, out _);

        Assert.True(File.Exists(output));
        Assert.True(new FileInfo(output).Length <= options.TargetSizeBytes);
    }

    [Fact]
    public void ConvertFile_DatamoshUsesStreamingTransform()
    {
        string input = CreateInput(false, 2.2);
        ConversionOptions options = CreateOptions("mp4", datamosh: true);

        string output = CreateService().ConvertFile(input, options, CancellationToken.None, out _);

        Assert.True(File.Exists(output));
        Assert.True(new FileInfo(output).Length <= options.TargetSizeBytes);
    }

    [Fact]
    public void ConvertFile_HonorsCancellationAndRemovesTemporaryOutput()
    {
        string input = CreateInput(false, 20, "640x360");
        ConversionOptions options = CreateOptions("mp4", resolution: "640x360");
        using var cancellation = new CancellationTokenSource();
        cancellation.CancelAfter(50);

        Assert.Throws<OperationCanceledException>(() => CreateService().ConvertFile(input, options, cancellation.Token, out _));
        Assert.Empty(Directory.GetFiles(tempDirectory, ".*.tmp.*"));
    }

    private FfmpegConversionService CreateService()
    {
        return new FfmpegConversionService(
            (_, _, _, _, _) => { },
            (_, _) => { },
            FilterGraphBuilder.BuildVideoFilter,
            FilterGraphBuilder.BuildAudioFilter,
            FilterGraphBuilder.BuildSpeedFilterComplex,
            FilterGraphBuilder.BuildDatamoshFilterComplex,
            ConversionAlgorithms.BuildDatamoshSegments,
            ConversionAlgorithms.SelectAudioBitrate,
            CreateText(),
            () => ffmpegPath);
    }

    private string CreateInput(bool withAudio, double duration, string resolution = "320x240")
    {
        string path = Path.Combine(tempDirectory, $"input-{Guid.NewGuid():N}.mp4");
        string durationText = duration.ToString(CultureInfo.InvariantCulture);
        string audioInput = withAudio ? $" -f lavfi -i \"sine=frequency=1000:duration={durationText}\"" : string.Empty;
        string audioOutput = withAudio ? " -c:a aac -shortest" : " -an";
        RunFfmpeg($"-hide_banner -loglevel error -f lavfi -i \"testsrc2=size={resolution}:rate=24:duration={durationText}\"{audioInput} -c:v libx264{audioOutput} -y \"{path}\"");
        return path;
    }

    private void RunFfmpeg(string arguments)
    {
        using var process = Process.Start(new ProcessStartInfo
        {
            FileName = ffmpegPath,
            Arguments = arguments,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardError = true
        });
        Assert.NotNull(process);
        string error = process.StandardError.ReadToEnd();
        process.WaitForExit();
        Assert.True(process.ExitCode == 0, error);
    }

    private static ConversionOptions CreateOptions(string format, bool speed = false, bool glitch = false, bool datamosh = false, string resolution = "320x240")
    {
        return new ConversionOptions
        {
            TargetSizeBytes = 512 * 1024,
            OutputFormat = format,
            AudioSampleRate = 44100,
            AudioBitDepth = "s16",
            VideoResolution = resolution,
            VideoFps = 24,
            EnableSpeedEffect = speed,
            EnableGlitchEffect = glitch,
            EnableDatamosh = datamosh,
            GlitchJumpSeconds = 0.3,
            GlitchChance = 0.5
        };
    }

    private static ConversionText CreateText()
    {
        return new ConversionText
        {
            FileNotFound = "File not found",
            DurationError = "Duration error",
            TimeConversionError = "Time conversion error",
            VideoDurationError = "Video duration error",
            DatamoshSpeedConflict = "Conflict",
            DatamoshMp4Only = "MP4 only",
            BitrateError = "Bitrate error",
            InvalidFileFormat = "Invalid format",
            ConversionErrorPrefix = "Conversion failed: ",
            FileConversionError = "Unable to fit target size",
            DatamoshAudioMissing = "Audio missing",
            ProgressStagePrepare = "Prepare",
            ProgressStageGlitch = "Glitch",
            ProgressStageSpeed = "Speed",
            ProgressStagePass1 = "Pass 1",
            ProgressStagePass2 = "Pass 2",
            ProgressStageDatamoshEncode = "Datamosh encode",
            ProgressStageDatamoshTransform = "Datamosh transform",
            ProgressStageDatamoshRemux = "Datamosh remux"
        };
    }

    public void Dispose()
    {
        if (Directory.Exists(tempDirectory))
        {
            Directory.Delete(tempDirectory, true);
        }
    }
}
