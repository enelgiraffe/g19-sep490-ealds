using g19_sep490_ealds.Server.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace g19_sep490_ealds.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class FilesController : ControllerBase
{
    private readonly IFileStorageService _fileStorage;

    public FilesController(IFileStorageService fileStorage)
    {
        _fileStorage = fileStorage;
    }

    public class UploadFileForm
    {
        public IFormFile? File { get; set; }
    }

    [HttpPost("upload")]
    [Authorize]
    [RequestSizeLimit(20_000_000)]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> Upload([FromForm] UploadFileForm form)
    {
        var file = form.File;
        if (file == null || file.Length == 0)
            return BadRequest("File is required.");

        var result = await _fileStorage.UploadAsync(file, Request, HttpContext.RequestAborted);
        return Ok(new { fileName = result.FileName, url = result.Url });
    }
}

