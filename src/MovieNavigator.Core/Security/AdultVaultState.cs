namespace MovieNavigator.Core.Security;

public sealed record AdultVaultState(bool IsUnlocked, DateTimeOffset? UnlockExpiresAt)
{
    public bool CanQueryAdultLibrary => IsUnlocked;
    public bool CanShowAdultTags => IsUnlocked;

    public static AdultVaultState Locked() => new(false, null);

    public static AdultVaultState Unlocked(DateTimeOffset expiresAt) => new(true, expiresAt);

    public AdultVaultState LockIfExpired(DateTimeOffset now)
    {
        return UnlockExpiresAt is not null && now >= UnlockExpiresAt.Value ? Locked() : this;
    }
}
