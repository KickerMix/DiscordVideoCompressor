namespace DiscordVideoCompressor.Tests;

public class FfmpegBinaryManagerTests
{
    [Fact]
    public void GetOrExtract_ReusesContentAddressedBinaryAndRemovesLegacyCopy()
    {
        string firstPath = FfmpegBinaryManager.GetOrExtract(
            typeof(FfmpegConversionService).Assembly,
            "DiscordVideoCompressor.Resources.ffmpeg.exe");
        string legacyPath = Path.Combine(Path.GetDirectoryName(firstPath)!, "ffmpeg_legacy.exe");
        File.WriteAllBytes(legacyPath, new byte[] { (byte)'M', (byte)'Z', 0 });

        string secondPath = FfmpegBinaryManager.GetOrExtract(
            typeof(FfmpegConversionService).Assembly,
            "DiscordVideoCompressor.Resources.ffmpeg.exe");

        Assert.Equal(firstPath, secondPath);
        Assert.True(FfmpegBinaryManager.IsValidExecutable(secondPath));
        Assert.False(File.Exists(legacyPath));
    }
}
