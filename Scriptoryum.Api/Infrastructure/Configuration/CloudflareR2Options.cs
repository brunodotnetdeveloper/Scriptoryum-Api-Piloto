using System.ComponentModel.DataAnnotations;

namespace Scriptoryum.Api.Infrastructure.Configuration;

public class CloudflareR2Options
{
    public const string SectionName = "CloudflareR2";

    [Required(ErrorMessage = "BucketName is required")]
    public string BucketName { get; set; } = string.Empty;

    [Required(ErrorMessage = "AccessKey is required")]
    public string AccessKey { get; set; } = string.Empty;

    [Required(ErrorMessage = "SecretKey is required")]
    public string SecretKey { get; set; } = string.Empty;

    [Required(ErrorMessage = "ServiceUrl is required")]
    [Url(ErrorMessage = "ServiceUrl must be a valid URL")]
    public string ServiceUrl { get; set; } = string.Empty;

    /// <summary>
    /// Maximum file size allowed for uploads in bytes (default: 50MB)
    /// </summary>
    public long MaxFileSizeBytes { get; set; } = 50 * 1024 * 1024;

    /// <summary>
    /// Default expiration time for pre-signed URLs (default: 1 hour)
    /// </summary>
    public TimeSpan DefaultUrlExpiration { get; set; } = TimeSpan.FromHours(1);

    /// <summary>
    /// Validates the configuration options
    /// </summary>
    /// <returns>True if all required fields are valid</returns>
    public bool IsValid()
    {
        return !string.IsNullOrWhiteSpace(BucketName) &&
               !string.IsNullOrWhiteSpace(AccessKey) &&
               !string.IsNullOrWhiteSpace(SecretKey) &&
               !string.IsNullOrWhiteSpace(ServiceUrl) &&
               Uri.IsWellFormedUriString(ServiceUrl, UriKind.Absolute) &&
               MaxFileSizeBytes > 0;
    }
}