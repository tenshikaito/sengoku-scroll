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
        var path = SlotPath(slot);
        if (!File.Exists(path))
            return null;

        try
        {
            var json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<StrategySaveSlotEnvelope>(json, JsonOptions);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    public StrategySaveSlotSummary WriteSlot(int slot, StrategySaveSlotEnvelope envelope)
    {
        ValidateSlot(slot);
        var json = JsonSerializer.Serialize(envelope, JsonOptions);
        File.WriteAllText(SlotPath(slot), json);
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
