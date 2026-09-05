using System.Text.Json;
using System.Text.Json.Serialization;

namespace SengokuScroll.Strategy.Persistence;

/// <summary>10 槽策略存档文件持久化（App_Data/strategy-saves）。</summary>
public sealed class StrategySaveSlotRepository
{
    public const int MaxSlots = 10;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = true
    };

    private readonly string saveDirectory;
    private readonly object[] slotLocks = Enumerable.Range(0, MaxSlots).Select(_ => new object()).ToArray();

    public StrategySaveSlotRepository(string saveDirectory)
    {
        this.saveDirectory = saveDirectory;
        Directory.CreateDirectory(saveDirectory);
    }

    public IReadOnlyList<StrategySaveSlotSummary> ListSlots()
    {
        var list = new List<StrategySaveSlotSummary>(MaxSlots);
        for (var slot = 1; slot <= MaxSlots; slot++)
            list.Add(ReadSummary(slot));
        return list;
    }

    public StrategySaveSlotSummary ReadSummary(int slot)
    {
        ValidateSlot(slot);
        var envelope = ReadEnvelope(slot);
        if (envelope is null)
        {
            return new StrategySaveSlotSummary
            {
                Slot = slot,
                Occupied = false
            };
        }

        return ToSummary(slot, envelope);
    }

    public StrategySaveSlotEnvelope? ReadEnvelope(int slot)
    {
        ValidateSlot(slot);
        lock (slotLocks[slot - 1])
            return ReadEnvelopeCore(slot);
    }

    private StrategySaveSlotEnvelope? ReadEnvelopeCore(int slot)
    {
        ValidateSlot(slot);
        var path = SlotPath(slot);
        if (!File.Exists(path))
            return null;

        try
        {
            using var stream = new FileStream(path, FileMode.Open, FileAccess.Read,
                FileShare.ReadWrite | FileShare.Delete);
            var envelope = JsonSerializer.Deserialize<StrategySaveSlotEnvelope>(stream, JsonOptions);
            return envelope?.Save?.Date is null ? null : envelope;
        }
        catch (JsonException)
        {
            return null;
        }
        catch (FileNotFoundException)
        {
            return null;
        }
    }

    public StrategySaveSlotSummary WriteSlot(int slot, StrategySaveSlotEnvelope envelope)
    {
        ValidateSlot(slot);
        lock (slotLocks[slot - 1])
            return WriteSlotCore(slot, envelope);
    }

    private StrategySaveSlotSummary WriteSlotCore(int slot, StrategySaveSlotEnvelope envelope)
    {
        ValidateSlot(slot);
        ArgumentNullException.ThrowIfNull(envelope);
        ArgumentNullException.ThrowIfNull(envelope.Save);
        ArgumentNullException.ThrowIfNull(envelope.Save.Date);
        var json = JsonSerializer.Serialize(envelope, JsonOptions);
        var destination = SlotPath(slot);
        var temporary = Path.Combine(saveDirectory, $"slot-{slot:D2}-{Guid.NewGuid():N}.tmp");
        try
        {
            using (var stream = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                var bytes = System.Text.Encoding.UTF8.GetBytes(json);
                stream.Write(bytes);
                stream.Flush(flushToDisk: true);
            }
            // Same-directory rename publishes a complete file without truncating
            // the previous save. Readers can keep their old file handle open.
            File.Move(temporary, destination, overwrite: true);
        }
        finally
        {
            if (File.Exists(temporary)) File.Delete(temporary);
        }
        return ToSummary(slot, envelope);
    }

    private static StrategySaveSlotSummary ToSummary(int slot, StrategySaveSlotEnvelope envelope)
        => new()
        {
            Slot = slot,
            Occupied = true,
            SavedAtUtc = envelope.SavedAtUtc,
            ScenarioId = envelope.Save.ScenarioId,
            LordName = envelope.LordName,
            DateLabel = FormatGameDate(envelope.Save.Date.Year, envelope.Save.Date.Month, envelope.Save.Date.Day)
        };

    private static string FormatGameDate(int year, int month, int day)
        => $"{year}年{month}月{day}日";

    private static void ValidateSlot(int slot)
    {
        if (slot is < 1 or > MaxSlots)
            throw new ArgumentOutOfRangeException(nameof(slot), slot, $"Slot must be 1..{MaxSlots}.");
    }

    private string SlotPath(int slot) => Path.Combine(saveDirectory, $"slot-{slot:D2}.json");
}
