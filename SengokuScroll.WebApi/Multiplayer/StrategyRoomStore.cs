using System.Text.Json;
using System.Text.RegularExpressions;
using SengokuScroll.Strategy.Persistence;

namespace SengokuScroll.WebApi.Multiplayer;

public sealed record StrategyRoomSnapshot(int FormatVersion, string RoomId, string RoomName, string ScenarioId,
    int MaxPlayers, long WorldVersion, long TurnNumber, bool HasStarted,
    IReadOnlyList<StrategyMultiplayerForceDefinition> Forces, IReadOnlyList<StrategyMultiplayerPlayer> Players,
    IReadOnlyList<string> ProcessedCommands, StrategySaveDocument World);

/// <summary>Single-process durable room store. Never place beneath wwwroot.</summary>
public sealed class StrategyRoomStore : IDisposable
{
    private readonly string directory;
    private readonly bool enabled;
    private readonly FileStream? processLock;
    private static readonly JsonSerializerOptions Json = new() { PropertyNameCaseInsensitive = true };
    public StrategyRoomStore(StrategyMultiplayerOptions options, string contentRoot)
    {
        enabled = options.PersistenceEnabled;
        directory = Path.GetFullPath(options.StoragePath, contentRoot);
        var webRoot = Path.GetFullPath(Path.Combine(contentRoot, "wwwroot"));
        if (directory.Equals(webRoot, StringComparison.OrdinalIgnoreCase)
            || directory.StartsWith(webRoot + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Room storage must not be public");
        if (enabled)
        {
            Directory.CreateDirectory(directory);
            processLock = new FileStream(Path.Combine(directory, ".room-store.lock"),
                FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
        }
    }

    private string FilePath(string id)
    {
        if (!Regex.IsMatch(id, "\\A[A-Fa-f0-9]{10}\\z")) throw new InvalidOperationException("Invalid room ID");
        return Path.Combine(directory, id.ToUpperInvariant() + ".json");
    }

    public IEnumerable<(string Path, StrategyRoomSnapshot? Snapshot)> ReadAll()
    {
        if (!enabled) yield break;
        foreach (var path in Directory.EnumerateFiles(directory, "*.json").Order(StringComparer.Ordinal))
        {
            StrategyRoomSnapshot? snapshot = null;
            try
            {
                using var stream = File.OpenRead(path);
                snapshot = JsonSerializer.Deserialize<StrategyRoomSnapshot>(stream, Json);
                if (snapshot is null || !string.Equals(Path.GetFullPath(path), FilePath(snapshot.RoomId), StringComparison.OrdinalIgnoreCase))
                    snapshot = null;
            }
            catch (Exception ex) when (ex is JsonException or IOException or UnauthorizedAccessException or InvalidOperationException) { }
            yield return (path, snapshot);
        }
    }

    public bool Exists(string id) => enabled && File.Exists(FilePath(id));

    public StrategyRoomSnapshot Read(string id)
    {
        using var stream = File.OpenRead(FilePath(id));
        return JsonSerializer.Deserialize<StrategyRoomSnapshot>(stream, Json)
            ?? throw new InvalidOperationException("Invalid room snapshot");
    }

    public void Write(StrategyRoomSnapshot snapshot)
    {
        if (!enabled) return;
        var target = FilePath(snapshot.RoomId);
        var temporary = target + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            using (var stream = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                JsonSerializer.Serialize(stream, snapshot, Json);
                stream.Flush(flushToDisk: true);
            }
            File.Move(temporary, target, overwrite: true);
        }
        finally { if (File.Exists(temporary)) File.Delete(temporary); }
    }

    public void Delete(string id) { if (enabled) File.Delete(FilePath(id)); }
    public void Dispose() => processLock?.Dispose();
}
