namespace DiscordVideoCompressor
{
    internal enum ConversionValidationError
    {
        None,
        MissingFile,
        DatamoshSpeedConflict,
        DatamoshMp4Only,
        BitrateTooLow,
        InvalidOutputFormat
    }

    internal static class ConversionValidation
    {
        public static ConversionValidationError ValidateRequest(bool fileExists, ConversionOptions options, string normalizedOutputFormat, long videoBitrate)
        {
            if (!fileExists)
            {
                return ConversionValidationError.MissingFile;
            }

            if (options.EnableDatamosh && options.EnableSpeedEffect)
            {
                return ConversionValidationError.DatamoshSpeedConflict;
            }

            if (options.EnableDatamosh && normalizedOutputFormat != "mp4")
            {
                return ConversionValidationError.DatamoshMp4Only;
            }

            if (normalizedOutputFormat != "mp4" && normalizedOutputFormat != "webm")
            {
                return ConversionValidationError.InvalidOutputFormat;
            }

            if (videoBitrate <= 0)
            {
                return ConversionValidationError.BitrateTooLow;
            }

            return ConversionValidationError.None;
        }
    }
}
