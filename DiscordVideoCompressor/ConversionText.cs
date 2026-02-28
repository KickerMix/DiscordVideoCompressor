namespace DiscordVideoCompressor
{
    internal sealed class ConversionText
    {
        public string FileNotFound { get; init; }

        public string DurationError { get; init; }

        public string TimeConversionError { get; init; }

        public string VideoDurationError { get; init; }

        public string DatamoshSpeedConflict { get; init; }

        public string DatamoshMp4Only { get; init; }

        public string BitrateError { get; init; }

        public string InvalidFileFormat { get; init; }

        public string ConversionErrorPrefix { get; init; }

        public string FileConversionError { get; init; }

        public string DatamoshAudioMissing { get; init; }

        public string ProgressStagePrepare { get; init; }

        public string ProgressStageGlitch { get; init; }

        public string ProgressStageSpeed { get; init; }

        public string ProgressStagePass1 { get; init; }

        public string ProgressStagePass2 { get; init; }

        public string ProgressStageDatamoshEncode { get; init; }

        public string ProgressStageDatamoshTransform { get; init; }

        public string ProgressStageDatamoshRemux { get; init; }
    }
}
