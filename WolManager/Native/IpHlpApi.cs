using System.Runtime.InteropServices;

namespace WolManager.Native;

/// <summary>
/// ARP 요청에 필요한 iphlpapi.dll 함수.
/// </summary>
internal static class IpHlpApi
{
    public const int NoError = 0;

    /// <summary>
    /// destIp로 ARP 요청을 보내 MAC 주소를 얻는다. 주소는 네트워크 바이트 순서의 IPv4 값이다.
    /// srcIp에 로컬 IP를 넣으면 그 NIC로 요청을 보낸다. 응답이 없으면 OS 기본 시간만큼 기다린 뒤 실패한다.
    /// </summary>
    [DllImport("iphlpapi.dll", ExactSpelling = true)]
    public static extern int SendARP(uint destIp, uint srcIp, byte[] macAddress, ref uint physicalAddressLength);
}
