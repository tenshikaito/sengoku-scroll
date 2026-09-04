using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SengokuScroll.Application.Commands;
using SengokuScroll.Application.Constants;
using SengokuScroll.Common.Extensions;
using SengokuScroll.Common.Types;
using SengokuScroll.Domain.Contexts;
using SengokuScroll.Domain.Events;

namespace SengokuScroll.Application.Tests.Tests;

public class GameTests(ITestOutputHelper output) : GameTestsBase(output)
{
    [Fact]
    public Task Run()
        => Build(async (game, playerCharacter) =>
        {
            try
            {
                game.Start(true);

                var gameEventHandler = new MovementEventHandler(output);

                game.RegisterEventHandler(gameEventHandler);

                var r = await game.SendCommandAsync(new CharacterMoveCommand()
                {
                    CharacterId = 1,
                    Location = new(1, 1)
                });

                output.WriteLine(r.ToJson());

                Assert.True(r.IsSuccess);

                game.Resume();
                game.Resume();
                game.Resume();

                Assert.Equal(new Point3(1, 1), playerCharacter.Location);
            }
            finally
            {
                await game.StopAsync();
            }
        }, services =>
        {
            services.RemoveAllKeyed<IGameLoop>(ServiceConstants.GameEventLoop);
            services.AddKeyedSingleton<IGameLoop, TestGameLoop>(ServiceConstants.GameEventLoop);
        });

    private class MovementEventHandler(ITestOutputHelper output) : IGameEventHandler<CharacterMovedEvent>
    {
        public void Handle(CharacterMovedEvent e)
        {
            output.WriteLine($"CharacterMovedEvent: Id={e.CharacterId}, Name={e.CharacterName}, From={e.From}, To={e.To}");

            switch (e.From)
            {
                case Point2 p when p == new Point2(0, 0):
                    Assert.True(e.To == new Point2(1, 0));
                    break;
                case Point2 p when p == new Point2(1, 0):
                    Assert.True(e.To == new Point2(1, 1));
                    break;
                default:
                    Assert.Fail();
                    break;
            }
        }
    }
}
