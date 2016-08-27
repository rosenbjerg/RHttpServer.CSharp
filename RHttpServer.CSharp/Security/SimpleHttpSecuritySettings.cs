namespace RHttpServer.Security
{
    /// <summary>
    /// The default security settings
    /// </summary>
    public sealed class SimpleHttpSecuritySettings : IHttpSecuritySettings
    {
        public SimpleHttpSecuritySettings(int sessLenSec = 600, int maxReqsPrSess = 1000)
        {
            SessionLengthSeconds = sessLenSec;
            MaxRequestsPerSession = maxReqsPrSess;
        }

        public int SessionLengthSeconds { get; set; }
        public int MaxRequestsPerSession { get; set; }
    }
}