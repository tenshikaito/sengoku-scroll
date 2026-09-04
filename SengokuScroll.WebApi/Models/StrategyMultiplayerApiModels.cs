using SengokuScroll.Strategy.Models;

namespace SengokuScroll.WebApi.Models;

public sealed record StrategyMultiplayerCreateRoomRequest
{
    public string RoomName { get; init; } = "战国房间";

    public string PlayerName { get; init; } = "玩家";

    public string ScenarioId { get; init; } = "mini_kanto";

    public string? Difficulty { get; init; }

    public GameStartOptionsDto? CustomStartOptions { get; init; }

    public int ForceId { get; init; } = 1;

    public int MaxPlayers { get; init; } = 8;
}

public sealed record StrategyMultiplayerJoinRoomRequest
{
    public string PlayerName { get; init; } = "玩家";

    public required int ForceId { get; init; }
}

public sealed record StrategyMultiplayerReconnectRequest
{
    public required string PlayerId { get; init; }

    public required string PlayerToken { get; init; }
}

public sealed record StrategyMultiplayerReadyRequest
{
    public bool Ready { get; init; } = true;
}

public sealed record StrategyMultiplayerRoomResponse
{
    public required StrategyMultiplayerRoomDto Room { get; init; }

    public required StrategyMultiplayerCredentialsDto Credentials { get; init; }
}

public sealed record StrategyMultiplayerReadyResponse
{
    public required StrategyMultiplayerRoomDto Room { get; init; }

    public required StrategyAdvanceDayResponseDto Advance { get; init; }

    public required bool Advanced { get; init; }
}

public sealed record StrategyMultiplayerCredentialsDto
{
    public required string PlayerId { get; init; }

    public required string PlayerToken { get; init; }

    public required int ForceId { get; init; }

    public required bool IsHost { get; init; }
}

public sealed record StrategyMultiplayerRoomDto
{
    public required string RoomId { get; init; }

    public required string RoomName { get; init; }

    public required string ScenarioId { get; init; }

    public required string Status { get; init; }

    public required int MaxPlayers { get; init; }

    public required int PlayerCount { get; init; }

    public required long WorldVersion { get; init; }

    public required IReadOnlyList<StrategyMultiplayerPlayerDto> Players { get; init; }

    public required IReadOnlyList<StrategyMultiplayerForceDto> Forces { get; init; }
}

public sealed record StrategyMultiplayerPlayerDto
{
    public required string PlayerId { get; init; }

    public required string PlayerName { get; init; }

    public required int ForceId { get; init; }

    public required bool IsHost { get; init; }

    public required bool Ready { get; init; }

    public required bool Connected { get; init; }
}

public sealed record StrategyMultiplayerForceDto
{
    public required int ForceId { get; init; }

    public required string ForceName { get; init; }

    public required string Category { get; init; }

    public required bool Occupied { get; init; }
}
