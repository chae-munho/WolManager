using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;
using WolManager.Models;
using WolManager.Validation;

namespace WolManager.Services;

/// <summary>
/// PC 목록을 JSON 파일로 저장하는 구현.
/// </summary>
public sealed class JsonPcRepository : IPcRepository
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Encoder = JavaScriptEncoder.Create(UnicodeRanges.All),
    };

    private readonly ILogService _log;
    private readonly string _filePath;
    private readonly ObservableCollection<PcEntry> _items = new();

    public JsonPcRepository(ILogService log, string filePath)
    {
        _log = log;
        _filePath = filePath;
        Items = new ReadOnlyObservableCollection<PcEntry>(_items);
    }

    /// <summary>
    /// PC 목록.
    /// </summary>
    public ReadOnlyObservableCollection<PcEntry> Items { get; }

    /// <summary>
    /// 저장 파일에서 목록을 불러온다. 파일이 없거나 깨져 있으면 빈 목록으로 시작한다.
    /// </summary>
    public void Load()
    {
        foreach (var item in _items)
        {
            item.PropertyChanged -= OnItemPropertyChanged;
        }
        _items.Clear();

        if (!File.Exists(_filePath))
        {
            _log.Write("저장된 PC 목록이 없어 빈 목록으로 시작합니다.");
            return;
        }

        List<PcRecord>? records;
        try
        {
            records = JsonSerializer.Deserialize<List<PcRecord>>(File.ReadAllText(_filePath), JsonOptions);
        }
        catch (JsonException)
        {
            _log.Write("PC 목록 파일 내용이 올바르지 않아 빈 목록으로 시작합니다.");
            BackupBrokenFile();
            return;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            _log.Write($"PC 목록 파일을 열 수 없어 빈 목록으로 시작합니다. ({ex.Message})");
            return;
        }

        foreach (var record in records ?? [])
        {
            if (record is null)
            {
                continue;
            }

            var name = record.Name?.Trim() ?? string.Empty;

            // 손으로 고친 파일도 입력 검증과 같은 형식으로 맞춘다. 틀린 값은 그대로 두고 알린다.
            if (!InputValidator.TryNormalizeMac(record.Mac, out var mac))
            {
                mac = record.Mac?.Trim() ?? string.Empty;
                _log.Write($"MAC 주소 형식이 올바르지 않습니다: {name} ({mac})");
            }

            if (!InputValidator.TryNormalizeIp(record.Ip, out var ip))
            {
                ip = record.Ip?.Trim() ?? string.Empty;
                _log.Write($"IP 주소 형식이 올바르지 않습니다: {name} ({ip})");
            }

            Attach(new PcEntry
            {
                Name = name,
                Mac = mac,
                Ip = ip,
                IsTarget = record.IsTarget ?? true,
            });
        }

        _log.Write($"저장된 PC 목록을 불러왔습니다. ({_items.Count}대)");
    }

    /// <summary>
    /// 현재 목록을 저장 파일에 기록한다.
    /// </summary>
    /// <returns>저장에 성공하면 true</returns>
    public bool Save()
    {
        var records = _items.Select(item => new PcRecord
        {
            Name = item.Name,
            Mac = item.Mac,
            Ip = item.Ip,
            IsTarget = item.IsTarget,
        }).ToList();

        try
        {
            File.WriteAllText(_filePath, JsonSerializer.Serialize(records, JsonOptions));
            return true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            _log.Write($"PC 목록을 저장하지 못했습니다. ({ex.Message})");
            return false;
        }
    }

    /// <summary>
    /// PC를 목록에 추가하고 저장한다.
    /// </summary>
    public void Add(PcEntry entry)
    {
        Attach(entry);
        _log.Write($"추가: {entry.Name} ({entry.Mac})");
        Save();
    }

    /// <summary>
    /// PC의 이름, MAC, IP를 바꾸고 저장한다.
    /// </summary>
    public void Update(PcEntry entry, string name, string mac, string ip)
    {
        entry.Name = name;
        entry.Mac = mac;
        entry.Ip = ip;
        _log.Write($"수정: {entry.Name} ({entry.Mac})");
        Save();
    }

    /// <summary>
    /// PC를 목록에서 삭제하고 저장한다.
    /// </summary>
    public void Remove(PcEntry entry)
    {
        if (_items.Remove(entry))
        {
            entry.PropertyChanged -= OnItemPropertyChanged;
            _log.Write($"삭제: {entry.Name} ({entry.Mac})");
            Save();
        }
    }

    private void Attach(PcEntry entry)
    {
        entry.PropertyChanged += OnItemPropertyChanged;
        _items.Add(entry);
    }

    // 목록의 대상 체크를 바꾸면 바로 저장한다.
    private void OnItemPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (sender is not PcEntry entry || e.PropertyName != nameof(PcEntry.IsTarget))
        {
            return;
        }

        _log.Write(entry.IsTarget
            ? $"전체 깨우기 대상에 포함: {entry.Name}"
            : $"전체 깨우기 대상에서 제외: {entry.Name}");
        Save();
    }

    // 깨진 파일을 다음 저장이 덮어쓰지 않도록 옮겨 둔다.
    private void BackupBrokenFile()
    {
        try
        {
            var directory = Path.GetDirectoryName(_filePath) ?? string.Empty;
            var backupPath = Path.Combine(directory, Constants.BrokenPcFileName);
            File.Move(_filePath, backupPath, overwrite: true);
            _log.Write($"기존 파일은 {Constants.BrokenPcFileName} 파일로 옮겨 두었습니다.");
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            _log.Write($"읽을 수 없는 파일을 옮기지 못했습니다. ({ex.Message})");
        }
    }
}
