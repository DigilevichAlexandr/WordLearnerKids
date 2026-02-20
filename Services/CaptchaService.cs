using System.Collections.Concurrent;

namespace WordLearnerKids.Services;

public sealed class CaptchaService
{
    private static readonly TimeSpan ChallengeLifetime = TimeSpan.FromMinutes(10);
    private readonly ConcurrentDictionary<string, CaptchaEntry> entries = new();

    public CaptchaChallenge CreateChallenge()
    {
        CleanupExpired();

        var left = Random.Shared.Next(2, 10);
        var right = Random.Shared.Next(2, 10);
        var token = Guid.NewGuid().ToString("N");

        entries[token] = new CaptchaEntry((left + right).ToString(), DateTime.UtcNow + ChallengeLifetime);
        return new CaptchaChallenge(token, $"{left} + {right}");
    }

    public bool Validate(string token, string answer)
    {
        if (string.IsNullOrWhiteSpace(token) || string.IsNullOrWhiteSpace(answer))
        {
            return false;
        }

        if (!entries.TryRemove(token, out var entry))
        {
            return false;
        }

        if (entry.ExpiresAtUtc < DateTime.UtcNow)
        {
            return false;
        }

        return string.Equals(entry.Answer, answer.Trim(), StringComparison.Ordinal);
    }

    private void CleanupExpired()
    {
        var now = DateTime.UtcNow;
        foreach (var (token, entry) in entries)
        {
            if (entry.ExpiresAtUtc < now)
            {
                entries.TryRemove(token, out _);
            }
        }
    }

    private sealed record CaptchaEntry(string Answer, DateTime ExpiresAtUtc);
}

public sealed record CaptchaChallenge(string Token, string Question);
