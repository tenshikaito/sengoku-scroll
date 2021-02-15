using Library;
using System;
using System.Collections.Generic;

namespace Core.Model
{
    public class Force : BaseObject
    {
        public int id;
        public string name;
        public Status status;
        /// <summary>表示宗主势力</summary>
        public int? suzerainId;
        /// <summary>表示势力范围</summary>
        public int? orbitId;

        /// <summary>家督</summary>
        public int leaderId;

        public DateTime foundTime;

        public int stronghold;
        /// <summary>将领数包含当主</summary>
        public int character;
        public int general;

        public int food;
        public int storedFood;

        public int population;
        public int labour;
        public Supplies supplies;

        public int capitalId;
        public int heirId;

        public string introduction;

        public bool isInvalid;
        /// <summary>和平状态中</summary>
        public bool isPeaceStatus;

        public bool isHomeless => id == 0;
        /// <summary>是附属势力</summary>
        public bool isIndependence => suzerainId != null;

        public List<string> positions;
        public Dictionary<int, Relationship> relationship;
        public readonly HashSet<AskJobApply> askJobApplies = new HashSet<AskJobApply>();

        public Relationship getRelationship(int force)
        {
            if (isSelf(force)) return Relationship.self;

            if (relationship.TryGetValue(force, out var value)) return value;

            return Relationship.none;
        }

        public bool setRelationship(int force, Relationship r)
        {
            if (id == force) return false;

            if (r == Relationship.normal) relationship.Remove(force);
            else relationship[force] = r;

            return true;
        }

        public bool isSelf(int force) => id == force;

        public bool isAlly(int force) => getRelationship(force) == Relationship.ally;

        public bool isEnemy(int force) => getRelationship(force) == Relationship.enemy;

        public bool isNatural(int force) => getRelationship(force) == Relationship.normal;

        public enum Status : byte
        {
            dependent = 0,
            detailIndependent = 1,
            mainIndependent = 2
        }
    }
}
