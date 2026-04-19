using Microsoft.AspNetCore.Http;

namespace g19_sep490_ealds.Server.Services;

public interface IFileStorageService
{
    Task<FileUploadStorageResult> UploadAsync(IFormFile file, HttpRequest request, CancellationToken cancellationToken = default);
}

public sealed record FileUploadStorageResult(string FileName, string Url);
