using SengokuScroll.Domain.Enums;

namespace SengokuScroll.Domain.Entities.Types;

public struct ClimateFactor
{
    public Level5 BaseTemperature { get; set; }

    public Level5 BaseWet { get; set; }
}