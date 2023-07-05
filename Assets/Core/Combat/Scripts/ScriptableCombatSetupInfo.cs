using System.Collections.Generic;
using System.Linq;
using Core.Audio.Scripts.MusicControllers;
using Core.Combat.Scripts.BackgroundGeneration;
using Core.Combat.Scripts.Interfaces;
using Core.Combat.Scripts.WinningCondition;
using Core.Main_Characters.Nema.Combat;
using Core.Main_Database.Combat;
using Core.Save_Management.SaveObjects;
using Data.Main_Characters.Ethel;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;
using Save = Core.Save_Management.SaveObjects.Save;

namespace Core.Combat.Scripts
{
    [CreateAssetMenu(fileName = "combat_", menuName = "Database/Combat/SetupInfo")]
    public class ScriptableCombatSetupInfo : ScriptableObject
    {
        [SerializeField]
        private bool useCustomEthelScript;
        
        [SerializeField, Required, ShowIf(nameof(useCustomEthelScript))]
        private SerializedCharacterInfo serializedEthelScript;
        
        [SerializeField]
        private bool useCustomNemaScript;
        
        [SerializeField, Required, ShowIf(nameof(useCustomNemaScript))] 
        private SerializedCharacterInfo serializedNemaScript;
        
        [FormerlySerializedAs("serializedWinningCondition"),SerializeField]
        private WinningConditionGenerator winningConditionGenerator;
        public WinningConditionGenerator WinningConditionGenerator => winningConditionGenerator;

        [field: SerializeField]
        public SerializedCharacterInfo[] Enemies { get; private set; }
        
        [field: SerializeField] 
        public CombatBackground BackgroundPrefab { get; private set; }
        
        [field: SerializeField]
        public bool MistExists { get; private set; }

        [SerializeField]
        private bool allowLust;

        [SerializeField]
        private GeneralPaddingSettings paddingSettings = GeneralPaddingSettings.Default;

        [SerializeField, Required]
        private MusicController musicController;
        public MusicController MusicController => musicController;

        private (ICharacterScript script, CombatSetupInfo.RecoveryInfo recovery, float startExp, bool bindToSave) GetEthel()
        {
            Save save = Save.Current;
            float startExp = save != null ? save.EthelStats.Experience : 0f;

            if (useCustomEthelScript)
            {
                (ICharacterScript script, CombatSetupInfo.RecoveryInfo info) serializedStruct = serializedEthelScript.ToStruct();
                return (serializedStruct.script, serializedStruct.info, startExp, bindToSave: true);
            }

            return (save != null ? save.EthelScript : CharacterDatabase.DefaultEthel, CombatSetupInfo.RecoveryInfo.Default, startExp, bindToSave: true);
        }

        private (ICharacterScript script, CombatSetupInfo.RecoveryInfo recovery, float startExp, bool bindToSave) GetNema()
        {
            Save save = Save.Current;
            float startExp = save != null ? save.NemaStats.Experience : 0f;

            if (useCustomNemaScript)
            {
                (ICharacterScript script, CombatSetupInfo.RecoveryInfo info) serializedStruct = serializedNemaScript.ToStruct();
                return (serializedStruct.script, serializedStruct.info, startExp, bindToSave: true);
            }

            return (save != null ? save.NemaScript : CharacterDatabase.DefaultNema, CombatSetupInfo.RecoveryInfo.Default, startExp, bindToSave: true);
        }

        public string Key => name;

        public CombatSetupInfo ToStruct()
        {
            Save save = Save.Current;
            if (save == null)
                return new CombatSetupInfo(allies: new[] { GetEthel(), GetNema() },
                                           Enemies.Select(enemy => enemy.ToStruct()).Select(enemy => ((ICharacterScript)enemy.script, enemy.info)).ToArray(), MistExists, allowLust, paddingSettings);

            IReadOnlyList<(IReadonlyCharacterStats stats, bool bindToSave)> alliesOrder = save.GetCombatOrderAsStats();
            List<(ICharacterScript, CombatSetupInfo.RecoveryInfo, float expAtStart, bool bindToSave)> allies = new(alliesOrder.Count);
            foreach ((IReadonlyCharacterStats stats, bool bindToSave) in alliesOrder)
            {
                ICharacterScript script;
                CombatSetupInfo.RecoveryInfo recovery;
                if (useCustomEthelScript && stats.Key == Ethel.GlobalKey)
                {
                    script = serializedEthelScript.SerializedCharacter;
                    recovery = serializedEthelScript.RecoveryInfo;
                }
                else if (useCustomNemaScript && stats.Key == Nema.GlobalKey)
                {
                    script = serializedNemaScript.SerializedCharacter;
                    recovery = serializedNemaScript.RecoveryInfo;
                }
                else
                {
                    script = stats.GetScript();
                    recovery = CombatSetupInfo.RecoveryInfo.Default;
                }
                
                allies.Add((script, recovery, stats.Experience, bindToSave));
            }
            
            return new CombatSetupInfo(allies.ToArray(), Enemies.Select(enemy => enemy.ToStruct()).Select(enemy => ((ICharacterScript)enemy.script, enemy.info)).ToArray(), MistExists, allowLust, paddingSettings);
        }

        [System.Serializable]
        public struct SerializedCharacterInfo
        {
            [SerializeField]
            private CharacterScriptable serializedCharacter;
            public CharacterScriptable SerializedCharacter => serializedCharacter;

            [SerializeField]
            private bool useCustomRecovery;
            
            [SerializeField, ShowIf(nameof(useCustomRecovery))]
            private CombatSetupInfo.RecoveryInfo recoveryInfo;
            public CombatSetupInfo.RecoveryInfo RecoveryInfo => useCustomRecovery ? recoveryInfo : CombatSetupInfo.RecoveryInfo.Default;
        
            public (CharacterScriptable script, CombatSetupInfo.RecoveryInfo info) ToStruct() 
                => useCustomRecovery ? (serializedCharacter, recoveryInfo) : (serializedCharacter, CombatSetupInfo.RecoveryInfo.Default);

            public static implicit operator (CharacterScriptable, CombatSetupInfo.RecoveryInfo)(SerializedCharacterInfo info) => info.ToStruct();
        }
    }
}