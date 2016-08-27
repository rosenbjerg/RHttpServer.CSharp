namespace RHttpServer.Security
{
    /// <summary>
    /// The minimum interface that must be implented in a class for it to be used as security settings
    /// </summary>
    public interface IHttpSecuritySettings
    {
        int SessionLengthSeconds { get; set; }
        int MaxRequestsPerSession { get; set; }
    }
}