namespace DiscordVideoCompressor
{
    internal static class AppUpdaterSettings
    {
        // Replace these values with your published appcast URL and NetSparkle
        // Ed25519 public key when you are ready to ship automatic updates.
        public const string AppCastUrl = "https://github.com/KickerMix/DiscordVideoCompressor/releases/latest/download/appcast.xml";
        public const string PublicEd25519Key = "Szu5M/jnXn/lVUgi7d4GX29IWi6QPy5Ua16YY8OWJyU=";

        // Automatic update checks stay disabled until both values are configured.
        public static bool IsConfigured =>
            !string.IsNullOrWhiteSpace(AppCastUrl) &&
            !string.IsNullOrWhiteSpace(PublicEd25519Key) &&
            PublicEd25519Key.Length > 20;
    }
}
