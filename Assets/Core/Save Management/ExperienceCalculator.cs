using System;
using System.Text;
using Core.Combat.Scripts;
using Core.Main_Database.Combat;
using Core.Save_Management.SaveObjects;
using Core.Utils.Extensions;
using Core.Utils.Math;
using Core.Utils.Patterns;
using JetBrains.Annotations;
using UnityEngine;
using Save = Core.Save_Management.SaveObjects.Save;

namespace Core.Save_Management
{
    public static class ExperienceCalculator
    {
        private static readonly StringBuilder Builder = new();
        private static readonly CleanString KillCountVariableStart = "killcount_";
        
        private const double Neperian = Math.E;

        private const int ExperienceAcquiringPostProcess = 25;
        public const int ExperienceNeededForLevelUp = 100;

        /// <summary> NOT PURE! This increments the enemy's kill counter </summary>
        public static int GetExperienceFromEnemy(CleanString characterKey, Save save)
        {
            Option<CharacterScriptable> character = CharacterDatabase.GetCharacter(characterKey);
            if (character.AssertSome(out CharacterScriptable script) == false)
                return 0;
            
            CleanString killCountVariableName = Builder.Override(KillCountVariableStart.ToString(), characterKey.ToString()).ToString();
            int killCount = save.GetVariable<int>(killCountVariableName);
            int killCountValue;
            if (killCount == 0)
            {
                killCountValue = 1;
                save.SetVariable(killCountVariableName, killCountValue);
            }
            else
            {
                killCountValue = Mathf.RoundToInt(killCount);
            }

            if (killCountValue <= 0)
            {
                save.SetVariable(killCountVariableName, 1);
                return 1 * ExperienceAcquiringPostProcess;
            }

            double experience = 1 - (0.090909 * Math.Log(Neperian * killCountValue * killCountValue * killCountValue)).Clamp01();
            return (int)(experience * ExperienceAcquiringPostProcess * script.ExpMultiplier);
        }

        public static double GetExperiencePercentage([NotNull] IReadonlyCharacterStats character) => GetExperiencePercentage(character.TotalExperience);

        public static double GetExperiencePercentage(int totalExperience)
        {
            int level = totalExperience / ExperienceNeededForLevelUp;
            double experience = totalExperience - (level * ExperienceNeededForLevelUp);
            return experience / ExperienceNeededForLevelUp;
        }

        public static bool IsLevelUp(int startEthelExp, int ethelExp)
        {
            int startLevel = startEthelExp / ExperienceNeededForLevelUp;
            int endLevel = ethelExp / ExperienceNeededForLevelUp;
            return startLevel < endLevel;
        }
    }
}