using System.Text.RegularExpressions;

namespace MovieNavigator.Core.Tags;

public readonly record struct TagKey(string Value)
{
    private static readonly Regex ValidPattern = new("^[a-z0-9]+(?:_[a-z0-9]+)*(?:\\.[a-z0-9]+(?:_[a-z0-9]+)*)*$", RegexOptions.Compiled);

    public string? ParentKey
    {
        get
        {
            var lastDot = Value.LastIndexOf('.');
            return lastDot < 0 ? null : Value[..lastDot];
        }
    }

    public static TagKey Parse(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw) || !ValidPattern.IsMatch(raw))
        {
            throw new ArgumentException($"Invalid tag key: {raw}", nameof(raw));
        }

        return new TagKey(raw);
    }

    public override string ToString() => Value;
}
