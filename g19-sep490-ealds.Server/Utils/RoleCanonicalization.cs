using System.Globalization;
using System.Text;

namespace g19_sep490_ealds.Server.Utils;

/// <summary>Maps DB role id/code to JWT/profile role strings. Accountant id from <c>App:AccountantRoleId</c>.</summary>
public static class RoleCanonicalization
{
    private static string RemoveDiacritics(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        var normalized = value.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(normalized.Length);
        foreach (var c in normalized)
        {
            var category = CharUnicodeInfo.GetUnicodeCategory(c);
            if (category != UnicodeCategory.NonSpacingMark)
                sb.Append(c);
        }
        return sb.ToString().Normalize(NormalizationForm.FormC);
    }

    public static string CanonicalizeRoleCode(int roleId, string? code, int accountantRoleId)
    {
        if (roleId == accountantRoleId)
            return "ACCOUNTANT";

        var raw = RemoveDiacritics(code ?? string.Empty).Trim();
        if (raw.Length == 0)
            return string.Empty;

        var upper = raw.ToUpperInvariant();
        upper = upper.Replace(' ', '_').Replace('-', '_');
        while (upper.Contains("__", StringComparison.Ordinal))
            upper = upper.Replace("__", "_", StringComparison.Ordinal);

        var compact = upper.Replace("_", "", StringComparison.Ordinal);

        if (upper.Contains("ACCOUNTANT", StringComparison.Ordinal) || compact.Contains("KETOAN", StringComparison.Ordinal))
            return "ACCOUNTANT";
        if (upper.Contains("DIRECTOR", StringComparison.Ordinal) || upper.Contains("GIAM_DOC", StringComparison.Ordinal))
            return "DIRECTOR";
        if (upper.Contains("DEPARTMENT_HEAD", StringComparison.Ordinal) || upper.Contains("TRUONG_PHONG", StringComparison.Ordinal))
            return "DEPARTMENT_HEAD";
        if (upper.Contains("ADMIN", StringComparison.Ordinal))
            return "ADMIN";

        return upper;
    }
}
