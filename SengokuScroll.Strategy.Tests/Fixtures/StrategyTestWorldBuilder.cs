using SengokuScroll.Common.Types;
using SengokuScroll.Domain;
using SengokuScroll.Domain.Actions;
using SengokuScroll.Domain.Definitions;
using SengokuScroll.Domain.Entities;
using SengokuScroll.Domain.Entities.Types;
using SengokuScroll.Domain.Types;
using SengokuScroll.Domain.World;
using SengokuScroll.Strategy.Constants;
using SengokuScroll.Strategy.Rules;
using static SengokuScroll.Domain.Entities.Unit;

namespace SengokuScroll.Strategy.Tests.Fixtures;

/// <summary>组装策略测试用 <see cref="GameWorld"/> 的静态构建器。</summary>
public static class StrategyTestWorldBuilder
{
    /// <summary>
    /// 创建 10×10 平地地图，含 1 个势力与 1 个位于 (0,0) 的军事单位。
    /// </summary>
    public static GameWorld BuildMinimalWorld()
    {
        var size = StrategyTestWorldFactory.DefaultMapSize;
        var tileMap = new TileMap(new byte[size * size], new byte[size * size], size, size);

        var terrains = new Dictionary<int, TerrainDefinition>
        {
            [(int)TerrainType.Plain] = new()
            {
                Name = "平地",
                Altitude = 0,
                Description = "平地",
                MovementCost = 2,
                Type = TerrainType.Plain
            }
        };

        var forces = new Dictionary<int, Force> { [1] = CreateTestForce(1) };
        var units = new Dictionary<int, Unit> { [1] = CreateTestUnit(1, 1, new Point3(0, 0)) };

        var world = new GameWorld("strategy_test")
        {
            GameMapMasterData = new GameMapMasterData
            {
                Name = "test",
                Version = "1",
                TileMap = tileMap,
                Terrains = terrains,
                TerrainVegatationFeatures = [],
                TerrainSurfaceFeatures = [],
                Climates = [],
                Regions = [],
                Roads = [],
                Landmarks = []
            },
            GameMapData = new GameMapData
            {
                Strongholds = [],
                Characters = [],
                Units = [],
                Roads = []
            },
            GameMasterData = new GameMasterData
            {
                CultureGroups = [],
                Cultures = [],
                ReligionGroups = [],
                Religions = [],
                StrongholdTypes = [],
                DefenseFacilityTypes = [],
                UnitTypes = [],
                Characters = []
            },
            GameData = new GameData
            {
                GameDate = new GameDate(1, 1, 1),
                Forces = forces,
                Strongholds = [],
                Units = units,
                Characters = [],
                SupplyConvoys = [],
                MessageCarriers = [],
                SubUnits = []
            }
        };

        MapLocationActions.RegisterUnit(world, units[1]);
        return world;
    }

    /// <summary>两军相邻的最小测试世界（野战对峙/决战用）。</summary>
    public static GameWorld BuildAdjacentBattleWorld(Unit attacker, Unit defender)
    {
        var world = BuildMinimalWorld();
        world.GameData.Forces[defender.ForceId] = CreateTestForce(defender.ForceId);
        world.GameData.Units[attacker.Id] = attacker;
        world.GameData.Units[defender.Id] = defender;
        MapLocationActions.RegisterUnit(world, defender);
        return world;
    }

    /// <summary>
    /// 创建含粮库据点与低粮远程单位的测试世界（M1-c 后勤/信使集成）。
    /// </summary>
    /// <param name="unitLocation">前线单位坐标，默认 (3,0)。</param>
    /// <param name="unitFood">单位当前粮，默认 100 合（低于自动补给阈值）。</param>
    public static GameWorld BuildLogisticsWorld(
        Point3? unitLocation = null,
        int unitFood = 100)
    {
        var world = BuildMinimalWorld();
        var location = unitLocation ?? new Point3(3, 0);
        var stronghold = CreateTestStronghold(1, 1, new Point3(0, 0), food: 10_000);
        stronghold.ForceActor.Soldier = 0;

        world.GameData.Strongholds[1] = stronghold;
        world.GameData.Units[1] = CreateTestUnit(1, 1, location, food: unitFood);

        MapLocationActions.RegisterStronghold(world, stronghold);
        MapLocationActions.RegisterUnit(world, world.GameData.Units[1]);

        return world;
    }

