namespace g19_sep490_ealds.Server.Configuration;

public class GoogleCloudStorageSettings
{
    public const string SectionName = "GoogleCloudStorage";

    /// <summary>
    /// GCS bucket name. When empty, uploads use local wwwroot/uploads instead.
    /// </summary>
    public string BucketName { get; set; } = "";

    /// <summary>
    /// Optional prefix for object names (no leading/trailing slashes required).
    /// </summary>
    public string ObjectPrefix { get; set; } = "uploads";

    /// <summary>
    /// Path to service account JSON. If empty, uses Application Default Credentials
    /// (e.g. GOOGLE_APPLICATION_CREDENTIALS environment variable).
    /// </summary>
    public string CredentialsPath { get; set; } = "";

    /// <summary>
    /// Optional base URL for returned links (e.g. CDN). Must not end with a slash.
    /// When empty, uses https://storage.googleapis.com/{bucket}/{object}
    /// </summary>
    public string? PublicUrlBase { get; set; }
}
