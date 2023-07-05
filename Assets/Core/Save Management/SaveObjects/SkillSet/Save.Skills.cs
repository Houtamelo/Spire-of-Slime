using System.Collections.Generic;
using Core.Combat.Scripts.Interfaces;
using Core.Combat.Scripts.Skills;
using Core.Combat.Scripts.Skills.Interfaces;
using Core.Main_Database.Combat;
using Core.Utils.Patterns;
using UnityEngine;
using Utils.Patterns;

namespace Core.Save_Management.SaveObjects
{
    public partial class Save 
    {
        /// <summary> Skills are perks too. </summary>
        public void UnlockPerk(CleanString key)
        {
            if (Booleans.TryGetValue(key, out bool oldValue) && oldValue == true)
            {
                Debug.LogWarning($"Trying to unlock perk {key} but it is already unlocked");
                return;
            }

            SetDirty();
            Booleans[key] = true;
            if (key.StartsWith("perk_ethel") || key.StartsWith("skill_ethel"))
            {
                if (_ethelStats.AvailablePerkPoints == 0)
                {
                    Debug.LogWarning($"Trying to unlock perk {key} but Ethel has no perk points");
                    return;
                }
                
                uint oldPoints = _ethelStats.AvailablePerkPoints;
                uint newPoints = _ethelStats.AvailablePerkPoints - 1;
                _ethelStats.AvailablePerkPoints = newPoints;
                FloatChanged?.Invoke(VariablesName.Ethel_AvailablePerkPoints, oldPoints, newPoints);
            }
            else if (key.StartsWith("perk_nema") || key.StartsWith("skill_nema"))
            {
                if (_nemaStats.AvailablePerkPoints == 0)
                {
                    Debug.LogWarning($"Trying to unlock perk {key} but Nema has no perk points");
                    return;
                }
                
                uint oldPoints = _nemaStats.AvailablePerkPoints;
                uint newPoints = _nemaStats.AvailablePerkPoints - 1;
                _nemaStats.AvailablePerkPoints = newPoints;
                FloatChanged?.Invoke(VariablesName.Nema_AvailablePerkPoints, oldPoints, newPoints);
            }
            else
            {
                Debug.LogWarning($"Trying to unlock perk {key} but it does not start with perk_ or skill_, no character will lose a perk point");
            }
            
            BoolChanged?.Invoke(key, oldValue: false, newValue: true);
        }

        /// <summary> Does not refund perk point. </summary>
        public void LockPerk(CleanString variableName)
        {
            if (Booleans.TryGetValue(variableName, out bool oldValue) == false || oldValue == false)
            {
                Debug.LogWarning($"Trying to lock perk {variableName.ToString()} but it is already locked");
                return;
            }
             
            SetDirty();
            Booleans[variableName] = false;
            BoolChanged?.Invoke(variableName, oldValue: true, newValue: false);
        }

        public void AssignSkill(CleanString characterKey, CleanString skillKey)
        {
            if (GetStats(characterKey).AssertSome(out CharacterStats stats) == false)
                return;

            CleanString oldValue = string.Empty;
            int index;
            if (stats.SkillSet.One.IsNullOrEmpty())
            {
                stats.SkillSet.One = skillKey;
                index = 0;
            }
            else if (stats.SkillSet.Two.IsNullOrEmpty())
            {
                stats.SkillSet.Two = skillKey;
                index = 1;
            }
            else if (stats.SkillSet.Three.IsNullOrEmpty())
            {
                stats.SkillSet.Three = skillKey;
                index = 2;
            }
            else
            {
                oldValue = stats.SkillSet.Four;
                stats.SkillSet.Four = skillKey;
                index = 3;
            }

            CleanString variableName = VariablesName.AssignedSkillName(stats.Key, index);
            if (oldValue != skillKey)
                StringChanged?.Invoke(variableName, oldValue.ToString(), skillKey.ToString());
        }
        
        public bool UnassignSkill(CleanString characterKey, ISkill skill) => UnassignSkill(characterKey, skill.Key);
        
        public bool UnassignSkill(CleanString characterKey, CleanString skillKey)
        {
            if (GetStats(characterKey).AssertSome(out CharacterStats stats) == false)
                return false;
            
            SetDirty();
            bool foundAny = false;
            for (int index = 0; index < 4; index++)
            {
                if (stats.SkillSet.Get(index) == skillKey)
                {
                    CleanString oldOne = stats.SkillSet.One, oldTwo = stats.SkillSet.Two, oldThree = stats.SkillSet.Three, oldFour = stats.SkillSet.Four;
                    stats.SkillSet.Set(index, string.Empty);
                    foundAny = true;
                    Sort(stats, oldOne, oldTwo, oldThree, oldFour);
                    break;
                }
            }

            return foundAny;
        }

        public bool UnassignSkill(CleanString characterKey, int slotIndex)
        {
            if (GetStats(characterKey).AssertSome(out CharacterStats stats) == false)
                return false;
            
            SetDirty();
            if (slotIndex is < 0 or > 3)
                return false;

            CleanString oldOne = stats.SkillSet.One, oldTwo = stats.SkillSet.Two, oldThree = stats.SkillSet.Three, oldFour = stats.SkillSet.Four;
            stats.SkillSet.Set(slotIndex, string.Empty);
            Sort(stats, oldOne, oldTwo, oldThree, oldFour);
            return true;
        }

