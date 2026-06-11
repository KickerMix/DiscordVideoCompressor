using System.Text;

namespace DiscordVideoCompressor.Tests;

public class DatamoshTransformerTests
{
    [Fact]
    public void Transform_ConvertsEveryIdrAfterTheFirst_WithoutLoadingWholeFile()
    {
        byte[] inputBytes =
        {
            0x00, 0x00, 0x01, 0x67, 0x11,
            0x00, 0x00, 0x00, 0x01, 0x65, 0x22,
            0x00, 0x00, 0x01, 0x41, 0x33,
            0x00, 0x00, 0x00, 0x01, 0x65, 0x44
        };
        using var input = new MemoryStream(inputBytes);
        using var output = new MemoryStream();

        int converted = DatamoshTransformer.Transform(input, output, CancellationToken.None);
        byte[] result = output.ToArray();

        Assert.Equal(1, converted);
        Assert.Equal(0x65, result[9]);
        Assert.Equal(0x61, result[20]);
    }

    [Fact]
    public void Transform_HonorsCancellation()
    {
        using var input = new MemoryStream(Encoding.ASCII.GetBytes("not important"));
        using var output = new MemoryStream();
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        Assert.Throws<OperationCanceledException>(() => DatamoshTransformer.Transform(input, output, cancellation.Token));
    }
}
