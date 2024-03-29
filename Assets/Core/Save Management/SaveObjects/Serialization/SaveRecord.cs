﻿using System;
using System.Collections.Generic;
using System.Text;
using Core.Utils.Extensions;
using Core.World_Map.Scripts;
using Sirenix.Serialization;

namespace Core.Save_Management.SaveObjects
{
    public record SaveRecord(string Name, DateTime Date, Dictionary<CleanString, bool> Booleans, Dictionary<CleanString, int> Ints, Dictionary<CleanString, CleanString> Strings, LocationEnum Location,
                             LocationEnum[] LocationsUnlocked, CharacterStats[] Characters, int NemaExhaustion, bool IsNemaClearingMist, Random GeneralRandomizer, CleanString[] CombatOrder, SavePoint.Base SavePoint)
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
            
            if (Ints == null)
            {
                errors.AppendLine("Invalid ", nameof(SaveRecord), ". ", nameof(Ints), " is null");
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