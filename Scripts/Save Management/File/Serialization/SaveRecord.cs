using System;
using System.Collections.Generic;
using System.Text;
using Core.World_Map.Scripts;
using Save_Management.Stats;
using Sirenix.Serialization;
using Utils.Extensions;

namespace Save_Management.Serialization
{
    public record SaveRecord(string Name, DateTime Date, Dictionary<CleanString, bool> Booleans, Dictionary<CleanString, float> Floats, Dictionary<CleanString, CleanString> Strings, LocationEnum Location,
                             LocationEnum[] LocationsUnlocked, CharacterStats[] Characters, float NemaExhaustion, bool IsNemaClearingMist, Random GeneralRandomizer, CleanString[] CombatOrder, SavePoint.Base SavePoint)
    {
        public bool IsDirty { get; set; }
        #if UNITY_EDITOR
        [OdinSerialize]
        public SavePoint.Base SavePoint { get; init; } = SavePoint;
        #endif

        public bool IsDataValid(StringBuilder errors)
        {
            if (string.IsNullOrEmpty(Name))
            {
                errors.AppendLine("Invalid ", nameof(SaveRecord), ". ", nameof(Name), " is null or empty");
                return false;
            }
            
            if (Booleans == null)
            {
                errors.AppendLine("Invalid ", nameof(SaveRecord), ". ", nameof(Booleans), " is null");
                return false;
            }
            
            if (Floats == null)
            {
                errors.AppendLine("Invalid ", nameof(SaveRecord), ". ", nameof(Floats), " is null");
                return false;
            }
            
            if (Strings == null)
            {
                errors.AppendLine("Invalid ", nameof(SaveRecord), ". ", nameof(Strings), " is null");
                return false;
            }
            
            if (LocationsUnlocked == null)
            {
                errors.AppendLine("Invalid ", nameof(SaveRecord), ". ", nameof(LocationsUnlocked), " is null");
                return false;
            }
            
            if (Characters == null)
            {
                errors.AppendLine("Invalid ", nameof(SaveRecord), ". ", nameof(Characters), " is null");
                return false;
            }

            for (int i = 0; i < Characters.Length; i++)
            {
                CharacterStats character = Characters[i];
                if (character == null)
                {
                    errors.AppendLine("Invalid ", nameof(SaveRecord), ". ", nameof(Characters), " at index ", i.ToString(), " is null");
                    return false;
                }

                if (character.IsDataValid(errors) == false)
                    return false;
            }
            
            if (GeneralRandomizer == null)
            {
                errors.AppendLine("Invalid ", nameof(SaveRecord), ". ", nameof(GeneralRandomizer), " is null");
                return false;
            }
            
            if (CombatOrder == null)
            {
                errors.AppendLine("Invalid ", nameof(SaveRecord), ". ", nameof(CombatOrder), " is null");
                return false;
            }
            
            if (SavePoint == null)
            {
                errors.AppendLine("Invalid ", nameof(SaveRecord), ". ", nameof(SavePoint), " is null");
                return false;
            }
            
            if (SavePoint.IsDataValid(errors) == false)
                return false;
            
            return true;
        }
    }
}