        public bool SwitchSkillSlot(CleanString characterKey, ISkill skill, int slotIndex)
        {
            if (GetStats(characterKey).AssertSome(out CharacterStats stats) == false)
                return false;
            
            SetDirty();
            SkillSet skillSet = stats.SkillSet;
            CleanString oldOne = skillSet.One, oldTwo = skillSet.Two, oldThree = skillSet.Three, oldFour = skillSet.Four;

            for (int index = 0; index < 4; index++)
            {
                CleanString atIndex = skillSet.Get(index);
                if (atIndex != skill.Key)
                    continue;

                if (index == slotIndex)
                    return true;

                CleanString atTargetIndex = skillSet.Get(slotIndex);
                skillSet.Set(index,     atTargetIndex);
                skillSet.Set(slotIndex, atIndex);
                Sort(stats, oldOne, oldTwo, oldThree, oldFour);
                return true;
            }

            return false;
        }

        public void OverrideSkill(CleanString characterKey, ISkill skill, int slotIndex) => OverrideSkill(characterKey, skill.Key, slotIndex);

        public void OverrideSkill(CleanString characterKey, CleanString skillKey, int slotIndex) 
        {
            if (GetStats(characterKey).AssertSome(out CharacterStats stats) == false)
                return;
            
            SetDirty();
            CleanString oldOne = stats.SkillSet.One, oldTwo = stats.SkillSet.Two, oldThree = stats.SkillSet.Three, oldFour = stats.SkillSet.Four;
            stats.SkillSet.Set(slotIndex, skillKey);
            Sort(stats, oldOne, oldTwo, oldThree, oldFour);
        }
        
        public Option<CleanString> GetSkill(CleanString characterKey, int index)
        {
            if (GetStats(characterKey).AssertSome(out CharacterStats stats) == false)
                return Option.None;
            
            CleanString skill = stats.SkillSet.Get(index);
            return skill.IsNullOrEmpty() ? Option.None : skill;
        }

        public void GetSkills(CleanString characterKey, in IList<ISkill> destinationList, ICharacterScript character, ref int hash)
        {
            if (GetStats(characterKey).AssertSome(out CharacterStats stats) == false)
                return;
            
            SetDirty();
            int currentHash = stats.SkillSet.GetHashCode();
            if (currentHash == hash)
                return;

            CleanString oldOne = stats.SkillSet.One, oldTwo = stats.SkillSet.Two, oldThree = stats.SkillSet.Three, oldFour = stats.SkillSet.Four;
            hash = currentHash;
                
            destinationList.Clear();
            IReadOnlyList<ISkill> possibleSkills = character.GetAllPossibleSkills();
            foreach (CleanString skillKey in stats.SkillSet)
            {
                bool found = false;
                foreach (ISkill skill in possibleSkills)
                {
                    if (skill.Key != skillKey)
                        continue;

                    destinationList.Add(skill);
                    found = true;
                    break;
                }

                if (found)
                    continue;

                Debug.LogWarning($"Skill with key {skillKey.ToString()} not found in character {character.CharacterName}. Searching on database.");
                if (SkillDatabase.GetSkill(skillKey).AssertSome(out SkillScriptable skillScriptable))
                    destinationList.Add(skillScriptable);
            }

            Sort(stats, oldOne, oldTwo, oldThree, oldFour);
        }

        public static void Sort(CharacterStats stats, CleanString oldOne, CleanString oldTwo, CleanString oldThree, CleanString oldFour)
        {
            for (int i = 0; i < 4; i++)
            {
                if (stats.SkillSet.Get(i).IsSome())
                    continue;

                for (int j = i + 1; j < 4; j++)
                {
                    if (stats.SkillSet.Get(j).IsNullOrEmpty())
                        continue;

                    CleanString iValue = stats.SkillSet.Get(i);
                    CleanString jValue = stats.SkillSet.Get(j);
                    stats.SkillSet.Set(i, jValue);
                    stats.SkillSet.Set(j, iValue);
                    break;
                }
            }

            if (oldOne != stats.SkillSet.One)
                StringChanged?.Invoke(VariablesName.AssignedSkillName(stats.Key, index: 0), oldOne.ToString(), newValue: stats.SkillSet.One.ToString());

            if (oldTwo != stats.SkillSet.Two)
                StringChanged?.Invoke(VariablesName.AssignedSkillName(stats.Key, index: 1), oldTwo.ToString(), newValue: stats.SkillSet.Two.ToString());

            if (oldThree != stats.SkillSet.Three)
                StringChanged?.Invoke(VariablesName.AssignedSkillName(stats.Key, index: 2), oldThree.ToString(), newValue: stats.SkillSet.Three.ToString());

            if (oldFour != stats.SkillSet.Four)
                StringChanged?.Invoke(VariablesName.AssignedSkillName(stats.Key, index: 3), oldFour.ToString(), newValue: stats.SkillSet.Four.ToString());
        }
    }
}