using Google.Apis.Auth.OAuth2;
using Google.Cloud.Storage.V1;
using g19_sep490_ealds.Server.Configuration;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace g19_sep490_ealds.Server.Services.Implementation;

public sealed class GoogleCloudFileStorageService : IFileStorageService
{
    private readonly GoogleCloudStorageSettings _settings;
    private readonly StorageClient _client;

    public GoogleCloudFileStorageService(
        IOptions<GoogleCloudStorageSettings> options,
        IHostEnvironment hostEnvironment)
    {
        _settings = options.Value;
        _client = CreateClient(_settings, hostEnvironment);
    }

    private static StorageClient CreateClient(GoogleCloudStorageSettings settings, IHostEnvironment hostEnvironment)
    {
        if (!string.IsNullOrWhiteSpace(settings.CredentialsPath))
        {
            var path = Path.IsPathRooted(settings.CredentialsPath)
                ? settings.CredentialsPath
                : Path.Combine(hostEnvironment.ContentRootPath, settings.CredentialsPath);
            if (File.Exists(path))
                return StorageClient.Create(GoogleCredential.FromFile(path));

            throw new InvalidOperationException(
                $"Google Cloud Storage: credentials file not found at '{path}'. " +
                "Fix GoogleCloudStorage:CredentialsPath or place the service account JSON there.");
        }

        var adcEnv = Environment.GetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS");
        if (!string.IsNullOrWhiteSpace(adcEnv))
        {
            if (File.Exists(adcEnv))
                return StorageClient.Create(GoogleCredential.FromFile(adcEnv));

            throw new InvalidOperationException(
                $"GOOGLE_APPLICATION_CREDENTIALS is set to '{adcEnv}' but that file does not exist.");
        }

        try
        {
            return StorageClient.Create();
        }
        catch (InvalidOperationException ex)
        {
            throw new InvalidOperationException(
                "Google Cloud Storage has a bucket configured but no credentials were found. " +
                "Do one of the following: (1) Set GoogleCloudStorage:CredentialsPath in appsettings to your service account JSON " +
                $"(path is relative to the server folder: '{hostEnvironment.ContentRootPath}'), " +
                "(2) Set environment variable GOOGLE_APPLICATION_CREDENTIALS to the full path of that JSON file, or " +
                "(3) On a dev machine only, run: gcloud auth application-default login. " +
                "See https://cloud.google.com/docs/authentication/provide-credentials-adc",
                ex);
        }
    }

    public async Task<FileUploadStorageResult> UploadAsync(
        IFormFile file,
        HttpRequest _,
        CancellationToken cancellationToken = default)
    {

        var safeName = Path.GetFileName(file.FileName);
        var ext = Path.GetExtension(safeName);
        var generatedName = $"{Guid.NewGuid():N}{ext}";

        var prefix = string.IsNullOrWhiteSpace(_settings.ObjectPrefix)
            ? "uploads"
            : _settings.ObjectPrefix.Trim().Trim('/');
        if (string.IsNullOrEmpty(prefix))
            prefix = "uploads";

        var objectName = $"{prefix}/{generatedName}";
        var contentType = string.IsNullOrWhiteSpace(file.ContentType)
            ? "application/octet-stream"
            : file.ContentType;

        await using var stream = file.OpenReadStream();
        await _client.UploadObjectAsync(
            _settings.BucketName,
            objectName,
            contentType,
            stream,
            cancellationToken: cancellationToken);

        var url = BuildPublicUrl(objectName);
        return new FileUploadStorageResult(safeName, url);
    }

    private string BuildPublicUrl(string objectName)
    {
        if (!string.IsNullOrWhiteSpace(_settings.PublicUrlBase))
            return $"{_settings.PublicUrlBase!.TrimEnd('/')}/{objectName}";

        var encodedObject = string.Join("/", objectName
            .Split('/', StringSplitOptions.RemoveEmptyEntries)
            .Select(Uri.EscapeDataString));

        return $"https://storage.googleapis.com/{_settings.BucketName}/{encodedObject}";
    }
}
