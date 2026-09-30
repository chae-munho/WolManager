using System.Globalization;
using WolManager.Validation;

namespace WolManager.Services;

/// <summary>
/// Wake-on-LAN 매직 패킷 생성.
/// </summary>
public static class MagicPacket
{
    private const int HeaderLength = 6;
    private const int MacLength = 6;
    private const int MacRepeatCount = 16;

    /// <summary>매직 패킷 길이 (FF 6바이트 + MAC 6바이트 × 16회 = 102바이트)</summary>
    public const int Length = HeaderLength + MacLength * MacRepeatCount;

    /// <summary>
    /// MAC 주소로 매직 패킷을 만든다.
    /// </summary>
    /// <returns>MAC 형식이 올바르면 true</returns>
    public static bool TryCreate(string mac, out byte[] packet)
    {
        packet = [];
        if (!InputValidator.TryNormalizeMac(mac, out var normalized))
        {
            return false;
        }

        var macBytes = normalized.Split('-')
            .Select(part => byte.Parse(part, NumberStyles.HexNumber, CultureInfo.InvariantCulture))
            .ToArray();

        packet = new byte[Length];
        for (var i = 0; i < HeaderLength; i++)
        {
            packet[i] = 0xFF;
        }

        for (var i = 0; i < MacRepeatCount; i++)
        {
            macBytes.CopyTo(packet, HeaderLength + i * MacLength);
        }

        return true;
    }
}
