using System;
using System.IO;
using System.Threading;

namespace DiscordVideoCompressor
{
    internal static class DatamoshTransformer
    {
        private const int BufferSize = 1024 * 1024;

        public static bool Transform(string inputPath, string outputPath, CancellationToken token, out string error)
        {
            error = string.Empty;

            try
            {
                using var input = new FileStream(inputPath, FileMode.Open, FileAccess.Read, FileShare.Read, BufferSize, FileOptions.SequentialScan);
                using var output = new FileStream(outputPath, FileMode.Create, FileAccess.Write, FileShare.None, BufferSize, FileOptions.SequentialScan);
                Transform(input, output, token);
                return true;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                error = "Datamosh transform failed: " + ex.Message;
                return false;
            }
        }

        internal static int Transform(Stream input, Stream output, CancellationToken token)
        {
            byte[] buffer = new byte[BufferSize];
            int zeroCount = 0;
            bool expectNalHeader = false;
            bool firstIdrSeen = false;
            int convertedIdrCount = 0;
            int bytesRead;

            while ((bytesRead = input.Read(buffer, 0, buffer.Length)) > 0)
            {
                token.ThrowIfCancellationRequested();

                for (int i = 0; i < bytesRead; i++)
                {
                    byte value = buffer[i];
                    if (expectNalHeader)
                    {
                        int nalType = value & 0x1F;
                        if (nalType == 5)
                        {
                            if (firstIdrSeen)
                            {
                                value = (byte)((value & 0xE0) | 0x01);
                                convertedIdrCount++;
                            }

                            firstIdrSeen = true;
                        }

                        expectNalHeader = false;
                    }

                    output.WriteByte(value);

                    if (value == 0)
                    {
                        zeroCount = Math.Min(3, zeroCount + 1);
                    }
                    else if (value == 1 && zeroCount >= 2)
                    {
                        expectNalHeader = true;
                        zeroCount = 0;
                    }
                    else
                    {
                        zeroCount = 0;
                    }
                }
            }

            return convertedIdrCount;
        }
    }
}
