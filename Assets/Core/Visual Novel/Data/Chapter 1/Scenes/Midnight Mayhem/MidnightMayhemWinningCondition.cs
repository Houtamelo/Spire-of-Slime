using System.Text;
using Core.Combat.Scripts;
using Core.Combat.Scripts.Managers;
using Core.Combat.Scripts.WinningCondition;
using Core.Main_Database.Combat;
using Core.Save_Management.SaveObjects;
using Core.Utils.Extensions;
using Core.Utils.Math;
using Core.Utils.Patterns;
using JetBrains.Annotations;
using UnityEngine;

namespace Core.Visual_Novel.Data.Chapter_1.Scenes.Midnight_Mayhem
{
    public record MidnightMayhemRecord(TSpan Duration, CleanString CrabdraKey, int CrabdrasToSpawn, bool SpawnTimerStarted, bool WaitingSpawn, TSpan TimeUntilNextSpawn, TSpan LastUpdate)
        : WinningConditionRecord(ConditionType.MidnightMayhemSurvive)
    {
        [NotNull]
        public override IWinningCondition Deserialize(CombatManager combatManager)
        {
            Option<CharacterScriptable> crabdraScript = CharacterDatabase.GetCharacter(CrabdraKey);
            if (crabdraScript.IsNone)
            {
                Debug.LogWarning($"Failed to find character with key {CrabdraKey}, returning defeatAll condition");
                return new DefeatAll(combatManager);
            }
            
            return new MidnightMayhemWinningCondition(combatManager, Duration, crabdraScript.Value, CrabdrasToSpawn, SpawnTimerStarted, WaitingSpawn, TimeUntilNextSpawn, LastUpdate);
        }

        public override bool IsDataValid(StringBuilder errors)
        {
            if (Duration.Ticks <= 0)
            {
                errors.AppendLine("Invalid ", nameof(MidnightMayhemRecord), ". Duration must be greater than 0.");
                return false;
            }

            if (CharacterDatabase.GetCharacter(CrabdraKey).IsNone)
            {
                errors.AppendLine("Invalid ", nameof(MidnightMayhemRecord), ". Character: ", CrabdraKey.ToString(), " does not exist in database.");
                return false;
            }
            
            return true;
        }
    }
    
    public class MidnightMayhemWinningCondition : ISurviveDuration
    {
        private static readonly TSpan SpawnDelay = TSpan.FromSeconds(5.0);

        private readonly CombatManager _combatManager;
        private readonly TSpan _duration;
        private readonly CharacterScriptable _crabdraScript;
        
        private int _crabdrasToSpawn;
        private bool _spawnTimerStarted;
        private bool _waitingSpawn;
        private TSpan _timeUntilNextSpawn;
        private TSpan _lastUpdate;

        public MidnightMayhemWinningCondition(CombatManager combatManager, TSpan duration, CharacterScriptable crabdraScript, int crabdrasToSpawn)
        {
            _crabdrasToSpawn = crabdrasToSpawn;
            _combatManager = combatManager;
            _duration = duration;
            _crabdraScript = crabdraScript;
        }

        public MidnightMayhemWinningCondition(CombatManager combatManager, TSpan duration, CharacterScriptable crabdraScript, int crabdrasToSpawn, bool spawnTimerStarted, bool waitingSpawn, TSpan timeUntilNextSpawn, TSpan lastUpdate)
        {
            _crabdrasToSpawn = crabdrasToSpawn;
            _combatManager = combatManager;
            _duration = duration;
            _crabdraScript = crabdraScript;
            _spawnTimerStarted = spawnTimerStarted;
            _waitingSpawn = waitingSpawn;
            _timeUntilNextSpawn = timeUntilNextSpawn;
            _lastUpdate = lastUpdate;
        }

        public CombatStatus Evaluate()
        {
            CombatStatus status = this.DefaultTick(_combatManager, _duration);
            TSpan deltaTime = _combatManager.ElapsedTime - _lastUpdate;
            _lastUpdate = _combatManager.ElapsedTime;
            if (status is CombatStatus.RightSideWon || (status is CombatStatus.LeftSideWon && _crabdrasToSpawn <= 0))
                return status;

            State state = EvaluateState();
            switch (state)
            {
                case State.WaitingSpace when _combatManager.Characters.RightSideCount < 4:
                    _timeUntilNextSpawn = SpawnDelay;
                    _spawnTimerStarted = true;
                    break;
                case State.WaitingTimer when deltaTime.Ticks > 0:
                    _timeUntilNextSpawn -= deltaTime;
                    if (_timeUntilNextSpawn.Ticks > 0 || _crabdrasToSpawn <= 0 || _combatManager.Characters.RightSideCount >= 4)
                        break;

                    Option<Promise<int>> createdCount = _combatManager.Characters.CreateOutsideSkill(_crabdraScript, isLeftSide: false, position: Option<int>.None, Option<int>.Some(_crabdrasToSpawn));
                    if (createdCount.TrySome(out Promise<int> promise) == false)
                        break;

                    _spawnTimerStarted = false;
                    _waitingSpawn = true;
                    promise.OnResolve(count =>
                    {
                        _crabdrasToSpawn -= count;
                        _waitingSpawn = false;
                    });

                    break;
            }
            
            return CombatStatus.InProgress;
        }

        [NotNull]
        public WinningConditionRecord Serialize() => new MidnightMayhemRecord(_duration, _crabdraScript.Key, _crabdrasToSpawn, _spawnTimerStarted, _waitingSpawn, _timeUntilNextSpawn, _lastUpdate);

        [NotNull]
        public string DisplayName => this.DefaultDisplayName(_duration);
        public TSpan GetTimeToDisplay() => this.DefaultTimeToDisplay(_combatManager, _duration);
        
        private State EvaluateState()
        {
            if (_waitingSpawn)
                return State.WaitingSpawn;
            
            if (_spawnTimerStarted)
                return State.WaitingTimer;
            
            if (_crabdrasToSpawn <= 0)
                return State.LimitReached;
            
            return State.WaitingSpace;
        }

        private enum State
        {
            WaitingSpace,
            WaitingTimer,
            WaitingSpawn,
            LimitReached
        }
    }
}