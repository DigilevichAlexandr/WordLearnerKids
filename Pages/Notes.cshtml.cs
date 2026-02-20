using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using WordLearnerKids.Data;
using WordLearnerKids.Models;

namespace WordLearnerKids.Pages;

[Authorize]
public sealed class NotesModel(AppDbContext dbContext) : PageModel
{
    public IReadOnlyList<Note> Notes { get; private set; } = [];

    [TempData]
    public string? StatusMessage { get; set; }

    public async Task OnGetAsync()
    {
        await LoadNotesAsync();
    }

    public async Task<IActionResult> OnPostAddAsync(string title, string content)
    {
        var trimmedTitle = title.Trim();
        var trimmedContent = content.Trim();

        if (string.IsNullOrWhiteSpace(trimmedTitle))
        {
            ModelState.AddModelError(string.Empty, "Введите заголовок заметки.");
            await LoadNotesAsync();
            return Page();
        }

        if (trimmedTitle.Length > 200)
        {
            ModelState.AddModelError(string.Empty, "Заголовок слишком длинный (до 200 символов).");
            await LoadNotesAsync();
            return Page();
        }

        var userId = GetUserId();
        var now = DateTime.UtcNow;
        var note = new Note
        {
            UserId = userId,
            Title = trimmedTitle,
            Content = trimmedContent,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };

        dbContext.Notes.Add(note);
        await dbContext.SaveChangesAsync();

        StatusMessage = "Заметка сохранена.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostUpdateAsync(int id, string title, string content)
    {
        var trimmedTitle = title.Trim();
        var trimmedContent = content.Trim();

        if (string.IsNullOrWhiteSpace(trimmedTitle))
        {
            ModelState.AddModelError(string.Empty, "Введите заголовок заметки.");
            await LoadNotesAsync();
            return Page();
        }

        if (trimmedTitle.Length > 200)
        {
            ModelState.AddModelError(string.Empty, "Заголовок слишком длинный (до 200 символов).");
            await LoadNotesAsync();
            return Page();
        }

        var userId = GetUserId();
        var note = await dbContext.Notes.SingleOrDefaultAsync(item => item.Id == id && item.UserId == userId);
        if (note is null)
        {
            return NotFound();
        }

        note.Title = trimmedTitle;
        note.Content = trimmedContent;
        note.UpdatedAtUtc = DateTime.UtcNow;
        await dbContext.SaveChangesAsync();

        StatusMessage = "Заметка обновлена.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDeleteAsync(int id)
    {
        var userId = GetUserId();
        var note = await dbContext.Notes.SingleOrDefaultAsync(item => item.Id == id && item.UserId == userId);
        if (note is null)
        {
            return NotFound();
        }

        dbContext.Notes.Remove(note);
        await dbContext.SaveChangesAsync();

        StatusMessage = "Заметка удалена.";
        return RedirectToPage();
    }

    private async Task LoadNotesAsync()
    {
        var userId = GetUserId();
        Notes = await dbContext.Notes
            .Where(note => note.UserId == userId)
            .OrderByDescending(note => note.UpdatedAtUtc)
            .ToListAsync();
    }

    private int GetUserId()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.Parse(userId!);
    }
}
