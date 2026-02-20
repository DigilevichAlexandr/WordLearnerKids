using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
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
    private const int MinFormFillMilliseconds = 2500;

    [BindProperty]
    public InputModel Input { get; set; } = new();

    [BindProperty]
    public string CaptchaToken { get; set; } = string.Empty;

    [BindProperty]
    public long FormRenderedAtUnixMs { get; set; }

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

        if (!string.IsNullOrWhiteSpace(Input.Website))
        {
            ModelState.AddModelError(string.Empty, "Проверка защиты не пройдена.");
            GenerateChallenge();
            return Page();
        }

        var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var elapsedMs = now - FormRenderedAtUnixMs;
        if (elapsedMs < MinFormFillMilliseconds)
        {
            ModelState.AddModelError(string.Empty, "Форма заполнена слишком быстро. Повторите попытку.");
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
        await dbContext.SaveChangesAsync();
        await SignInUserAsync(userAccount);

        return RedirectToPage("/Notes");
    }

    private void GenerateChallenge()
    {
        var challenge = captchaService.CreateChallenge();
        CaptchaToken = challenge.Token;
        CaptchaQuestion = challenge.Question;
        FormRenderedAtUnixMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
    }

    private async Task SignInUserAsync(UserAccount user)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.Email)
        };

        var principal = new ClaimsPrincipal(
            new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme));

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            principal,
            new AuthenticationProperties { IsPersistent = false });
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

        public string Website { get; set; } = string.Empty;
    }
}
