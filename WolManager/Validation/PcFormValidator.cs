using WolManager.Models;

namespace WolManager.Validation;

/// PC 추가/수정 입력값 검사. 형식 검사와 다른 PC와의 MAC/IP 중복 검사를 함께 한다.
public static class PcFormValidator
{
    /// 입력값을 검사하고 정리한 값을 돌려준다.
    /// except: 수정 중인 PC (중복 검사에서 제외). 새로 추가할 때는 null
    /// 반환: 문제가 없으면 null, 있으면 사용자에게 보여 줄 안내 문구
    public static string? Validate(
        IEnumerable<PcEntry> items,
        PcEntry? except,
        string nameInput,
        string macInput,
        string ipInput,
        out string name,
        out string mac,
        out string ip)
    {
        name = nameInput.Trim();
        mac = string.Empty;
        ip = string.Empty;

        if (name.Length == 0)
        {
            return "이름을 입력하세요.";
        }

        if (!InputValidator.TryNormalizeMac(macInput, out mac))
        {
            return "MAC 주소 형식이 올바르지 않습니다.\n예: AA-BB-CC-DD-EE-FF";
        }

        if (!InputValidator.TryNormalizeIp(ipInput, out ip))
        {
            return "IP 주소 형식이 올바르지 않습니다.\n예: 192.168.0.11 (모르면 비워 두세요)";
        }

        var others = items.Where(item => item != except).ToList();

        var normalizedMac = mac;
        if (others.FirstOrDefault(item => item.Mac == normalizedMac) is { } sameMac)
        {
            return $"이미 등록된 MAC 주소입니다. ({sameMac.Name})";
        }

        var normalizedIp = ip;
        if (normalizedIp.Length > 0 && others.FirstOrDefault(item => item.Ip == normalizedIp) is { } sameIp)
        {
            return $"이미 등록된 IP 주소입니다. ({sameIp.Name})";
        }

        return null;
    }
}
