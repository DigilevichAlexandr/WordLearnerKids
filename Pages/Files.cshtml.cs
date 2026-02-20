using System.Globalization;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using WordLearnerKids.Configuration;
using WordLearnerKids.Data;
using WordLearnerKids.Models;

namespace WordLearnerKids.Pages;

[Authorize]
public sealed class FilesModel(AppDbContext dbContext, IOptions<AppStorageOptions> storageOptions) : PageModel
{
    private const long MaxFileSizeBytes = 25 * 1024 * 1024;

    public IReadOnlyList<StoredFile> Files { get; private set; } = [];

    [TempData]
    public string? StatusMessage { get; set; }

    public async Task OnGetAsync()
    {
        await LoadFilesAsync();
    }

    public async Task<IActionResult> OnPostUploadAsync(IFormFile? upload)
    {
        if (upload is null || upload.Length == 0)
        {
            ModelState.AddModelError(string.Empty, "Выберите файл для загрузки.");
            await LoadFilesAsync();
            return Page();
        }

        if (upload.Length > MaxFileSizeBytes)
        {
            ModelState.AddModelError(string.Empty, "Максимальный размер файла — 25 МБ.");
            await LoadFilesAsync();
            return Page();
        }

        var userId = GetUserId();
        var originalName = Path.GetFileName(upload.FileName);
        var extension = Path.GetExtension(originalName);
        var storedName = $"{Guid.NewGuid():N}{extension}";

        var userStorageDirectory = Path.Combine(StorageRoot, userId.ToString(CultureInfo.InvariantCulture));
        Directory.CreateDirectory(userStorageDirectory);

        var fullPath = Path.Combine(userStorageDirectory, storedName);
        await using (var stream = System.IO.File.Create(fullPath))
        {
            await upload.CopyToAsync(stream);
        }

        var file = new StoredFile
        {
            UserId = userId,
            OriginalName = originalName,
            StoredName = storedName,
            ContentType = string.IsNullOrWhiteSpace(upload.ContentType)
                ? "application/octet-stream"
                : upload.ContentType,
            SizeBytes = upload.Length,
            UploadedAtUtc = DateTime.UtcNow
        };

        dbContext.Files.Add(file);
        await dbContext.SaveChangesAsync();

        StatusMessage = "Файл загружен.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnGetDownloadAsync(int id)
    {
        var userId = GetUserId();
        var file = await dbContext.Files.SingleOrDefaultAsync(item => item.Id == id && item.UserId == userId);
        if (file is null)
        {
            return NotFound();
        }

        var path = Path.Combine(StorageRoot, userId.ToString(CultureInfo.InvariantCulture), file.StoredName);
        if (!System.IO.File.Exists(path))
        {
            return NotFound();
        }

        return new PhysicalFileResult(path, file.ContentType)
        {
            FileDownloadName = file.OriginalName,
            EnableRangeProcessing = true
        };
    }

    public async Task<IActionResult> OnPostDeleteAsync(int id)
    {
        var userId = GetUserId();
        var file = await dbContext.Files.SingleOrDefaultAsync(item => item.Id == id && item.UserId == userId);
        if (file is null)
        {
            return NotFound();
        }

        var path = Path.Combine(StorageRoot, userId.ToString(CultureInfo.InvariantCulture), file.StoredName);
        if (System.IO.File.Exists(path))
        {
            System.IO.File.Delete(path);
        }

        dbContext.Files.Remove(file);
        await dbContext.SaveChangesAsync();

        StatusMessage = "Файл удалён.";
        return RedirectToPage();
    }

    private string StorageRoot => storageOptions.Value.FilesPath;

    private async Task LoadFilesAsync()
    {
        var userId = GetUserId();
        Files = await dbContext.Files
            .Where(file => file.UserId == userId)
            .OrderByDescending(file => file.UploadedAtUtc)
            .ToListAsync();
    }

    private int GetUserId()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.Parse(userId!);
    }
}
