using System.Globalization;

namespace WolManager.Validation;

/// <summary>
/// MAC, IP 입력값 검증과 정규화.
/// </summary>
public static class InputValidator
{
    private const int MacByteCount = 6;
    private const int Ipv4PartCount = 4;
    private const int Ipv4PartMax = 255;

    /// <summary>
    /// MAC 주소를 검사하고 AA-BB-CC-DD-EE-FF 형식으로 바꾼다.
    /// 구분자 없는 12자리, 또는 '-'나 ':' 한 가지로 구분한 6묶음만 허용한다.
    /// </summary>
    /// <returns>올바른 형식이면 true</returns>
    public static bool TryNormalizeMac(string? input, out string normalized)
    {
        normalized = string.Empty;
        var text = input?.Trim() ?? string.Empty;

        string hex;
        if (text.Length == MacByteCount * 2)
        {
            hex = text;
        }
        else if (text.Length == MacByteCount * 3 - 1)
        {
            var separator = text[2];
            if (separator is not ('-' or ':'))
            {
                return false;
            }

            var groups = text.Split(separator);
            if (groups.Length != MacByteCount || groups.Any(group => group.Length != 2))
            {
                return false;
            }

            hex = string.Concat(groups);
        }
        else
        {
            return false;
        }

        if (!hex.All(Uri.IsHexDigit))
        {
            return false;
        }

        hex = hex.ToUpperInvariant();
        normalized = string.Join('-', Enumerable.Range(0, MacByteCount).Select(i => hex.Substring(i * 2, 2)));
        return true;
    }

    /// <summary>
    /// IP 주소를 검사하고 정리한다. 빈 값은 허용하며 빈 문자열로 돌려준다.
    /// 점 4개로 나뉜 0~255 숫자 형식만 허용한다.
    /// </summary>
    /// <returns>비어 있거나 올바른 IPv4 형식이면 true</returns>
    public static bool TryNormalizeIp(string? input, out string normalized)
    {
        normalized = string.Empty;
        var text = input?.Trim() ?? string.Empty;
        if (text.Length == 0)
        {
            return true;
        }

        var parts = text.Split('.');
        if (parts.Length != Ipv4PartCount)
        {
            return false;
        }

        var values = new int[Ipv4PartCount];
        for (var i = 0; i < Ipv4PartCount; i++)
        {
            var part = parts[i];
            if (part.Length is 0 or > 3
                || !part.All(char.IsAsciiDigit)
                || !int.TryParse(part, NumberStyles.None, CultureInfo.InvariantCulture, out values[i])
                || values[i] > Ipv4PartMax)
            {
                return false;
            }
        }

        normalized = string.Join('.', values);
        return true;
    }
}
