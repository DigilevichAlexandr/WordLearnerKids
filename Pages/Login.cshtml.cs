using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using WordLearnerKids.Data;
using WordLearnerKids.Services;

namespace WordLearnerKids.Pages;

public sealed class LoginModel(AppDbContext dbContext, PasswordService passwordService) : PageModel
{
    [TempData]
    public string? StatusMessage { get; set; }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    [BindProperty(SupportsGet = true)]
    public string? ReturnUrl { get; set; }

    public IActionResult OnGet()
    {
        if (User.Identity?.IsAuthenticated ?? false)
        {
            return RedirectToPage("/Notes");
        }

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
            return Page();
        }

        var normalizedEmail = Input.Email.Trim().ToLowerInvariant();
        var user = await dbContext.Users.SingleOrDefaultAsync(u => u.Email == normalizedEmail);
        if (user is null || !passwordService.VerifyPassword(Input.Password, user.PasswordHash))
        {
            ModelState.AddModelError(string.Empty, "Неверный email или пароль.");
            return Page();
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.Email)
        };

        var principal = new ClaimsPrincipal(
            new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme));

        var authProperties = new AuthenticationProperties
        {
            IsPersistent = Input.RememberMe,
            ExpiresUtc = Input.RememberMe ? DateTimeOffset.UtcNow.AddDays(14) : null
        };

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            principal,
            authProperties);

        if (!string.IsNullOrWhiteSpace(ReturnUrl) && Url.IsLocalUrl(ReturnUrl))
        {
            return LocalRedirect(ReturnUrl);
        }

        return RedirectToPage("/Notes");
    }

    public sealed class InputModel
    {
        [Required(ErrorMessage = "Укажите email.")]
        [EmailAddress(ErrorMessage = "Некорректный email.")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Укажите пароль.")]
        public string Password { get; set; } = string.Empty;

        public bool RememberMe { get; set; }
    }
}
