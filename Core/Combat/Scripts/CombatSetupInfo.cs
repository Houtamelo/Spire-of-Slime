using System;
using System.Text;
using Core.Combat.Scripts.Interfaces;
using Main_Database.Combat;
using Save_Management;
using Sirenix.OdinInspector;
using UnityEngine;
using Utils.Extensions;
using Utils.Patterns;

namespace Core.Combat.Scripts
{
    public readonly struct CombatSetupInfo
    {
        public readonly (ICharacterScript script, RecoveryInfo recoveryInfo, float expAtStart, bool bindToSave)[] Allies;
        public readonly (ICharacterScript script, RecoveryInfo recoveryInfo)[] Enemies;
        public readonly bool MistExists;
        public readonly bool AllowLust;
        public readonly GeneralPaddingSettings PaddingSettings;

        public CombatSetupInfo((ICharacterScript, RecoveryInfo, float expAtStart, bool bindToSave)[] allies, (ICharacterScript, RecoveryInfo)[] enemies, bool mistExists, bool allowLust, GeneralPaddingSettings paddingSettings)
        {
            Allies = allies;
            Enemies = enemies;
            MistExists = mistExists;
            AllowLust = allowLust;
            PaddingSettings = paddingSettings;
        }

        public Record GetRecord()
        {
            (CleanString key, RecoveryInfo recoveryInfo, float expAtStart, bool bindToSave)[] allies = new (CleanString, RecoveryInfo, float expAtStart, bool bindToSave)[Allies.Length];
            for (int i = 0; i < Allies.Length; i++)
                allies[i] = (Allies[i].script.Key, Allies[i].recoveryInfo, Allies[i].expAtStart, Allies[i].bindToSave);
            
            (CleanString key, RecoveryInfo recoveryInfo)[] enemies = new (CleanString, RecoveryInfo)[Enemies.Length];
            for (int i = 0; i < Enemies.Length; i++)
                enemies[i] = (Enemies[i].script.Key, Enemies[i].recoveryInfo);
            
            return new Record(allies, enemies, MistExists, AllowLust, PaddingSettings);
        }

        public static Option<CombatSetupInfo> FromRecord(Record record)
        {
            (ICharacterScript script, RecoveryInfo recoveryInfo, float expAtStart, bool bindToSave)[] allies = new (ICharacterScript, RecoveryInfo, float expAtStart, bool bindToSave)[record.Allies.Length];
            for (int i = 0; i < allies.Length; i++)
            {
                (CleanString key, RecoveryInfo recoveryInfo, float expAtStart, bool bindToSave) = record.Allies[i];
                Option<CharacterScriptable> characterScript = CharacterDatabase.GetCharacter(key);
                if (characterScript.IsNone)
                    return Option.None;
                
                allies[i] = (characterScript.Value, recoveryInfo, expAtStart, bindToSave);
            }
            
            (ICharacterScript script, RecoveryInfo recoveryInfo)[] enemies = new (ICharacterScript, RecoveryInfo)[record.Enemies.Length];
            for (int i = 0; i < enemies.Length; i++)
            {
                (CleanString key, RecoveryInfo recoveryInfo) = record.Enemies[i];
                Option<CharacterScriptable> characterScript = CharacterDatabase.GetCharacter(key);
                if (characterScript.IsNone)
                    return Option.None;
                
                enemies[i] = (characterScript.Value, recoveryInfo);
            }
            
            return new CombatSetupInfo(allies, enemies, record.MistExists, record.AllowLust, record.PaddingSettings);
        }

        [Serializable]
        public struct RecoveryInfo
        {
            [SerializeField] 
            private float baseValue;
            
            [SerializeField] 
            private bool randomize;
            
            [SerializeField, ShowIf(nameof(randomize))]
            private float amplitude;

            public RecoveryInfo(float baseValue, float amplitude, bool randomize)
            {
                this.baseValue = baseValue;
                this.amplitude = amplitude;
                this.randomize = randomize;
            }
            
            public float GenerateValue() => randomize ? baseValue + UnityEngine.Random.Range(-amplitude, amplitude) : baseValue;
            public static RecoveryInfo Default => new(1, 0.5f, true);
        }

        public record Record((CleanString key, RecoveryInfo recoveryInfo, float expAtStart, bool bindToSave)[] Allies, (CleanString key, RecoveryInfo recoveryInfo)[] Enemies, bool MistExists, bool AllowLust, GeneralPaddingSettings PaddingSettings)
        {
            public bool IsDataValid(StringBuilder errors)
            {
                if (Allies == null)
                {
                    errors.AppendLine("Invalid ", nameof(CombatSetupInfo.Record), " data. ", nameof(Allies), " is null.");
                    return false;
                }

                for (int i = 0; i < Allies.Length; i++)
                {
                    CleanString key = Allies[i].key;
                    if (CharacterDatabase.GetCharacter(key).IsNone)
                    {
                        errors.AppendLine("Invalid ", nameof(CombatSetupInfo.Record), " data. Ally at index: ", i.ToString() ," with key: ", key.ToString(), " does not exist in database.");
                        return false;
                    }
                }
                
                if (Enemies == null)
                {
                    errors.AppendLine("Invalid ", nameof(CombatSetupInfo.Record), " data. ", nameof(Enemies), " is null.");
                    return false;
                }

                for (int i = 0; i < Enemies.Length; i++)
                {
                    CleanString key = Enemies[i].key;
                    if (CharacterDatabase.GetCharacter(key).IsNone)
                    {
                        errors.AppendLine("Invalid ", nameof(CombatSetupInfo.Record), " data. Enemy at index: ", i.ToString() , " with key: ", key.ToString(), " does not exist in database.");
                        return false;
                    }
                }

                return true;
            }
        }
    }
}