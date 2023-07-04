using System.Text;
using Core.Combat.Scripts;
using Core.Combat.Scripts.Managers;
using Core.Combat.Scripts.WinningCondition;
using Main_Database.Combat;
using Save_Management;
using UnityEngine;
using Utils.Patterns;
using Utils.Extensions;
using static Utils.Patterns.Result<Core.Combat.Scripts.WinningCondition.IWinningCondition>;

namespace Core.Visual_Novel.Data.Chapter_1.Scenes.Midnight_Mayhem
{
    public record MidnightMayhemRecord(float Duration, CleanString CrabdraKey, int CrabdrasToSpawn, bool SpawnTimerStarted, bool WaitingSpawn, float TimeUntilNextSpawn, float LastUpdate)
        : WinningConditionRecord(ConditionType.MidnightMayhemSurvive)
    {
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
            if (Duration <= 0f)
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
        private const float SpawnDelay = 5f;

        private readonly CombatManager _combatManager;
        private readonly float _duration;
        private readonly CharacterScriptable _crabdraScript;
        
        private int _crabdrasToSpawn;
        private bool _spawnTimerStarted;
        private bool _waitingSpawn;
        private float _timeUntilNextSpawn;
        private float _lastUpdate;

        public MidnightMayhemWinningCondition(CombatManager combatManager, float duration, CharacterScriptable crabdraScript, int crabdrasToSpawn)
        {
            _crabdrasToSpawn = crabdrasToSpawn;
            _combatManager = combatManager;
            _duration = duration;
            _crabdraScript = crabdraScript;
        }

        public MidnightMayhemWinningCondition(CombatManager combatManager, float duration, CharacterScriptable crabdraScript, int crabdrasToSpawn, bool spawnTimerStarted, bool waitingSpawn, float timeUntilNextSpawn, float lastUpdate)
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

        public CombatStatus Tick()
        {
            CombatStatus status = this.DefaultTick(_combatManager, _duration);
            float deltaTime = _combatManager.ElapsedTime - _lastUpdate;
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
                case State.WaitingTimer when deltaTime > 0:
                    _timeUntilNextSpawn -= deltaTime;
                    if (_timeUntilNextSpawn > 0 || _crabdrasToSpawn <= 0 || _combatManager.Characters.RightSideCount >= 4)
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

        public WinningConditionRecord Serialize() => new MidnightMayhemRecord(_duration, _crabdraScript.Key, _crabdrasToSpawn, _spawnTimerStarted, _waitingSpawn, _timeUntilNextSpawn, _lastUpdate);

        public static Result<IWinningCondition> Parse(string data, CombatManager combatManager)
        {
            string[] split = data.Split(',');
            if (split.Length != 7)
                return Error($"Invalid split length: {split.Length}, data: {data}");
            
            if (split[0].ParseFloat().TrySome(out float duration) == false)
                return Error($"Failed to parse duration: {split[0]}, data: {data}");

            if (CharacterDatabase.GetCharacter(split[1]).TrySome(out CharacterScriptable script) == false)
                return Error($"Character script: {split[1]} not found in database, data: {data}");
            
            if (split[2].ParseInt().TrySome(out int crabdrasToSpawn) == false)
                return Error($"Failed to parse crabdras to spawn: {split[2]}, data: {data}");
            
            if (split[3].ParseBool().TrySome(out bool spawnTimerStarted) == false)
                return Error($"Failed to parse spawn timer started: {split[3]}, data: {data}");
            
            if (split[4].ParseBool().TrySome(out bool waitingSpawn) == false)
                return Error($"Failed to parse waiting spawn: {split[4]}, data: {data}");
            
            if (split[5].ParseFloat().TrySome(out float timeUntilNextSpawn) == false)
                return Error($"Failed to parse time until next spawn: {split[5]}, data: {data}");
            
            if (split[6].ParseFloat().TrySome(out float lastUpdate) == false)
                return Error($"Failed to parse last update: {split[6]}, data: {data}");

            MidnightMayhemWinningCondition condition = new(combatManager, duration, script, crabdrasToSpawn) 
            {
                _spawnTimerStarted = spawnTimerStarted,
                _waitingSpawn = waitingSpawn,
                _timeUntilNextSpawn = timeUntilNextSpawn,
                _lastUpdate = lastUpdate
            };
            
            return Ok(condition);
        }
        
        public string DisplayName => this.DefaultDisplayName(_duration);
        public float GetTimeToDisplay() => this.DefaultTimeToDisplay(_combatManager, _duration);
        
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