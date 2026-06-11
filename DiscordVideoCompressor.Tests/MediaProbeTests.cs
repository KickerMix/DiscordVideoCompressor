namespace DiscordVideoCompressor.Tests;

public class MediaProbeTests
{
    [Fact]
    public void TryParse_ReadsDurationAndAudioSampleRate()
    {
        const string output = "Duration: 01:02:03.45, start: 0.000000, bitrate: 1000 kb/s\nStream #0:1: Audio: aac, 48000 Hz, stereo";

        bool parsed = MediaProbe.TryParse(output, 44100, out MediaProbeResult result);

        Assert.True(parsed);
        Assert.Equal(3723.45, result.Duration, 2);
        Assert.True(result.HasAudio);
        Assert.Equal(48000, result.AudioSampleRate);
    }

    [Fact]
    public void TryParse_HandlesSilentVideo()
    {
        const string output = "Duration: 00:00:02.00, start: 0.000000, bitrate: 500 kb/s\nStream #0:0: Video: h264";

        bool parsed = MediaProbe.TryParse(output, 44100, out MediaProbeResult result);

        Assert.True(parsed);
        Assert.False(result.HasAudio);
        Assert.Equal(44100, result.AudioSampleRate);
    }
}
