using System.Text.Json;
using MobiFlux.Application.Abstractions;
using MobiFlux.Shared.Contracts;

namespace MobiFlux.Infrastructure.Telemetry;

public sealed class RotationHistory : IRotationHistory
{
    private readonly Queue<RotationResultDto> _entries;
    private readonly object _sync = new();
    private readonly string _path;
    public RotationHistory(string path)
    {
        _path = path;
        _entries = File.Exists(path) ? new(JsonSerializer.Deserialize<RotationResultDto[]>(File.ReadAllText(path)) ?? []) : new();
    }
    public IReadOnlyList<RotationResultDto> List() { lock (_sync) return _entries.Reverse().ToArray(); }
    public void Add(RotationResultDto result)
    {
        lock (_sync)
        {
            var next = _entries.Append(result).TakeLast(500).ToArray();
            var temporary = _path + ".tmp";
            File.WriteAllText(temporary, JsonSerializer.Serialize(next));
            File.Move(temporary, _path, overwrite: true);
            _entries.Clear();
            foreach (var item in next) _entries.Enqueue(item);
        }
    }
}
