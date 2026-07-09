using SengokuScroll.Domain.Enums;
using SengokuScroll.Domain.Types;

namespace SengokuScroll.Domain.Entities;

public class GameMessage
{
    public int Id { get; set; }

    public required MessageObject[] MessageObjects { get; set; }

    public EventCategory Category { get; set; }

    public EventType Type { get; set; }

    public Level5 Level { get; set; }

    public GameDate EventTime { get; set; }

    public class MessageObject
    {
        public ObjectType Type { get; set; }

        public int Id { get; set; }
    }

    public enum ObjectType : byte
    {
        Character = 0,
        Force = 1,
        Stronghold = 2,
        Unit = 3,
    }

    public enum EventType : byte
    {
        Message = 0,
        Command = 1
    }

    public enum EventCategory : ushort
    {
        /// <summary>势力建立</summary>
        ForceEstablished = 100,
        /// <summary>势力灭亡</summary>
        ForceDissolved,
        /// <summary>领袖继位</summary>
        ForceLeaderChanged,
        /// <summary>变更文化</summary>
        ForceCultureChanged,
        /// <summary>变更信仰</summary>
        ForceReligionChanged,

        /// <summary>据点建立</summary>
        StrongholdEstablished = 200,
        /// <summary>据点拆除</summary>
        StrongholdDestroyed,
        /// <summary>一夜城</summary>
        StrongholdNightFortEstablished,
        /// <summary>天灾</summary>
        StrongholdNaturalDisaster,
        /// <summary>暴动</summary>
        StrongholdRebellion,
        /// <summary>任免领主</summary>
        StrongholdAppointLord,
        /// <summary>任免代官</summary>
        StrongholdAppointMayor,
        /// <summary>变更文化</summary>
        StrongholdCultureChanged,
        /// <summary>变更信仰</summary>
        StrongholdReligionChanged,

        /// <summary>出征</summary>
        UnitStarted = 300,
        /// <summary>战争优势</summary>
        UnitWarAdvantage,
        /// <summary>战争僵持</summary>
        UnitWarStalemate,
        /// <summary>战争劣势</summary>
        UnitWarDisadvantage,
        /// <summary>混乱</summary>
        UnitChaos,
        /// <summary>恢复</summary>
        UnitRecovery,
        /// <summary>战斗胜利</summary>
        UnitBattleVictory,
        /// <summary>战斗失败</summary>
        UnitBattleDefeated,
        /// <summary>占领敌方据点</summary>
        UnitOccupation,
        /// <summary>投降</summary>
        UnitSurrender,
        /// <summary>阵亡</summary>
        UnitLeaderKilledInAction,
        /// <summary>被处决</summary>
        CharacterExecuted,

        /// <summary>单位指令：任免指挥官</summary>
        UnitAppointLeader,
        /// <summary>单位指令撤退：指示方针</summary>
        UnitRetreat,

        /// <summary>赠礼/进贡</summary>
        DiplomacyTribute = 400,
        /// <summary>联姻（政治婚姻）</summary>
        DiplomacyMarriageAlliance,
        /// <summary>外交侮辱</summary>
        DiplomacyInsult,
        /// <summary>宣战</summary>
        DiplomacyDeclarationOfWar,
        /// <summary>停战</summary>
        DiplomacyCeasefire,
        /// <summary>同盟</summary>
        DiplomacyAlliance,
        /// <summary>解盟</summary>
        DiplomacyAllianceBroken,
        /// <summary>支配</summary>
        DiplomacySubjugation,
        /// <summary>从属</summary>
        DiplomacyVassalage,

        /// <summary>人物死亡</summary>
        Death = 500,
        /// <summary>角色不满</summary>
        CharacterDissatisfaction,
    }


}
