using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using WordLearnerKids.Configuration;
using WordLearnerKids.Data;
using WordLearnerKids.Services;

var builder = WebApplication.CreateBuilder(args);

var appDataRoot = ResolveAppDataRoot(
    builder.Configuration["AppDataRoot"],
    builder.Environment.ContentRootPath);

var filesPath = Path.Combine(appDataRoot, "files");
var keysPath = Path.Combine(appDataRoot, "keys");

Directory.CreateDirectory(appDataRoot);
Directory.CreateDirectory(filesPath);
Directory.CreateDirectory(keysPath);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? $"Data Source={Path.Combine(appDataRoot, "app.db")}";

builder.Services.AddRazorPages();
builder.Services.AddDbContext<AppDbContext>(options => options.UseSqlite(connectionString));
builder.Services.Configure<AppStorageOptions>(options =>
{
    options.RootPath = appDataRoot;
    options.FilesPath = filesPath;
});
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(keysPath));
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Login";
        options.AccessDeniedPath = "/Login";
        options.SlidingExpiration = true;
    });
builder.Services.AddAuthorization();
builder.Services.AddScoped<PasswordService>();
builder.Services.AddSingleton<CaptchaService>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    dbContext.Database.EnsureCreated();
}

app.UseForwardedHeaders();
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));
app.MapRazorPages();

app.Run();

static string ResolveAppDataRoot(string? configuredPath, string contentRootPath)
{
    if (string.IsNullOrWhiteSpace(configuredPath))
    {
        return Path.Combine(contentRootPath, "data");
    }

    return Path.IsPathRooted(configuredPath)
        ? configuredPath
        : Path.Combine(contentRootPath, configuredPath);
}
