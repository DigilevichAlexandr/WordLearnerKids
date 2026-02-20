using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using WordLearnerKids.Data;
using WordLearnerKids.Models;
using WordLearnerKids.Services;

namespace WordLearnerKids.Pages;

public sealed class RegisterModel(
    AppDbContext dbContext,
    PasswordService passwordService,
    CaptchaService captchaService) : PageModel
{
    [BindProperty]
    public InputModel Input { get; set; } = new();

    [BindProperty]
    public string CaptchaToken { get; set; } = string.Empty;

    public string CaptchaQuestion { get; private set; } = string.Empty;

    public IActionResult OnGet()
    {
        if (User.Identity?.IsAuthenticated ?? false)
        {
            return RedirectToPage("/Notes");
        }

        GenerateChallenge();
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (User.Identity?.IsAuthenticated ?? false)
        {
            return RedirectToPage("/Notes");
        }

        if (!ModelState.IsValid)
        {
            GenerateChallenge();
            return Page();
        }

        if (!captchaService.Validate(CaptchaToken, Input.CaptchaAnswer))
        {
            ModelState.AddModelError(nameof(Input.CaptchaAnswer), "Неверный ответ капчи.");
            GenerateChallenge();
            return Page();
        }

        var email = Input.Email.Trim().ToLowerInvariant();
        var emailAlreadyExists = await dbContext.Users.AnyAsync(user => user.Email == email);
        if (emailAlreadyExists)
        {
            ModelState.AddModelError(nameof(Input.Email), "Пользователь с таким email уже существует.");
            GenerateChallenge();
            return Page();
        }

        var userAccount = new UserAccount
        {
            Email = email,
            PasswordHash = passwordService.HashPassword(Input.Password)
        };

        dbContext.Users.Add(userAccount);
        try
        {
            await dbContext.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            ModelState.AddModelError(string.Empty, "Не удалось создать аккаунт. Попробуйте снова.");
            GenerateChallenge();
            return Page();
        }

        TempData["StatusMessage"] = "Аккаунт создан. Теперь войдите.";
        return RedirectToPage("/Login");
    }

    private void GenerateChallenge()
    {
        var challenge = captchaService.CreateChallenge();
        CaptchaToken = challenge.Token;
        CaptchaQuestion = challenge.Question;
    }

    public sealed class InputModel
    {
        [Required(ErrorMessage = "Укажите email.")]
        [EmailAddress(ErrorMessage = "Некорректный email.")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Укажите пароль.")]
        [MinLength(8, ErrorMessage = "Минимальная длина пароля — 8 символов.")]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "Повторите пароль.")]
        [Compare(nameof(Password), ErrorMessage = "Пароли не совпадают.")]
        public string ConfirmPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "Ответьте на капчу.")]
        public string CaptchaAnswer { get; set; } = string.Empty;
    }
}
