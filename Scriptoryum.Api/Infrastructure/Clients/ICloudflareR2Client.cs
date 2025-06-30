namespace Scriptoryum.Api.Infrastructure.Clients;

public interface ICloudflareR2Client : IDisposable
{
    /// <summary>
    /// Uploads a file to Cloudflare R2 storage
    /// </summary>
    /// <param name="fileStream">The file stream to upload</param>
    /// <param name="objectKey">The unique key/path for the object in R2</param>
    /// <param name="contentType">The MIME type of the file</param>
    /// <returns>True if upload was successful, false otherwise</returns>
    Task<bool> UploadFileAsync(Stream fileStream, string objectKey, string contentType = "application/octet-stream");

    /// <summary>
    /// Deletes a file from Cloudflare R2 storage
    /// </summary>
    /// <param name="objectKey">The unique key/path of the object to delete</param>
    /// <returns>True if deletion was successful, false otherwise</returns>
    Task<bool> DeleteFileAsync(string objectKey);

    /// <summary>
    /// Generates a pre-signed URL for downloading a file from R2
    /// </summary>
    /// <param name="objectKey">The unique key/path of the object</param>
    /// <param name="expiration">How long the URL should remain valid</param>
    /// <returns>The pre-signed URL or null if generation failed</returns>
    Task<string> GetPresignedUrlAsync(string objectKey, TimeSpan expiration);
}