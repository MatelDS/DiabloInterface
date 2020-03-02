using Zutatensuppe.D2Reader.Struct;
using System;
using System.Linq;
using System.Collections.Generic;
using Zutatensuppe.D2Reader.Struct.Item;
using Zutatensuppe.D2Reader.Struct.Skill;
using Zutatensuppe.D2Reader.Struct.Stat;
using Zutatensuppe.D2Reader.Models;

namespace Zutatensuppe.D2Reader.Readers
{
    public class UnitReader
    {
        protected ProcessMemoryReader reader;
        protected GameMemoryTable memory;
        protected StringLookupTable stringReader;

        internal SkillReader skillReader;
        public InventoryReader inventoryReader;

        D2Unit player = null;

        public UnitReader(
            ProcessMemoryReader reader,
            GameMemoryTable memory,
            StringLookupTable stringReader
        ) {
            this.reader = reader;
            this.memory = memory;
            this.stringReader = stringReader;
            skillReader = new SkillReader(reader, memory);
            inventoryReader = createInventoryReader();
        }

        protected virtual InventoryReader createInventoryReader()
        {
            return new InventoryReader(reader, new ItemReader(reader, memory, stringReader));
        }

        public virtual void ResetCache()
        {
            player = null;
        }

        public D2Unit GetPlayer()
        {
            if (player == null && (long)memory.Address.PlayerUnit > 0)
            {
                IntPtr playerAddress = reader.ReadAddress32(memory.Address.PlayerUnit, AddressingMode.Relative);
                // when no game is started yet, the player address will be 0 and
                // there will be no benefit in trying to read it
                if ((long)playerAddress > 0)
                {
                    player = reader.Read<D2Unit>(playerAddress);
                }
            }

            return player;
        }

        public List<D2Stat> GetItemStats(D2Unit unit)
        {
            var player = GetPlayer();
            if (player == null)
                return new List<D2Stat>();

            // Build filter to get only equipped items and items in inventory
            bool filter(D2ItemData data) =>
               !data.ItemFlags.HasFlag(ItemFlag.RequirementsNotMet)
               &&
               (
                   (data.InvPage == InventoryPage.Equipped && data.BodyLoc != BodyLocation.SecondaryLeft && data.BodyLoc != BodyLocation.SecondaryRight)
                   ||
                   (data.InvPage == InventoryPage.Inventory)
               )
            ;

            List<D2Stat> statList = new List<D2Stat>();
            foreach (D2Unit item in inventoryReader.EnumerateInventoryBackward(player, filter))
            {
                List<D2Stat> itemStats = GetStats(item);
                if (itemStats == null)
                {
                    continue;
                }
                
                D2ItemData itemData = reader.Read<D2ItemData>(item.UnitData);
                if (itemData.InvPage == InventoryPage.Inventory && !(item.eClass == 605 || item.eClass == 604 || item.eClass == 603)) { 

                    // The item is in inventory, but it is not a charm
                    continue;

                } else
                {
                    // The item is either equipped (see filter above) or is a charm in inventory
                }
                
                List<D2Stat> magicalItems = inventoryReader.ItemReader.GetMagicalStats(item);
                foreach (D2Stat stat in magicalItems)
                {
                    statList.Add(stat);
                }

            }

            return statList;
        }

        public List<D2Stat> GetStats(D2Unit unit)
        {
            if (unit == null)
                return null;
            if (unit.StatListNode.IsNull)
                return null;

            var node = reader.Read<D2StatListEx>(unit.StatListNode);

            // Get the best available stat array.
            D2StatArray statArray = node.BaseStats;
            if (node.ListFlags.HasFlag(StatListFlag.HasCompleteStats))
                statArray = node.FullStats;

            // Empty list.
            if (node.FullStats.Length == 0)
                return new List<D2Stat>();

            // Return the array data and return as list.
            var stats = reader.ReadArray<D2Stat>(statArray.Address, statArray.Length);
            return new List<D2Stat>(stats);
        }
        

