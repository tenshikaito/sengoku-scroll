using SengokuScroll.Strategy.Persistence;

namespace SengokuScroll.Strategy.Tests;

public class StrategySaveSlotRepositoryTests : IDisposable
{
    private readonly string tempRoot;
    private readonly StrategySaveSlotRepository repository;

    public StrategySaveSlotRepositoryTests()
    {
        tempRoot = Path.Combine(Path.GetTempPath(), "sengoku-save-slots-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempRoot);
        repository = new StrategySaveSlotRepository(Path.Combine(tempRoot, "strategy-saves"));
    }

    [Fact]
    public void ListSlots_EmptyDirectory_ReturnsTenEmptySlots()
    {
        var slots = repository.ListSlots();
        Assert.Equal(StrategySaveSlotRepository.MaxSlots, slots.Count);
        Assert.All(slots, slot => Assert.False(slot.Occupied));
    }

    [Fact]
    public void WriteSlot_ThenReadSummary_ReturnsOccupiedMetadata()
    {
        var envelope = new StrategySaveSlotEnvelope
        {
            SavedAtUtc = new DateTime(2026, 7, 22, 3, 0, 0, DateTimeKind.Utc),
            LordName = "织田信长",
            Save = new StrategySaveDocument
            {
                ScenarioId = "mini_kanto",
                PlayerForceId = 1,
                Date = new StrategySaveDate { Year = 1560, Month = 1, Day = 1 },
                Forces = [],
                Strongholds = [],
                Units = []
            }
        };

        var summary = repository.WriteSlot(3, envelope);

        Assert.True(summary.Occupied);
        Assert.Equal(3, summary.Slot);
        Assert.Equal("织田信长", summary.LordName);
        Assert.Equal("1560年1月1日", summary.DateLabel);

        var listed = repository.ListSlots().Single(s => s.Slot == 3);
        Assert.True(listed.Occupied);
        Assert.Equal("mini_kanto", listed.ScenarioId);
    }

    public void Dispose()
    {
        if (Directory.Exists(tempRoot))
            Directory.Delete(tempRoot, recursive: true);
    }
}
