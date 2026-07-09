//using SengokuScroll.Application.Contexts;
//using SengokuScroll.Domain.Entities;

//namespace SengokuScroll.Domain.Services;

//public class GameQueryService
//{
//    public GameResult<Character?> GetCharacter(IGameRequestContext context, int playerSelectedId)
//    {
//        var c = context.GameWorldContext.GetCharacterOrDefault(playerSelectedId);

//        if (!c)
//            return GameError.CharacterNotFound;

//        return c;
//    }

//    public GameResult<InitResultModel> Init(IGameRequestContext context, int characterId)
//    {
//        throw new NotImplementedException();
//    }

//    public class InitResultModel
//    {
//        public int Id { get; set; }

//        public required string Name { get; set; }

//        public required bool IsOnMap { get; set; }

//        //public required CharacterStatus Status { get; set; }
//    }
//}