        public Dictionary<int, D2Skill> GetSkillMap(D2Unit unit)
        {
            Dictionary<int, D2Skill> skills = new Dictionary<int, D2Skill>();
            foreach (D2Skill skill in skillReader.EnumerateSkills(unit))
            {
                int numberOfSkillPoints = skillReader.GetTotalNumberOfSkillPoints(skill);
                if (numberOfSkillPoints > 0)
                {
                    D2SkillData skillData = skillReader.ReadSkillData(skill);
                    if (skillData.ClassId >= 0 && skillData.ClassId <= 6)
                    {
                        string skillName = skillReader.GetSkillName((ushort)skillData.SkillId);
                        int skillPoints = skill.numberOfSkillPoints;
                    }
                }
            }

            return skills;
        }

        public Dictionary<StatIdentifier, D2Stat> GetStatsMap(D2Unit unit)
        {
            List<D2Stat> stats = GetStats(unit);
            if (stats == null) return null;

            return (from stat in stats
                    where stat.HasValidLoStatIdentifier()
                    group stat by (StatIdentifier)stat.LoStatID into g
                    select g).ToDictionary(x => x.Key, x => x.Single());
        }

        public Dictionary<StatIdentifier, D2Stat> GetItemStatsMap(D2Unit unit)
        {
            List<D2Stat> stats = GetItemStats(unit);
            if (stats == null)
            {
                return null;
            }
            Dictionary<StatIdentifier, D2Stat> dict = new Dictionary<StatIdentifier, D2Stat>();

            foreach ( D2Stat stat in stats )
            {
                if (!stat.HasValidLoStatIdentifier())
                    continue;

                D2Stat s = new D2Stat(stat);

                if (dict.ContainsKey((StatIdentifier)stat.LoStatID))
                {
                    dict[(StatIdentifier)stat.LoStatID].Value = dict[(StatIdentifier)stat.LoStatID].Value + s.Value;
                } else
                {
                    dict[(StatIdentifier)stat.LoStatID] = s;
                }
            }

            return dict;
            
        }

        public int? GetStatValue(D2Unit unit, ushort statId)
        {
            List<D2Stat> stats = GetStats(unit);
            if (stats == null)
            {
                return null;
            }

            foreach (D2Stat stat in stats)
            {
                if (stat.LoStatID == statId)
                {
                    return stat.Value;
                }
            }

            return null;
        }

        public int? GetStatValue(D2Unit unit, StatIdentifier statId)
        {
            return GetStatValue(unit, (ushort)statId);
        }

        public bool IsNewChar(D2Unit unit)
        {
            return MatchesStartingProps(unit)
                && MatchesStartingItems(unit)
                && MatchesStartingSkills(unit);
        }

        private bool MatchesStartingProps(D2Unit p)
        {
            // check -act2/3/4/5 level|xp
            int level = GetStatValue(p, StatIdentifier.Level) ?? 0;
            int experience = GetStatValue(p, StatIdentifier.Experience) ?? 0;

            // first we will check the level and XP
            // act should be set to the act we are currently in
            return
                (level == 1 && experience == 0 && p.actNo == 0)
                || (level == 16 && experience == 220165 && p.actNo == 1)
                || (level == 21 && experience == 839864 && p.actNo == 2)
                || (level == 27 && experience == 2563061 && p.actNo == 3)
                || (level == 33 && experience == 7383752 && p.actNo == 4);
        }

        private bool MatchesStartingItems(D2Unit p)
        {
            int[] list = (
                from item
                in inventoryReader.EnumerateInventoryForward(p)
                select item.eClass
            ).ToArray();

            return list.SequenceEqual(Character.StartingItems[(CharacterClass)p.eClass]);
        }

        private bool MatchesStartingSkills(D2Unit p)
        {
            int skillCount = 0;
            foreach (D2Skill skill in skillReader.EnumerateSkills(p))
            {
                var skillData = skillReader.ReadSkillData(skill);
                Skill skillId = (Skill)skillData.SkillId;
                if (!Character.StartingSkills[(CharacterClass)p.eClass].ContainsKey(skillId))
                {
                    return false;
                }

                if (Character.StartingSkills[(CharacterClass)p.eClass][skillId] != skillReader.GetTotalNumberOfSkillPoints(skill))
                {
                    return false;
                }
                skillCount++;
            }

            return skillCount == Character.StartingSkills[(CharacterClass)p.eClass].Count;
        }
    }
}
