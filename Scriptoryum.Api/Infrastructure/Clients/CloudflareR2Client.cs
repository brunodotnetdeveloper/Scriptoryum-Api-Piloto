using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Options;
using Scriptoryum.Api.Infrastructure.Configuration;

namespace Scriptoryum.Api.Infrastructure.Clients;

public class CloudflareR2Client : ICloudflareR2Client
{
    private readonly CloudflareR2Options _options;
    private readonly ILogger<CloudflareR2Client> _logger;
    private readonly AmazonS3Client _s3Client;

    public CloudflareR2Client(IOptions<CloudflareR2Options> options, ILogger<CloudflareR2Client> logger)
    {
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        if (!_options.IsValid())
        {
            throw new InvalidOperationException("CloudflareR2Options configuration is invalid. Please check all required fields.");
        }

        var credentials = new BasicAWSCredentials(_options.AccessKey, _options.SecretKey);

        var config = new AmazonS3Config
        {
            ServiceURL = _options.ServiceUrl,
            ForcePathStyle = true,
            UseAccelerateEndpoint = false,
            UseDualstackEndpoint = false,
            // SignatureVersion = "4", // Explicitly set signature version
            SignatureMethod = SigningAlgorithm.HmacSHA256, // Ensure correct signing method
            UseHttp = false, // Force HTTPS
            DisableHostPrefixInjection = true // Important for R2 compatibility
        };

        _s3Client = new AmazonS3Client(credentials, config);
    }

    public async Task<bool> UploadFileAsync(Stream fileStream, string objectKey, string contentType = "application/octet-stream")
    {
        if (fileStream == null)
            throw new ArgumentNullException(nameof(fileStream));
        if (string.IsNullOrWhiteSpace(objectKey))
            throw new ArgumentException("Object key cannot be null or empty", nameof(objectKey));
        if (string.IsNullOrWhiteSpace(contentType))
            throw new ArgumentException("Content type cannot be null or empty", nameof(contentType));

        try
        {
            var request = new PutObjectRequest
            {
                BucketName = _options.BucketName,
                Key = objectKey,
                InputStream = fileStream,
                ContentType = contentType,
                ServerSideEncryptionMethod = ServerSideEncryptionMethod.None,
                DisablePayloadSigning = false, // Enable payload signing for better compatibility
                UseChunkEncoding = false // Disable chunked encoding
            };

            var response = await _s3Client.PutObjectAsync(request);
            
            _logger.LogInformation("Arquivo {ObjectKey} enviado com sucesso para R2. ETag: {ETag}", objectKey, response.ETag);
            return response.HttpStatusCode == System.Net.HttpStatusCode.OK;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao fazer upload do arquivo {ObjectKey} para R2", objectKey);
            return false;
        }
    }

    public async Task<bool> DeleteFileAsync(string objectKey)
    {
        if (string.IsNullOrWhiteSpace(objectKey))
            throw new ArgumentException("Object key cannot be null or empty", nameof(objectKey));

        try
        {
            var request = new DeleteObjectRequest
            {
                BucketName = _options.BucketName,
                Key = objectKey
            };

            var response = await _s3Client.DeleteObjectAsync(request);
            
            _logger.LogInformation("Arquivo {ObjectKey} deletado com sucesso do R2", objectKey);
            return response.HttpStatusCode == System.Net.HttpStatusCode.NoContent;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao deletar arquivo {ObjectKey} do R2", objectKey);
            return false;
        }
    }

    public async Task<string> GetPresignedUrlAsync(string objectKey, TimeSpan expiration)
    {
        if (string.IsNullOrWhiteSpace(objectKey))
            throw new ArgumentException("Object key cannot be null or empty", nameof(objectKey));
        if (expiration <= TimeSpan.Zero)
            throw new ArgumentException("Expiration must be greater than zero", nameof(expiration));

        try
        {
            var request = new GetPreSignedUrlRequest
            {
                BucketName = _options.BucketName,
                Key = objectKey,
                Expires = DateTime.UtcNow.Add(expiration),
                Verb = HttpVerb.GET
            };

            return await _s3Client.GetPreSignedURLAsync(request);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao gerar URL pré-assinada para {ObjectKey}", objectKey);
            return null;
        }
    }

    public void Dispose()
    {
        _s3Client?.Dispose();
        GC.SuppressFinalize(this);
    }
}
