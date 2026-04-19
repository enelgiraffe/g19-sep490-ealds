using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;

namespace g19_sep490_ealds.Server.Services.Implementation;

public sealed class LocalFileStorageService : IFileStorageService
{
    private readonly IWebHostEnvironment _environment;

    public LocalFileStorageService(IWebHostEnvironment environment)
    {
        _environment = environment;
    }

    public async Task<FileUploadStorageResult> UploadAsync(
        IFormFile file,
        HttpRequest request,
        CancellationToken cancellationToken = default)
    {
        var uploadsRoot = Path.Combine(_environment.ContentRootPath, "wwwroot", "uploads");
        Directory.CreateDirectory(uploadsRoot);

        var safeName = Path.GetFileName(file.FileName);
        var ext = Path.GetExtension(safeName);
        var generatedName = $"{Guid.NewGuid():N}{ext}";
        var fullPath = Path.Combine(uploadsRoot, generatedName);

        await using (var stream = new FileStream(fullPath, FileMode.Create, FileAccess.Write, FileShare.None, 81920, useAsync: true))
        {
            await file.CopyToAsync(stream, cancellationToken);
        }

        var url = $"{request.Scheme}://{request.Host}/uploads/{generatedName}";
        return new FileUploadStorageResult(safeName, url);
    }
}
