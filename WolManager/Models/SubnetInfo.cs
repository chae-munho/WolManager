using System.Buffers.Binary;
using System.Net;

namespace WolManager.Models;

/// <summary>
/// 마스터 PC의 NIC 하나에 설정된 IPv4 대역.
/// </summary>
/// <param name="InterfaceName">NIC 이름 (예: 이더넷, Wi-Fi)</param>
/// <param name="LocalAddress">이 NIC의 IP</param>
/// <param name="PrefixLength">서브넷 마스크 길이 (예: 24)</param>
/// <param name="Gateway">기본 게이트웨이. 없으면 null</param>
public sealed record SubnetInfo(string InterfaceName, IPAddress LocalAddress, int PrefixLength, IPAddress? Gateway)
{
    private uint Mask => PrefixLength == 0 ? 0u : uint.MaxValue << (32 - PrefixLength);

    /// <summary>서브넷 브로드캐스트 주소 (IP | ~Mask)</summary>
    public IPAddress Broadcast => FromUInt32(ToUInt32(LocalAddress) | ~Mask);

    /// <summary>대역 표시용 문자열 (예: 192.168.0.0/24)</summary>
    public string Network => $"{FromUInt32(ToUInt32(LocalAddress) & Mask)}/{PrefixLength}";

    /// <summary>
    /// 주어진 IP가 이 대역에 속하는지 확인한다.
    /// </summary>
    public bool Contains(IPAddress address)
    {
        return (ToUInt32(address) & Mask) == (ToUInt32(LocalAddress) & Mask);
    }

    /// <summary>
    /// IPv4 주소를 부호 없는 정수로 바꾼다.
    /// </summary>
    public static uint ToUInt32(IPAddress address) => BinaryPrimitives.ReadUInt32BigEndian(address.GetAddressBytes());

    /// <summary>
    /// 부호 없는 정수를 IPv4 주소로 바꾼다.
    /// </summary>
    public static IPAddress FromUInt32(uint value)
    {
        var bytes = new byte[4];
        BinaryPrimitives.WriteUInt32BigEndian(bytes, value);
        return new IPAddress(bytes);
    }
}
