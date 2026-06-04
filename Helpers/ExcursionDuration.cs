namespace WaldauCastle.Helpers;

public static class ExcursionDuration
{
    public static string Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        var trimmed = value.Trim();
        if (trimmed.Contains("мин", StringComparison.OrdinalIgnoreCase))
            return trimmed;

        var digits = new string(trimmed.Where(char.IsDigit).ToArray());
        if (digits.Length > 0)
            return $"{digits} мин";

        return trimmed;
    }
}
