using SengokuScroll.Application.Data;
using SengokuScroll.Domain;
using SengokuScroll.Domain.Entities.Types;
using SengokuScroll.Domain.World;

namespace SengokuScroll.Application.Tests.Data;

public class ExampleGameDataProcessor : IGameDataProcessor
{
    public GameWorld Load(string gameWorldName)
    {
        var width = 1000;
        var height = 1000;

        var gw = new GameWorld(gameWorldName)
        {
            GameMapMasterData = new()
            {
                Name = "testmap",
                Version = "1.0",
                TileMap = new TileMap(new byte[width * height], new byte[width * height], width, height),
                Terrains = new() {
                    {
                        (int)TerrainType.Plain,
                        new ()
                        {
                            Name = "平地",
                            Altitude = 0,
                            Description = "平地",
                            MovementCost = 2,
                            Type = Domain.Entities.Types.TerrainType.Plain
                        }
                    }
                },
                TerrainVegatationFeatures = [],
                TerrainSurfaceFeatures = [],
                Climates = [],
                Regions = [],
                Roads = [],
                Landmarks = []
            },

            GameMapData = new()
            {
                Strongholds = [],
                Characters = [],
                Units = [],
                Roads = []
            },

            GameMasterData = new()
            {
                ProficiencyMaxValue = 0,
                CultureGroups = [],
                Cultures = [],
                ReligionGroups = [],
                Religions = [],
                StrongholdTypes = [],
                DefenseFacilityTypes = [],
                UnitTypes = [],
                Characters = [],
            },

            GameData = new()
            {
                Forces = [],
                Strongholds = [],
                Units = [],
                SupplyConvoys = [],
                MessageCarriers = [],
                SubUnits = [],
                Characters = new()
                {
                    {
                        1,
                        new ()
                        {
                            Id = 1,
                            Name = "织田信长",
                            Portrait = "1.png",
                            Description = "织田信长（1534年6月23日－1582年6月21日），日本战国时代的武将、政治家，尾张国（今爱知县西部）出身。幼名吉法师，后改名为信长，字太守，号天王寺屋敷。父亲是织田信秀，母亲是土田御前。信长是日本战国时代最具影响力的人物之一，以其军事才能和政治手腕闻名。他通过一系列的战争和联盟，成功地统一了日本的大部分地区，并为后来的德川幕府奠定了基础。信长的统治时期被称为“安土桃山时代”，以其文化繁荣和艺术发展而闻名。他的统治结束于1582年，当时他在本能寺遭到部下明智光秀的背叛和袭击，最终自杀身亡。",
                            Personality = new ()
                            {
                                Ambition = 100,
                            },
                            Ap = 2,
                            Proficiency = new ()
                            {
                                Agriculture = 1,
                                Archery = 1,
                                Commerce = 1,
                                Construct = 1,
                                Court = 1,
                                Eloquence = 1,
                                Fighting = 1,
                                Firelock = 1,
                                Healing = 1,
                                Infantry = 1,
                                Military = 1,
                                Ride = 1,
                                Sealing= 1,
                                Smelt = 1,
                                Sociality = 1,
                                Spy = 1,
                            },
                            ActionTarget = new (){ RoutePoints = [] }
                        }
                    },
                    {
                        2,
                        new ()
                        {
                            Id = 2,
                            Name = "木下秀吉",
                            Portrait = "2.png",
                            Description = "丰臣秀吉（1537年3月17日－1598年9月18日），日本战国时代的武将、政治家，尾张国（今爱知县西部）出身。幼名日吉丸，后改名为秀吉，字太守，号羽柴。父亲是木下弥右卫门，母亲是大政所。秀吉是日本战国时代最具影响力的人物之一，以其军事才能和政治手腕闻名。他通过一系列的战争和联盟，成功地统一了日本的大部分地区，并为后来的德川幕府奠定了基础。秀吉的统治时期被称为“安土桃山时代”，以其文化繁荣和艺术发展而闻名。他的统治结束于1598年，当时他因病去世，享年61岁。",
                            Personality = new ()
                            {
                                Ambition = 100,
                            },
                            Proficiency = new ()
                            {
                                Agriculture = 1,
                                Archery = 1,
                                Commerce = 1,
                                Construct = 1,
                                Court = 1,
                                Eloquence = 1,
                                Fighting = 1,
                                Firelock = 1,
                                Healing = 1,
                                Infantry = 1,
                                Military = 1,
                                Ride = 1,
                                Sealing= 1,
                                Smelt = 1,
                                Sociality = 1,
                                Spy = 1,
                            },
                            ActionTarget = new (){ RoutePoints = [] }
                        }
                    }
                }
            },

        };

        return gw;
    }

    public void save(GameWorld gw)
    {
        throw new NotImplementedException();
    }
}
