using System;
using System.Text;
using Core.Combat.Scripts;
using Core.Main_Database.Combat;
using Core.Save_Management.SaveObjects;
using Core.Utils.Extensions;
using Core.Utils.Patterns;
using UnityEngine;
using Utils.Patterns;
using Save = Core.Save_Management.SaveObjects.Save;

namespace Core.Save_Management
{
    public static class ExperienceCalculator
    {
        private static readonly StringBuilder Builder = new();

        private static readonly CleanString KillCountVariableStart = "killcount_";
        private const float ExperienceAcquiringPostProcess = 0.25f;
        private const float Neperian = (float) Math.E;
        public const float ExperienceNeededForLevelUp = 1f;

        /// <summary> NOT PURE! This increments the enemy's kill counter </summary>
        public static float GetExperienceFromEnemy(CleanString characterKey, Save save)
        {
            Option<CharacterScriptable> character = CharacterDatabase.GetCharacter(characterKey);
            if (character.AssertSome(out CharacterScriptable script) == false)
                return 0f;
            
            CleanString killCountVariableName = Builder.Override(KillCountVariableStart.ToString(), characterKey.ToString()).ToString();
            float killCount = save.GetVariable<float>(killCountVariableName);
            int killCountValue;
            if (killCount == 0)
            {
                killCountValue = 1;
                save.SetVariable(killCountVariableName, killCountValue);
            }
            else
                killCountValue = Mathf.RoundToInt(killCount);

            if (killCountValue <= 0)
            {
                save.SetVariable(killCountVariableName, 1f);
                return 1f * ExperienceAcquiringPostProcess;
            }

            float experience = 1 - 0.090909f * Mathf.Log(Neperian * killCountValue * killCountValue * killCountValue);
            experience = Mathf.Clamp(experience, 0f, 1f);
            return experience * ExperienceAcquiringPostProcess * script.ExpMultiplier;
        }

        public static float GetExperiencePercentage(IReadonlyCharacterStats character) => GetExperiencePercentage(character.Experience);

        public static float GetExperiencePercentage(float totalExperience)
        {
            int level = Mathf.FloorToInt(totalExperience / ExperienceNeededForLevelUp);
            float experience = totalExperience - level * ExperienceNeededForLevelUp;
            return experience / ExperienceNeededForLevelUp;
        }

        public static bool IsLevelUp(float startEthelExp, float ethelExp)
        {
            int startLevel = Mathf.FloorToInt(startEthelExp / ExperienceNeededForLevelUp);
            int endLevel = Mathf.FloorToInt(ethelExp / ExperienceNeededForLevelUp);
            return startLevel < endLevel;
        }
    }
}