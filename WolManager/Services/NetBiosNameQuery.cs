using System.Net;
using System.Net.Sockets;
using System.Text;

namespace WolManager.Services;

/// NetBIOS 노드 상태 질의(UDP 137)로 PC의 컴퓨터 이름을 묻는다.
/// Windows PC는 DNS에 등록되어 있지 않아도 이 질의에 자기 컴퓨터 이름으로 대답한다.
public static class NetBiosNameQuery
{
    private const int NetBiosPort = 137;
    private const int HeaderLength = 12;
    private const int NameLength = 15;
    private const int EntryLength = 18;
    private const byte WorkstationSuffix = 0x00;
    private const ushort GroupNameFlag = 0x8000;

    // "*" 이름으로 노드 상태(NBSTAT)를 묻는 고정 요청. 앞 2바이트(요청 번호)만 매번 바꾼다.
    private static readonly byte[] RequestTemplate = BuildRequest();

    /// 컴퓨터 이름을 묻는다.
    /// 반환: 이름(앞뒤 공백 제거). 대답이 없거나 시간 안에 오지 않으면 null
    public static async Task<string?> QueryAsync(IPAddress address, TimeSpan timeout)
    {
        var request = (byte[])RequestTemplate.Clone();
        var transactionId = (ushort)Random.Shared.Next(ushort.MaxValue);
        request[0] = (byte)(transactionId >> 8);
        request[1] = (byte)transactionId;

        using var cancellation = new CancellationTokenSource(timeout);
        try
        {
            using var client = new UdpClient(AddressFamily.InterNetwork);
            await client.SendAsync(request, new IPEndPoint(address, NetBiosPort), cancellation.Token);

            while (true)
            {
                var response = await client.ReceiveAsync(cancellation.Token);
                if (response.RemoteEndPoint.Address.Equals(address)
                    && response.Buffer.Length > 1
                    && response.Buffer[0] == request[0]
                    && response.Buffer[1] == request[1])
                {
                    return ParseWorkstationName(response.Buffer);
                }
            }
        }
        catch (Exception ex) when (ex is OperationCanceledException or SocketException)
        {
            return null;
        }
    }

    private static byte[] BuildRequest()
    {
        var packet = new List<byte>
        {
            0x00, 0x00,             // 요청 번호 (보낼 때 채운다)
            0x00, 0x00,             // 플래그
            0x00, 0x01,             // 질문 수
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x20,                   // 인코딩된 이름 길이 (32)
        };

        // "*" 뒤를 0으로 채운 16바이트 이름을 NetBIOS 방식(바이트마다 'A'+상위 4비트, 'A'+하위 4비트)으로 인코딩한다.
        var name = new byte[16];
        name[0] = (byte)'*';
        foreach (var b in name)
        {
            packet.Add((byte)('A' + (b >> 4)));
            packet.Add((byte)('A' + (b & 0x0F)));
        }

        packet.Add(0x00);                   // 이름 끝
        packet.AddRange([0x00, 0x21]);      // 질의 종류: NBSTAT
        packet.AddRange([0x00, 0x01]);      // 클래스: IN
        return packet.ToArray();
    }

    // 응답의 이름 목록에서 컴퓨터 이름(접미사 0x00, 그룹 아님)을 찾는다.
    private static string? ParseWorkstationName(byte[] buffer)
    {
        var offset = HeaderLength;
        if (offset >= buffer.Length)
        {
            return null;
        }

        // 응답 이름: 압축 포인터(2바이트)이거나 길이-값 묶음이 0으로 끝난다.
        if ((buffer[offset] & 0xC0) == 0xC0)
        {
            offset += 2;
        }
        else
        {
            while (offset < buffer.Length && buffer[offset] != 0)
            {
                offset += buffer[offset] + 1;
            }

            offset++;
        }

        offset += 2 + 2 + 4 + 2; // 종류, 클래스, TTL, 데이터 길이
        if (offset >= buffer.Length)
        {
            return null;
        }

        var count = buffer[offset++];
        for (var i = 0; i < count && offset + EntryLength <= buffer.Length; i++, offset += EntryLength)
        {
            var suffix = buffer[offset + NameLength];
            var flags = (ushort)((buffer[offset + NameLength + 1] << 8) | buffer[offset + NameLength + 2]);
            if (suffix == WorkstationSuffix && (flags & GroupNameFlag) == 0)
            {
                var name = Encoding.ASCII.GetString(buffer, offset, NameLength).Trim();
                return name.Length > 0 ? name : null;
            }
        }

        return null;
    }
}
