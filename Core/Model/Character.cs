using System;
using System.Collections.Generic;
using System.Linq;

namespace Core.Model
{
    public class Character : TileObject
    {
        public static readonly List<Ability.Type> abilityList = Enum.GetValues(typeof(Ability.Type)).Cast<Ability.Type>().ToList();

        public static readonly List<Skill.Type> skillList = Enum.GetValues(typeof(Skill.Type)).Cast<Skill.Type>().ToList();

        public string name { get; set; }
        public string code { get; set; }
        public int type { get; set; }
        public int force { get; set; }

        public int stronghold { get; set; }
        public int home;
        public int culture;
        public int religion;
        public Policy policy = Policy.none;
        public string title;
        public string position;
        public int salary;
        public TimeSpan? joinTime;
        /// <summary>功勋</summary>
        public int contribution;

        public byte hp;
        public int currency;
        public Status status = Status.normal;
        public int unitId;
        public bool isShip;

        public Dictionary<Ability.Type, Ability> ability { get; set; }
        public Dictionary<Skill.Type, Skill> skill { get; set; }
        public HashSet<int> strategy { get; set; }

        public HashSet<int> generals;
        public HashSet<int> loadingGenerals;

        public Ability leadership => ability[Ability.Type.leadership];
        public Ability power => ability[Ability.Type.power];
        public Ability politics => ability[Ability.Type.politics];
        public Ability wisdom => ability[Ability.Type.wisdom];
        public Ability charm => ability[Ability.Type.charm];

        public Skill infantry => skill[Skill.Type.infantry];
        public Skill ride => skill[Skill.Type.ride];
        public Skill matchlock => skill[Skill.Type.matchlock];
        public Skill sailing => skill[Skill.Type.sailing];
        public Skill archery => skill[Skill.Type.archery];
        public Skill fight => skill[Skill.Type.fight];
        public Skill military => skill[Skill.Type.military];
        public Skill spy => skill[Skill.Type.spy];
        public Skill construct => skill[Skill.Type.construct];
        public Skill reclaim => skill[Skill.Type.reclaim];
        public Skill mineral => skill[Skill.Type.mineral];
        public Skill arithmetic => skill[Skill.Type.arithmetic];
        public Skill manners => skill[Skill.Type.manners];
        public Skill eloquence => skill[Skill.Type.eloquence];
        public Skill social => skill[Skill.Type.social];
        public Skill healing => skill[Skill.Type.healing];

        /// <summary>不可用字典、因为同一种类型可能存在placing状态的数据</summary>
        public List<int> items;

        public int maxLoadWight;
        public int maxLoadVolume;

        public int? weapon;

        public int? armor;

        public int? mount;

        public Character init()
        {
            ability = abilityList.ToDictionary(o => o, o => new Ability() { value = 1 });
            skill = skillList.ToDictionary(o => o, o => new Skill());

            return this;
        }

        public enum Status : byte
        {
            normal = 0,
            unit = 1,
            arrested = 2
        }
    }

    public class Ability
    {
        public int value;
        public int exp;

        public static implicit operator int(Ability s) => s.value;

        public enum Type : byte
        {
            leadership = 1,
            power = 2,
            politics = 3,
            wisdom = 4,
            charm = 5
        }
    }

    public class Skill
    {
        public int value;
        public int exp;

        public static implicit operator int(Skill s) => s.value;

        public enum Type : byte
        {
            infantry = 1,
            ride = 2,
            matchlock = 3,
            sailing = 4,
            archery = 5,
            fight = 6,
            military = 7,
            spy = 8,
            construct = 9,
            reclaim = 10,
            mineral = 11,
            arithmetic = 12,
            manners = 13,
            eloquence = 14,
            social = 15,
            healing = 16,
        }
    }
}
