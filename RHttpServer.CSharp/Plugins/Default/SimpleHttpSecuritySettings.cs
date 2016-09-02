namespace RHttpServer.Plugins.Default
{
    /// <summary>
    /// The default security settings
    /// </summary>
    public sealed class SimpleHttpSecuritySettings : IHttpSecuritySettings
    {
        public SimpleHttpSecuritySettings(int sessLenSec = 600, int maxReqsPrSess = 1000, int banTimeMin = 60)
        {
            SessionLengthSeconds = sessLenSec;
            MaxRequestsPerSession = maxReqsPrSess;
            BanTimeMinutes = banTimeMin;
        }

        public int SessionLengthSeconds { get; set; }
        public int MaxRequestsPerSession { get; set; }
        public int BanTimeMinutes { get; set; }
    }
}