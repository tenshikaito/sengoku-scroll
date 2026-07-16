using SengokuScroll.Application.Models;
using SengokuScroll.Common.Types;
using SengokuScroll.Domain;

namespace SengokuScroll.Application.Commands;

/// <summary>RPG 角色移动命令：指定角色 Id 与目标格。</summary>
public class CharacterMoveCommand : ICommand
{
    public required int CharacterId { get; set; }

    public required Point2 Location { get; set; }
}
