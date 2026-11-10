using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Models;
using RaceDay.Api.Services;

namespace RaceDay.Api.Controllers;

[ApiController]
[Authorize]
[Route("api")]
public sealed class ImagesController : ApiControllerBase
{
    private const long MaxFileSize = 5 * 1024 * 1024;
    private static readonly string[] AllowedTypes = ["image/jpeg", "image/png", "image/webp"];
    private readonly AppDbContext _db;
    private readonly BlobStorageService _blobs;

    public ImagesController(AppDbContext db, BlobStorageService blobs) { _db = db; _blobs = blobs; }

    [HttpPost("events/{eventId:int}/banner")]
    [Authorize(Roles = Roles.Organiser)]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> UploadEventBanner(int eventId, IFormFile file, CancellationToken cancellationToken)
    {
        var ev = await _db.Events.FirstOrDefaultAsync(x => x.Id == eventId, cancellationToken);
        if (ev == null) return NotFound();
        if (ev.OrganiserId != CurrentUserId) return Forbid();
        var error = Validate(file);
        if (error != null) return BadRequest(new { error });
        try
        {
            ev.BannerBlobName = await _blobs.UploadAsync(file, "event", cancellationToken);
            await _db.SaveChangesAsync(cancellationToken);
            return Ok(new { imageUrl = _blobs.GetReadUrl(ev.BannerBlobName, "event") });
        }
        catch (InvalidOperationException ex) { return StatusCode(503, new { error = ex.Message }); }
    }

    [HttpPost("profile/image")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> UploadProfileImage(IFormFile file, CancellationToken cancellationToken)
    {
        var user = await _db.Users.FindAsync([CurrentUserId], cancellationToken);
        if (user == null) return NotFound();
        var error = Validate(file);
        if (error != null) return BadRequest(new { error });
        try
        {
            user.ProfileImageBlobName = await _blobs.UploadAsync(file, "profile", cancellationToken);
            await _db.SaveChangesAsync(cancellationToken);
            return Ok(new { imageUrl = _blobs.GetReadUrl(user.ProfileImageBlobName, "profile") });
        }
        catch (InvalidOperationException ex) { return StatusCode(503, new { error = ex.Message }); }
    }

    private static string? Validate(IFormFile? file)
    {
        if (file == null || file.Length == 0) return "Choose an image file.";
        if (file.Length > MaxFileSize) return "Image must be 5 MB or smaller.";
        if (!AllowedTypes.Contains(file.ContentType, StringComparer.OrdinalIgnoreCase)) return "Only JPEG, PNG and WebP images are allowed.";
        return null;
    }
}