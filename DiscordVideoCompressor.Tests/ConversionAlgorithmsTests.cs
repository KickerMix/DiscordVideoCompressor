namespace DiscordVideoCompressor.Tests;

public class ConversionAlgorithmsTests
{
    [Fact]
    public void SelectAudioBitrate_ClampsToKnownPresetRange()
    {
        Assert.Equal(32000, ConversionAlgorithms.SelectAudioBitrate(1));
        Assert.Equal(128000, ConversionAlgorithms.SelectAudioBitrate(10_000_000));
    }

    [Fact]
    public void CalculateVideoBitrate_ReservesContainerAndAudioBudget()
    {
        long bitrate = ConversionAlgorithms.CalculateVideoBitrate(10 * 1024 * 1024, 60, 96_000);

        Assert.InRange(bitrate, 1_260_000, 1_280_000);
        Assert.Equal(0, ConversionAlgorithms.CalculateVideoBitrate(0, 60, 0));
        Assert.Equal(0, ConversionAlgorithms.CalculateVideoBitrate(100, 0, 0));
    }

    [Theory]
    [InlineData("webm", 44100, 48000)]
    [InlineData("webm", 22050, 24000)]
    [InlineData("mp4", 44100, 44100)]
    public void NormalizeAudioSampleRate_UsesEncoderSupportedValue(string format, int requested, int expected)
    {
        Assert.Equal(expected, ConversionAlgorithms.NormalizeAudioSampleRate(format, requested));
    }

    [Fact]
    public void BuildSpeedSegments_ReturnsBoundedContinuousSegments()
    {
        var segments = ConversionAlgorithms.BuildSpeedSegments(12);

        Assert.NotEmpty(segments);
        Assert.Equal(0, segments[0].Start, 6);
        Assert.True(segments[^1].End <= 12);
        Assert.All(segments, segment =>
        {
            Assert.True(segment.End > segment.Start);
            Assert.InRange(segment.Speed, 0.1, 1.4);
        });
    }

    [Fact]
    public void BuildDatamoshSegments_ProducesAtLeastOneEffect_WhenChanceIsPositive()
    {
        var segments = ConversionAlgorithms.BuildDatamoshSegments(10, 12345, 0.8, 0.25);

        Assert.NotEmpty(segments);
        Assert.Contains(segments, segment => segment.ApplyEffect);
        Assert.All(segments, segment =>
        {
            Assert.True(segment.End > segment.Start);
            Assert.InRange(segment.SourceStart, 0, 10);
        });
    }

    [Fact]
    public void BuildDatamoshSegments_ClampsChanceToZero()
    {
        var segments = ConversionAlgorithms.BuildDatamoshSegments(5, 42, 0.5, -1);

        Assert.NotEmpty(segments);
        Assert.DoesNotContain(segments, segment => segment.ApplyEffect);
    }

    [Fact]
    public void BuildSpeedFilterComplex_IncludesConcatScaleFpsAndAudioOutput()
    {
        string filter = FilterGraphBuilder.BuildSpeedFilterComplex(8, "1280x720", "s16", 44100, 44100, 30, null, true, out string videoOutLabel, out string audioOutLabel);

        Assert.Contains("concat=n=", filter);
        Assert.Contains("scale=1280:720", filter);
        Assert.Contains("fps=fps=30", filter);
        Assert.Contains("[aout]", filter);
        Assert.Equal("[vout]", videoOutLabel);
        Assert.Equal("[aout]", audioOutLabel);
    }

    [Fact]
    public void BuildDatamoshFilterComplex_UsesEffectSeedAndConcat()
    {
        var segments = new[]
        {
            new DatamoshSegment(0, 1, 0.2, true, 777),
            new DatamoshSegment(1, 2, 1.0, false, 888)
        };

        string filter = FilterGraphBuilder.BuildDatamoshFilterComplex("640x360", "u8", 44100, 24, new System.Collections.Generic.List<DatamoshSegment>(segments), true, out _, out _);

        Assert.Contains("random=frames=30:seed=777", filter);
        Assert.Contains("concat=n=2:v=1:a=1", filter);
        Assert.Contains("fps=fps=24", filter);
    }

    [Fact]
    public void FilterBuilders_OmitAudioGraph_WhenInputHasNoAudio()
    {
        string speedFilter = FilterGraphBuilder.BuildSpeedFilterComplex(2, "640x360", "s16", 44100, 44100, 24, null, false, out _, out string speedAudioLabel);
        var segments = ConversionAlgorithms.BuildDatamoshSegments(2, 42, 0.5, 0.5);
        string glitchFilter = FilterGraphBuilder.BuildDatamoshFilterComplex("640x360", "s16", 44100, 24, segments, false, out _, out string glitchAudioLabel);

        Assert.DoesNotContain("[0:a]", speedFilter);
        Assert.DoesNotContain("[0:a]", glitchFilter);
        Assert.Null(speedAudioLabel);
        Assert.Null(glitchAudioLabel);
    }

    [Fact]
    public void ValidateRequest_ReturnsConflictForDatamoshAndSpeed()
    {
        var options = new ConversionOptions
        {
            OutputFormat = "MP4",
            EnableDatamosh = true,
            EnableSpeedEffect = true
        };

        var result = ConversionValidation.ValidateRequest(true, options, "mp4", 10);

        Assert.Equal(ConversionValidationError.DatamoshSpeedConflict, result);
    }

    [Fact]
    public void ValidateRequest_RejectsDatamoshForWebm()
    {
        var options = new ConversionOptions
        {
            OutputFormat = "WEBM",
            EnableDatamosh = true
        };

        var result = ConversionValidation.ValidateRequest(true, options, "webm", 10);

        Assert.Equal(ConversionValidationError.DatamoshMp4Only, result);
    }
}