    /// <summary>创建最小字段集的测试据点，含官府粮库。</summary>
    public static Stronghold CreateTestStronghold(
        int id,
        int forceId,
        Point3 location,
        int food = 10_000)
        => new()
        {
            Id = id,
            Name = "测试城",
            ForceId = forceId,
            Location = location,
            Population = 1000,
            CommerceValue = 2000,
            ForceActor = CreateTestStrongholdActor(id * 10, forceId, id, food),
            CivilianActor = CreateTestCivilianActor(id * 10 + 1, forceId, id, population: 1000),
            Market = new StrongholdMarket(),
            MerchantActors = [],
            ReligionActors = [],
            DefenseFacilityIds = [],
            EconomyFacilityIds =
            [
                EconomyFacilityConstants.MarketFacilityTypeId,
            ],
            HasCoreForceIds = [forceId],
            Agriculture = new StrongholdAgricultureState()
        };

    /// <summary>创建市民 Actor（含默认月产能）。</summary>
    public static StrongholdActor CreateTestCivilianActor(
        int id,
        int forceId,
        int strongholdId,
        int population = 1000,
        int food = 50_000)
        => new()
        {
            Id = id,
            Name = "民间",
            Type = ActorType.Force,
            ForceId = forceId,
            StrongholdId = strongholdId,
            CharacterIds = [],
            SubUnitIds = [],
            Food = food,
            AgricultureProduction = population * 15,
            CommerceProduction = population * 10
        };

    /// <summary>创建据点内官府/民间 Actor。</summary>
    public static StrongholdActor CreateTestStrongholdActor(
        int id,
        int forceId,
        int strongholdId,
        int food = 0,
        int garrisonSoldiers = 0)
        => new()
        {
            Id = id,
            Name = "测试官府",
            Type = ActorType.Force,
            ForceId = forceId,
            StrongholdId = strongholdId,
            CharacterIds = [],
            SubUnitIds = [],
            Food = food,
            Soldier = garrisonSoldiers > 0 ? garrisonSoldiers : 500
        };

    /// <summary>创建最小字段集的测试势力。</summary>
    public static Force CreateTestForce(int id)
        => new()
        {
            Id = id,
            ForceId = id,
            Name = "测试势力",
            AcceptedCultureIds = [],
            Provinces = [],
            CharacterIds = [],
            Diplomacies = [],
            SubUnitIds = []
        };

    /// <summary>为两势力添加双向敌对外交。</summary>
    public static void LinkEnemyForces(Force forceA, Force forceB)
    {
        forceA.Diplomacies.Add(new Diplomacy
        {
            ForceId = forceA.Id,
            TargetForceId = forceB.Id,
            Relation = Diplomacy.DiplomacyRelation.Enemy
        });
        forceB.Diplomacies.Add(new Diplomacy
        {
            ForceId = forceB.Id,
            TargetForceId = forceA.Id,
            Relation = Diplomacy.DiplomacyRelation.Enemy
        });
    }

    /// <summary>
    /// 创建测试用军事单位，默认移动力 10、当前 AP 10、平地移动消耗 2。
    /// </summary>
    public static Unit CreateTestUnit(int id, int forceId, Point3 location, int food = 0)
        => new()
        {
            Id = id,
            Name = $"Unit{id}",
            ForceId = forceId,
            Location = location,
            Food = food,
            Ap = 10,
            Movement = 10,
            IsMilitary = true,
            IsReadyToMove = true,
            Status = UnitStatus.Waiting,
            Morale = 60,
            SubUnitIds = [],
            ActionTarget = new UnitActionTarget
            {
                RoutePoints = new Queue<Point2>()
            }
        };

    /// <summary>创建测试用运输 Unit（Convoy/Migrant/Merchant 在途）。</summary>
    public static Unit CreateTestTransportUnit(
        int id,
        int forceId,
        Point3 location,
        TransportPurpose purpose = TransportPurpose.Supply,
        UnitKind kind = UnitKind.Convoy)
        => new()
        {
            Id = id,
            Name = $"Transport{id}",
            ForceId = forceId,
            Location = location,
            Ap = 0,
            Movement = LogisticsConstants.ConvoyDailyAp,
            IsMilitary = false,
            Kind = kind,
            Status = UnitStatus.Moving,
            TransportPurpose = purpose,
            PorterCount = LogisticsConstants.DefaultPorterCount,
            EscortSoldierCount = purpose == TransportPurpose.Migrant
                ? 0
                : LogisticsConstants.DefaultEscortSoldierCount,
            Morale = 70,
            SubUnitIds = [],
            ActionTarget = new UnitActionTarget
            {
                RoutePoints = new Queue<Point2>()
            }
        };

    /// <summary>将运输 Unit 登记到测试世界。</summary>
    public static void RegisterTransportUnit(GameWorld world, Unit transport)
    {
        world.GameData.Units[transport.Id] = transport;
        MapLocationActions.RegisterUnit(world, transport);
    }
}
