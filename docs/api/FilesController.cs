using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RaceDay.Api.Services;
using RaceDay.Shared;

namespace RaceDay.Api.Controllers;

[Route("api/files")]
[Authorize]
public class FilesController : ApiControllerBase
{
    private static readonly string[] AllowedExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
    private const long MaxBytes = 5 * 1024 * 1024;

    private readonly IFileStorage _storage;
    private readonly IConfiguration _config;

    public FilesController(IFileStorage storage, IConfiguration config)
    {
        _storage = storage;
        _config = config;
    }

    /// <summary>Upload an image (event picture or profile photo). Stored in Azure Blob Storage when configured, otherwise locally.</summary>
    [HttpPost("upload")]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<FileUploadResultDto>> Upload(IFormFile file, CancellationToken ct)
    {
        if (file == null || file.Length == 0) return Fail(400, "No file was uploaded.");
        if (file.Length > MaxBytes) return Fail(400, "The image must be 5 MB or smaller.");

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (Array.IndexOf(AllowedExtensions, ext) < 0 || file.ContentType == null || !file.ContentType.StartsWith("image/"))
            return Fail(400, "Only JPG, PNG, GIF or WEBP images are allowed.");

        var baseUrl = _config["PublicBaseUrl"];
        if (string.IsNullOrWhiteSpace(baseUrl)) baseUrl = $"{Request.Scheme}://{Request.Host}";

        var storedName = $"{Guid.NewGuid():N}{ext}";
        await using var stream = file.OpenReadStream();
        var url = await _storage.SaveAsync(stream, storedName, file.ContentType, baseUrl, ct);
        return Ok(new FileUploadResultDto { Url = url });
    }
}
