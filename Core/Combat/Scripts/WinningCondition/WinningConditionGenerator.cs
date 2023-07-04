using System;
using Core.Visual_Novel.Data.Chapter_1.Scenes.Midnight_Mayhem;
using Sirenix.OdinInspector;
using UnityEngine;
using CombatManager = Core.Combat.Scripts.Managers.CombatManager;

namespace Core.Combat.Scripts.WinningCondition
{
    [Serializable]
    public class WinningConditionGenerator
    {
        public static readonly WinningConditionGenerator Default = new() { conditionType = ConditionType.DefeatAll };

        [SerializeField]
        private ConditionType conditionType = ConditionType.DefeatAll;

        [SerializeField, ShowIf(nameof(ShowDuration))]
        private float duration;

        [SerializeField, ShowIf(nameof(IsMidnightMayhem))]
        private CharacterScriptable characterToSpawn;

        [SerializeField, ShowIf(nameof(IsMidnightMayhem))]
        private int spawnCount;

        public IWinningCondition GenerateCondition(CombatManager combatManager)
        {
            return conditionType switch
            {
                ConditionType.DefeatAll             => new DefeatAll(combatManager),
                ConditionType.SurviveDuration       => new SurviveDuration(combatManager, duration),
                ConditionType.MidnightMayhemSurvive => new MidnightMayhemWinningCondition(combatManager, duration, characterToSpawn, spawnCount),
                _                                   => throw new ArgumentOutOfRangeException($"Unknown winning condition type: {conditionType}")
            };
        }

        private bool ShowDuration => conditionType is ConditionType.SurviveDuration or ConditionType.MidnightMayhemSurvive;
        private bool IsMidnightMayhem => conditionType is ConditionType.MidnightMayhemSurvive;
    }
}