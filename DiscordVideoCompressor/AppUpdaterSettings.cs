namespace DiscordVideoCompressor
{
    internal static class AppUpdaterSettings
    {
        // The release workflow publishes this signed appcast with every release.
        public const string AppCastUrl = "https://github.com/KickerMix/DiscordVideoCompressor/releases/latest/download/appcast.xml";
        public const string PublicEd25519Key = "Szu5M/jnXn/lVUgi7d4GX29IWi6QPy5Ua16YY8OWJyU=";

        // Keep the guard so development forks can disable updates with empty values.
        public static bool IsConfigured =>
            !string.IsNullOrWhiteSpace(AppCastUrl) &&
            !string.IsNullOrWhiteSpace(PublicEd25519Key) &&
            PublicEd25519Key.Length > 20;
    }
}
