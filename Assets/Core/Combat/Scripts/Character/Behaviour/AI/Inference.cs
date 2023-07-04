/*using System;
using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using Combat.AI.Training;
using Combat.Effects;
using Combat.Effects.BaseTypes;
using Combat.Effects.Types;
using Combat.Skills;
using NetFabric.Hyperlinq;
using Unity.Barracuda;
using UnityEngine;
using Utility;

namespace Combat.Behaviour
{
    public sealed partial class Character
    {
        public const float MinDamage = 0f;
        public const float MaxDamage = 200f;
        public const float StaminaBaseConstant = 200f;
        public const float MinCrit = -1f;
        public const float MaxCrit = 1f;
        public const float MinResilience = -1f; // composure as well
        public const float MaxResilience = 1f; // composure as well
        public const float MinDodge = -1f;
        public const float MaxDodge = 1f;
        public const float MinSpeed = 0f;
        public const float MaxSpeed = 4f;
        public const float MinResistance = -2f;
        public const float MaxResistance = 2f;
        public const float TimeUpperBound = 10f;
        public const int OrgasmMax = 8;

        public const float BasicReward = 0.01f;
        private const int EnemyObservationCount = 26 + TargetObservationCount * 4;
        private const int AllyObservationCount = 29 + TargetObservationCount * 4;
        private const int SelfObservationCount = 25;
        public const int TotalObservationCount = EnemyObservationCount * 4 + AllyObservationCount * 4 + 25;
        private const int TargetObservationCount = 47;
        
        [SerializeField] private NNModel modelAsset;
        [NonSerialized] private Model _model;

        private readonly float[] _observations = new float[TensorConstants.ObservationsInputSize];
        private readonly float[] _cachedActionMask = new float[TensorConstants.OutputSize];
        //Last computed sensor count = 340 + 328 + 13 = 681


        private readonly (Character character, bool[] positions)[] _alliesSortedByPosition = 
        {
            (null, new bool[4]),
            (null, new bool[4]),
            (null, new bool[4]),
            (null, new bool[4])
        };

        private readonly (Character character, bool[] positions)[] _enemiesSortedByPosition =
        {
            (null, new bool[4]),
            (null, new bool[4]),
            (null, new bool[4]),
            (null, new bool[4]),
        };

        private readonly Dictionary<CombatStat, (float, float)> _reusableBuffsAndDebuffs = new()
        {
            [CombatStat.Accuracy] = (0f, 0f),
            [CombatStat.Composure] = (0f, 0f),
            [CombatStat.CriticalChance] = (0f, 0f),
            [CombatStat.Dodge] = (0f, 0f),
            [CombatStat.Resilience] = (0f, 0f),
            [CombatStat.Speed] = (0f, 0f),
            [CombatStat.DamageMultiplier] = (0f, 0f),
            [CombatStat.DebuffResistance] = (0f, 0f),
            [CombatStat.MoveResistance] = (0f, 0f),
            [CombatStat.PoisonResistance] = (0f, 0f),
            [CombatStat.StunSpeed] = (0f, 0f),
            [CombatStat.ArousalResistance] = (0f, 0f),
        };
        
        private void ClearReusableBuffsAndDebuffs()
        {
            _reusableBuffsAndDebuffs[CombatStat.Accuracy] = (0f, 0f);
            _reusableBuffsAndDebuffs[CombatStat.Composure] = (0f, 0f);
            _reusableBuffsAndDebuffs[CombatStat.CriticalChance] = (0f, 0f);
            _reusableBuffsAndDebuffs[CombatStat.Dodge] = (0f, 0f);
            _reusableBuffsAndDebuffs[CombatStat.Resilience] = (0f, 0f);
            _reusableBuffsAndDebuffs[CombatStat.Speed] = (0f, 0f);
            _reusableBuffsAndDebuffs[CombatStat.DamageMultiplier] = (0f, 0f);
            _reusableBuffsAndDebuffs[CombatStat.DebuffResistance] = (0f, 0f);
            _reusableBuffsAndDebuffs[CombatStat.MoveResistance] = (0f, 0f);
            _reusableBuffsAndDebuffs[CombatStat.PoisonResistance] = (0f, 0f);
            _reusableBuffsAndDebuffs[CombatStat.StunSpeed] = (0f, 0f);
            _reusableBuffsAndDebuffs[CombatStat.ArousalResistance] = (0f, 0f);
        }
        
        [field: SerializeField] public bool WaitingForDecision { get; private set; }
        
        private void Start()
        {
            //_model = ModelLoader.Load(modelAsset);
        }
        
        /*public void RequestDecision()
        {
            if (WaitingForDecision || !IsSetup || IsDefeated)
                return;
            
            WaitingForDecision = true;
            float[] observations = CollectObservations();
            float[] actionMask = _cachedActionMask;

            Promise<ModelOutput> promise = new Promise<ModelOutput>();
            Coroutine coroutine = ModelRunner.GetOutputAsync(_model, observations, actionMask, promise);
            StartCoroutine(DecisionWaitingRoutine(coroutine, promise));
        }#1#
        
        public void RequestDecision()
        {
            if (WaitingForDecision || !IsSetup || IsDefeated)
                return;
            
            WaitingForDecision = true;
            float[] observations = CollectObservations();
            float[] actionMask = _cachedActionMask;

            Promise<DeterministicModelOutput> promise = new Promise<DeterministicModelOutput>();
            Coroutine coroutine = ModelRunner.GetOutputAsync(_model, observations, actionMask, promise);
            StartCoroutine(DecisionWaitingRoutine(coroutine, promise));
        }
        
        private IEnumerator DecisionWaitingRoutine(Coroutine coroutine, Promise<DeterministicModelOutput> promise)
        {
            yield return coroutine;
            
            OnActionReceived(promise.Option);
        }

        private IEnumerator DecisionWaitingRoutine(Coroutine coroutine, Promise<ModelOutput> promise)
        {
            yield return coroutine;
            
            OnActionReceived(promise.Option);

            if (promise.Option.IsSome)
                promise.Option.Value.Dispose();
        }

        public void OnActionReceived(Utility.Option<DeterministicModelOutput> option)
        {
            if (!WaitingForDecision)
                return;
            
            WaitingForDecision = false;
            if (!IsSetup || IsDefeated || !IsIdle)
                return;
            
            if (!option.IsSome)
            {
                Debug.LogWarning("Invalid result, using heuristic");
                Heuristic();
                return;
            }

            int weightedIndex = option.Value._action;

            if (weightedIndex < 16)
            {
                int targetIndex = weightedIndex % 4;
                int skillIndex = weightedIndex / 4;
            
                ISkill skill = Script.Skills[skillIndex];
                Character target = _alliesSortedByPosition[targetIndex].character;
            
                PlanSkill(skill, target);
                Debug.Log($"Planning skill {skill.SkillName} on {target.Script.CharacterName}");
            }
            else
            {
                weightedIndex -= 16;
                
                int targetIndex = weightedIndex % 4;
                int skillIndex = weightedIndex / 4;
                
                ISkill skill = Script.Skills[skillIndex];
                Character target = _enemiesSortedByPosition[targetIndex].character;
                
                PlanSkill(skill, target);
                Debug.Log($"Planning skill {skill.SkillName} on {target.Script.CharacterName}");
            }
            
            combatManager.PauseTime();
        }


        public void OnActionReceived(Utility.Option<ModelOutput> option)
        {
            if (!WaitingForDecision)
                return;
            
            WaitingForDecision = false;
            if (!IsSetup || IsDefeated || !IsIdle)
                return;
            
            if (!option.IsSome)
            {
                Debug.LogWarning("Invalid result, using heuristic");
                Heuristic();
                return;
            }

            ModelOutput output = option.Value;
            List<(string text, float probability)> actionList = new List<(string text, float probability)>();
            
            for (int i = 0; i < 32; i++)
            {
                float probability = output[i];

                int index = i;
                ISkill skill;
                Character target;
                if (index < 16)
                {
                    int targetIndex = index % 4;
                    int skillIndex = index / 4;
            
                    skill = Script.Skills[skillIndex];
                    target = _alliesSortedByPosition[targetIndex].character;
            
                    PlanSkill(skill, target);
                }
                else
                {
                    index -= 16;
                
                    int targetIndex = index % 4;
                    int skillIndex = index / 4;
                
                    skill = Script.Skills[skillIndex];
                    target = _enemiesSortedByPosition[targetIndex].character;
                
                    PlanSkill(skill, target);
                }

                if (!skill.IsTargetPositionValid(this, target))
                    continue;

                actionList.Add(($"{skill.SkillName}=>{target.Script.CharacterName}", probability));
            }
            
            ProbabilityDisplay.Instance.DisplayProbabilities(actionList, Bounds.center);
            combatManager.PauseTime();
            
            int weightedIndex = output.GetRandomWeightedIndex(Extensions.Random);

            if (weightedIndex < 16)
            {
                int targetIndex = weightedIndex % 4;
                int skillIndex = weightedIndex / 4;
            
                ISkill skill = Script.Skills[skillIndex];
                Character target = _alliesSortedByPosition[targetIndex].character;
            
                PlanSkill(skill, target);
            }
            else
            {
                weightedIndex -= 16;
                
                int targetIndex = weightedIndex % 4;
                int skillIndex = weightedIndex / 4;
                
                ISkill skill = Script.Skills[skillIndex];
                Character target = _enemiesSortedByPosition[targetIndex].character;
                
                PlanSkill(skill, target);
            }
        }

        public float[] CollectObservations()
        {
            if (!IsSetup || IsDefeated)
            {
                AddAllEmptyObservations();
                return _observations;
            }

            Character[] allies;
            Character[] enemies;
            if (IsLeftSide)
            {
                allies = combatManager.LeftCharacters;
                enemies = combatManager.RightCharacters;
            }
            else
            {
                allies = combatManager.RightCharacters;
                enemies = combatManager.LeftCharacters;
            }

            for (int i = 0; i < allies.Length; i++)
            {
                Character character = allies[i];
                bool[] positions = _alliesSortedByPosition[i].positions;
                Array.Clear(positions, 0, 4);

                using Lease<int> lease = character.ComputePosition();
                foreach (int position in lease) 
                    positions[position] = true;

                _alliesSortedByPosition[i] = (character, positions);
            }
            
            for (int i = 0; i < enemies.Length; i++)
            {
                Character character = enemies[i];
                bool[] positions = _enemiesSortedByPosition[i].positions;
                Array.Clear(positions, 0, 4);

                using Lease<int> lease = character.ComputePosition();
                foreach (int position in lease) 
                    positions[position] = true;

                _enemiesSortedByPosition[i] = (character, positions);
            }

            Array.Sort(_alliesSortedByPosition, Manager.CombatManager.IsNestedEnabledComparerInstance);
            Array.Sort(_enemiesSortedByPosition, Manager.CombatManager.IsNestedEnabledComparerInstance);
            
            using Lease<(ISkill, Lease<bool>)> castableSkills = ArrayPool<(ISkill, Lease<bool>)>.Shared.Lease(4);
            using Lease<int> selfPositionsLease = combatManager.ComputePosition(this);

            Array.Clear(_cachedActionMask, 0, 32);

            int maskCounter = 0;
            for (int index = 0; index < Script.Skills.Count && index < 4; index++)
            {
                ISkill skill = Script.Skills[index];
                Lease<bool> lease = ArrayPool<bool>.Shared.Lease(OrgasmMax);
                
                if (skill.CanCastFrom(selfPositionsLease))
                {
                    int counter = 0;
                    for (int i = 0; i < _alliesSortedByPosition.Length; i++, counter++, maskCounter++)
                    {
                        (Character character, bool[] positions) = _alliesSortedByPosition[i];
                        bool isValidTarget = skill.IsTargetPositionValid(this, character, positions, true);
                        _cachedActionMask[maskCounter] = isValidTarget ? 1f : 0f;
                        lease.Rented[counter] = isValidTarget;
                    }

                    for (int i = 0; i < _enemiesSortedByPosition.Length; i++, counter++, maskCounter++)
                    {
                        (Character character, bool[] positions) = _enemiesSortedByPosition[i];
                        bool isValidTarget = skill.IsTargetPositionValid(this, character, positions, true);
                        _cachedActionMask[maskCounter] = isValidTarget ? 1f : 0f;
                        lease.Rented[counter] = isValidTarget;
                    }

                    castableSkills.Rented[index] = (skill, lease);
                }
                else
                {
                    for (int i = 0; i < OrgasmMax; i++, maskCounter++)
                    {
                        _cachedActionMask[maskCounter] = 0f;
                        lease.Rented[i] = false;
                    }
                    
                    castableSkills.Rented[index] = (skill, lease);
                }
            }

            for (int i = Script.Skills.Count; i < 4; i++)
                castableSkills.Rented[i] = (null, null);
            
            int currentIndex = 0;

            AddSelfObservation(ref currentIndex);
            CollectAlyObservations(ref currentIndex, this, castableSkills, _alliesSortedByPosition);
            CollectEnemyObservations(ref currentIndex, this, castableSkills, _enemiesSortedByPosition);

            for (int index = 0; index < castableSkills.Length; index++)
            {
                (ISkill _, Lease<bool> lease) = castableSkills.Rented[index];
                lease?.Dispose();
                castableSkills.Rented[index] = default;
            }

            return _observations;
        }

        private void AddAllEmptyObservations()
        {
            Array.Clear(_observations, 0, _observations.Length);
        }

        //self observations = 25
        private void AddSelfObservation(ref int currentIndex)
        {
            float currentStamina = CurrentStamina.NormalizeClamped(0f, StaminaBaseConstant);
            AddObservation(ref currentIndex, this, currentStamina); // 1
            
            float maxStamina = MaxStamina.NormalizeClamped(0f, StaminaBaseConstant);
            AddObservation(ref currentIndex, this, maxStamina); // 2

            float resilience = GetResilience.NormalizeClamped(MinResilience, MaxResilience);
            AddObservation(ref currentIndex, this, resilience); // 3
            
            float dodge = GetDodge.NormalizeClamped(MinDodge, MaxDodge);
            AddObservation(ref currentIndex, this, dodge); // 4

            float estimatedFutureStamina = 0f;
            foreach (StatusInstance status in _statuses)
                if (status is Poison poison)
                    estimatedFutureStamina -= poison.CalculateTotalDamage();
                else if (status is OvertimeHeal overtimeHealInstance)
                    estimatedFutureStamina += overtimeHealInstance.CalculateTotalHeal();

            estimatedFutureStamina.NormalizeClamped(0f, StaminaBaseConstant);
            AddObservation(ref currentIndex, this, estimatedFutureStamina); // 5

            float critChance = GetCriticalChance.NormalizeClamped(MinCrit, MaxCrit);
            AddObservation(ref currentIndex, this, critChance); // 6

            float stunRecoverySpeed = GetStunRecoverySpeed.NormalizeClamped(MinSpeed, MaxSpeed);
            AddObservation(ref currentIndex, this, stunRecoverySpeed); // 7
            
            using (Lease<bool> positionsLease = combatManager.ComputePositionsAsBoolean(this))
            {
                for (int i = 0; i < 4; i++)
                    AddObservation(ref currentIndex, this, positionsLease.Rented[i]);
            }

            float moveResistance = GetMoveResistance.NormalizeClamped(MinResistance, MaxResistance);
            AddObservation(ref currentIndex, this, moveResistance); // 12

            float poisonResistance = GetPoisonResistance.NormalizeClamped(MinResistance, MaxResistance);
            AddObservation(ref currentIndex, this, poisonResistance); // 13
            
            (float lower, float upper) damage = GetDamage;
            float averageDamage = ((damage.lower + damage.upper) / 2f).NormalizeClamped(0f, StaminaBaseConstant);
            AddObservation(ref currentIndex, this, averageDamage); // 14

            float damageMultiplier = GetDamageMultiplier.NormalizeClamped(0f, 2f);
            AddObservation(ref currentIndex, this, damageMultiplier); // 15

            AddObservation(ref currentIndex, this, IsGirl); // 16
            
            if (IsGirl)
            {
                float currentLust = CurrentLust.NormalizeClamped(0f, MaxLust);
                AddObservation(ref currentIndex, this, currentLust); // 17
                
                float estimatedFutureLust = 0f;
                foreach (StatusInstance status in _statuses)
                    if (status is Arousal arousalInstance)
                        estimatedFutureLust += arousalInstance.CalculateTotalLust();
                
                estimatedFutureLust = (CurrentLust + estimatedFutureLust).NormalizeClamped(0f, MaxLust);
                AddObservation(ref currentIndex, this, estimatedFutureLust); // 18
                
                float composure = GetComposure.NormalizeClamped(MinResilience, MaxResilience);
                AddObservation(ref currentIndex, this, composure); // 19

                float orgasmsRemaining = (OrgasmLimit - OrgasmCount).NormalizeClamped(0, OrgasmMax);
                AddObservation(ref currentIndex, this, orgasmsRemaining); // 20
            }
            else
            {
                AddObservation(ref currentIndex, this, -1f); // 17
                AddObservation(ref currentIndex, this, -1f); // 18
                AddObservation(ref currentIndex, this, -1f); // 19
                AddObservation(ref currentIndex, this, -1f); // 20
            }

            float guardedDuration = -1f;
            foreach (StatusInstance status in _statuses)
                if (status is Guarded)
                {
                    guardedDuration = status.Duration.NormalizeClamped(0f, TimeUpperBound);
                    break;
                }

            AddObservation(ref currentIndex, this, guardedDuration); // 21

            float markedDuration = -1f;
            foreach (StatusInstance status in _statuses)
                if (status is Marked)
                {
                    markedDuration = status.Duration.NormalizeClamped(0f, TimeUpperBound);
                    break;
                }

            AddObservation(ref currentIndex, this, markedDuration); // 22

            float riposteDuration = -1f;
            float riposteDamageMultiplier = -1f;
            foreach (StatusInstance status in _statuses)
                if (status is Riposte riposteInstance)
                {
                    riposteDuration = status.Duration.NormalizeClamped(0f, TimeUpperBound);
                    riposteDamageMultiplier = riposteInstance.RiposteMultiplier.NormalizeClamped(0f, 2f);
                    break;
                }

            AddObservation(ref currentIndex, this, riposteDuration); // 23
            AddObservation(ref currentIndex, this, riposteDamageMultiplier); // 24

            float stealthDuration = -1f;
            foreach (StatusInstance status in _statuses)
                if (status is Stealth)
                {
                    stealthDuration = status.Duration.NormalizeClamped(0f, TimeUpperBound);
                    break;
                }

            AddObservation(ref currentIndex, this, stealthDuration); // 25
        }

        //Made this static to make sure I'm not accessing member variables
        private static void CollectAlyObservations(ref int currentIndex, Character self, Lease<(ISkill, Lease<bool>)> castableSkills, (Character character, bool[] positions)[] allies)
        {
            for (int i = 0; i < 4; i++)
            {
                (Character character, bool[] positions) ally = allies[i];
                if (ally.character == null)
                    AddEmptyAllyObservation(ref currentIndex, self);
                else
                    AddAllyObservation(ref currentIndex, self, castableSkills, ally.character, ally.positions, i);
            }
        }

        private static void AddEmptyAllyObservation(ref int currentIndex, Character self)
        {
            Array.Fill(self._observations, -1f, currentIndex, AllyObservationCount);
            currentIndex += AllyObservationCount;
        }
        
        // 29 + (48 * 4) = 221
        private static void AddAllyObservation(ref int currentIndex, Character self, Lease<(ISkill, Lease<bool>)> castableSkills, Character ally, bool[] positions, int allyIndex)
        {
            float staminaRaw = ally.CurrentStamina.NormalizeClamped(0f, StaminaBaseConstant);
            AddObservation(ref currentIndex, self, staminaRaw); // 1
            
            float maxStamina = ally.MaxStamina.NormalizeClamped(0f, StaminaBaseConstant);
            AddObservation(ref currentIndex, self, maxStamina); // 2

            float resilience = ally.GetResilience.NormalizeClamped(MinResilience, MaxResilience);
            AddObservation(ref currentIndex, self, resilience); // 3
            
            float dodge = ally.GetDodge.NormalizeClamped(MinDodge, MaxDodge);
            AddObservation(ref currentIndex, self, dodge); // 4

            float estimatedFutureStamina = 0f;
            foreach (StatusInstance status in ally._statuses)
                if (status is Poison poison)
                    estimatedFutureStamina -= poison.CalculateTotalDamage();
                else if (status is OvertimeHeal overtimeHealInstance)
                    estimatedFutureStamina += overtimeHealInstance.CalculateTotalHeal();

            estimatedFutureStamina.NormalizeClamped(0f, StaminaBaseConstant);
            AddObservation(ref currentIndex, self, estimatedFutureStamina); // 5

            float critChance = ally.GetCriticalChance.NormalizeClamped(MinCrit, MaxCrit);
            AddObservation(ref currentIndex, self, critChance); // 6

            float stunRecoverySpeed = ally.GetStunRecoverySpeed.NormalizeClamped(MinSpeed, MaxSpeed);
            AddObservation(ref currentIndex, self, stunRecoverySpeed); // 7

            foreach (bool position in positions) // 11
                    AddObservation(ref currentIndex, self, position);

            float moveResistance = ally.GetMoveResistance.NormalizeClamped(MinResistance, MaxResistance);
            AddObservation(ref currentIndex, self, moveResistance); // 12

            float poisonResistance = ally.GetPoisonResistance.NormalizeClamped(MinResistance, MaxResistance);
            AddObservation(ref currentIndex, self, poisonResistance); // 13
            
            (float lower, float upper) damage = ally.GetDamage;
            float averageDamage = ((damage.lower + damage.upper) / 2f).NormalizeClamped(0f, StaminaBaseConstant);
            AddObservation(ref currentIndex, self, averageDamage); // 14

            float damageMultiplier = ally.GetDamageMultiplier.NormalizeClamped(0f, 2f);
            AddObservation(ref currentIndex, self, damageMultiplier); // 15

            float charge = ally.GetEstimatedCharge().NormalizeClamped(0f, TimeUpperBound);
            AddObservation(ref currentIndex, self, charge); // 16

            float recovery = ally.GetEstimatedRecovery().NormalizeClamped(0f, TimeUpperBound);
            AddObservation(ref currentIndex, self, recovery); // 17

            float stun = ally.GetEstimatedStun().NormalizeClamped(0f, TimeUpperBound);
            AddObservation(ref currentIndex, self, stun); // 18

            AddObservation(ref currentIndex, self, ally.IsGirl); // 19
            
            if (ally.IsGirl)
            {
                float currentLust = ally.CurrentLust.NormalizeClamped(0f, MaxLust);
                AddObservation(ref currentIndex, self, currentLust); // 20
                
                float estimatedFutureLust = 0f;
                foreach (StatusInstance status in ally._statuses)
                    if (status is Arousal arousalInstance)
                        estimatedFutureLust += arousalInstance.CalculateTotalLust();
                
                estimatedFutureLust = (ally.CurrentLust + estimatedFutureLust).NormalizeClamped(0f, MaxLust);
                AddObservation(ref currentIndex, self, estimatedFutureLust); // 21
                
                float composure = ally.GetComposure.NormalizeClamped(MinResilience, MaxResilience);
                AddObservation(ref currentIndex, self, composure); // 22

                float orgasmsRemaining = (ally.OrgasmLimit - ally.OrgasmCount).NormalizeClamped(0, OrgasmMax);
                AddObservation(ref currentIndex, self, orgasmsRemaining); // 23
                
                float downed = ally.GetEstimatedDowned().NormalizeClamped(0f, TimeUpperBound);
                AddObservation(ref currentIndex, self, downed); // 24
            }
            else
            {
                AddObservation(ref currentIndex, self, -1f); // 20
                AddObservation(ref currentIndex, self, -1f); // 21
                AddObservation(ref currentIndex, self, -1f); // 22
                AddObservation(ref currentIndex, self, -1f); // 23
                AddObservation(ref currentIndex, self, -1f); // 24
            }

            float guardedDuration = -1f;
            foreach (StatusInstance status in ally._statuses)
                if (status is Guarded)
                {
                    guardedDuration = status.Duration.NormalizeClamped(0f, TimeUpperBound);
                    break;
                }

            AddObservation(ref currentIndex, self, guardedDuration); // 25

            float markedDuration = -1f;
            foreach (StatusInstance status in ally._statuses)
                if (status is Marked)
                {
                    markedDuration = status.Duration.NormalizeClamped(0f, TimeUpperBound);
                    break;
                }

            AddObservation(ref currentIndex, self, markedDuration); // 26

            float riposteDuration = -1f;
            float riposteDamageMultiplier = -1f;
            foreach (StatusInstance status in ally._statuses)
                if (status is Riposte riposteInstance)
                {
                    riposteDuration = status.Duration.NormalizeClamped(0f, TimeUpperBound);
                    riposteDamageMultiplier = riposteInstance.RiposteMultiplier.NormalizeClamped(0f, 2f);
                    break;
                }

            AddObservation(ref currentIndex, self, riposteDuration); // 27
            AddObservation(ref currentIndex, self, riposteDamageMultiplier); // 28

            float stealthDuration = -1f;
            foreach (StatusInstance status in ally._statuses)
                if (status is Stealth)
                {
                    stealthDuration = status.Duration.NormalizeClamped(0f, TimeUpperBound);
                    break;
                }

            AddObservation(ref currentIndex, self, stealthDuration); // 29
            
            for (int skillIndex = 0; skillIndex < castableSkills.Length; skillIndex++)
            {
                (ISkill skill, Lease<bool> validTargets) = castableSkills.Rented[skillIndex];
                
                if (skill == null || validTargets == null)
                {
                    AddEmptyTargetObservation(ref currentIndex);
                    continue;
                }

                bool canTarget = castableSkills.Rented[skillIndex].Item2.Rented[allyIndex];
                if (canTarget)
                {
                    float skillCharge = skill.BaseCharge.NormalizeClamped(0f, TimeUpperBound);
                    AddObservation(ref currentIndex, self, skillCharge); //1
                    
                    float skillRecovery = skill.BaseRecovery.NormalizeClamped(0f, TimeUpperBound);
                    AddObservation(ref currentIndex, self, skillRecovery); //2
                    
                    float skillHitChance = 1.NormalizeClamped(0f, 1f);
                    AddObservation(ref currentIndex, self, skillHitChance); //3

                    float skillCritChance = (skill.CanCrit ? skill.BaseCriticalChance + self.GetCriticalChance : 0).NormalizeClamped(0f, 1f);
                    AddObservation(ref currentIndex, self, skillCritChance); //4
                    
                    self.ClearReusableBuffsAndDebuffs();
                    
                    EffectValuesStruct estimatedEffects = new EffectValuesStruct(self._reusableBuffsAndDebuffs, self, ally)
                    {
                        AverageDamage = skill.GetEstimatedAverageDamageWithoutResilience(self, ally)
                    };
                    
                    foreach (StatusScript statusScript in skill.TargetEffects)
                    {
                    }


                    estimatedEffects.NormalizeValues();

                    int j = 0;
                    foreach (float value in estimatedEffects)  // 5 - 43 
                    {
                        AddObservation(ref currentIndex, self, value);
                        j++;
                    }

                    for (int i = 0; i < 4; i++) // 44 - 47
                    {
                        bool collateral = skill.TargetPlacement.collaterals[i];
                        AddObservation(ref currentIndex, self, collateral);
                    }
                }
                else
                {
                    AddEmptyTargetObservation(ref currentIndex);
                }
            }
            
            for (int i = castableSkills.Length; i < 4; i++)
                AddEmptyTargetObservation(ref currentIndex);

            void AddEmptyTargetObservation(ref int currentIndex)
            {
                Array.Fill(self._observations, -1f, currentIndex, TargetObservationCount);
                currentIndex += TargetObservationCount;
            }
        }

        private static void CollectEnemyObservations(ref int currentIndex, Character self, Lease<(ISkill, Lease<bool>)> castableSkills, (Character character, bool[] positions)[] enemies)
        {
            for (int i = 0; i < 4; i++)
            {
                (Character enemy, bool[] positions) = enemies[i];
                if (enemy == null)
                    AddEmptyEnemyObservation(ref currentIndex, self);
                else
                    AddEnemyObservation(ref currentIndex, self, castableSkills, enemy, positions, i);
            }
        }

        private static void AddEmptyEnemyObservation(ref int currentIndex, Character self)
        {
            Array.Fill(self._observations, -1f, currentIndex, EnemyObservationCount);
            currentIndex += EnemyObservationCount;
        }
        
        // 26 + (48 * 4) = 218
        private static void AddEnemyObservation(ref int currentIndex, Character self, Lease<(ISkill, Lease<bool>)> castableSkills, Character enemy, bool[] positions, int enemyIndex)
        {
            float currentStamina = enemy.CurrentStamina.NormalizeClamped(0f, StaminaBaseConstant);
            AddObservation(ref currentIndex, self, currentStamina); // 1
            
            float maxStamina = enemy.MaxStamina.NormalizeClamped(0f, StaminaBaseConstant);
            AddObservation(ref currentIndex, self, maxStamina); // 2
            
            float resilience = enemy.GetResilience.NormalizeClamped(MinResilience, MaxResilience);
            AddObservation(ref currentIndex, self, resilience); // 3
            
            float dodge = enemy.GetDodge.NormalizeClamped(MinDodge, MaxDodge);
            AddObservation(ref currentIndex, self, dodge); // 4

            float estimatedFutureStamina = 0f;
            foreach (StatusInstance status in enemy._statuses)
                if (status is Poison poison)
                    estimatedFutureStamina -= poison.CalculateTotalDamage();
                else if (status is OvertimeHeal overtimeHealInstance)
                    estimatedFutureStamina += overtimeHealInstance.CalculateTotalHeal();

            estimatedFutureStamina = estimatedFutureStamina.NormalizeClamped(0f, StaminaBaseConstant);
            AddObservation(ref currentIndex, self, estimatedFutureStamina); // 5
            
            float stunRecoverySpeed = enemy.GetStunRecoverySpeed.NormalizeClamped(MinSpeed, MaxSpeed);
            AddObservation(ref currentIndex, self, stunRecoverySpeed); // 6
            
            foreach (bool position in positions) // 10
                AddObservation(ref currentIndex, self, position);
            
            float moveResistance = enemy.GetMoveResistance.NormalizeClamped(MinResistance, MaxResistance);
            AddObservation(ref currentIndex, self, moveResistance); // 11
            
            float poisonResistance = enemy.GetPoisonResistance.NormalizeClamped(MinResistance, MaxResistance);
            AddObservation(ref currentIndex, self, poisonResistance); // 12

            float charge = enemy.GetEstimatedCharge().NormalizeClamped(0f, TimeUpperBound);
            AddObservation(ref currentIndex, self, charge); // 13

            float recovery = enemy.GetEstimatedRecovery().NormalizeClamped(0f, TimeUpperBound);
            AddObservation(ref currentIndex, self, recovery); // 14

            float stun = enemy.GetEstimatedStun().NormalizeClamped(0f, TimeUpperBound);
            AddObservation(ref currentIndex, self, stun); // 15
            
            AddObservation(ref currentIndex, self, enemy.IsGirl); // 16

            if (enemy.IsGirl)
            {
                float currentLust = enemy.CurrentLust.NormalizeClamped(0f, MaxLust);
                AddObservation(ref currentIndex, self, currentLust); // 17
                
                float estimatedFutureLust = 0f;
                foreach (StatusInstance status in enemy._statuses)
                    if (status is Arousal arousalInstance)
                        estimatedFutureLust += arousalInstance.CalculateTotalLust();
                
                estimatedFutureLust = (enemy.CurrentLust + estimatedFutureLust).NormalizeClamped(0f, MaxLust);
                AddObservation(ref currentIndex, self, estimatedFutureLust); // 18
                
                float composure = enemy.GetComposure.NormalizeClamped(MinResilience, MaxResilience);
                AddObservation(ref currentIndex, self, composure); // 19

                float orgasmsRemaining = (enemy.OrgasmLimit - enemy.OrgasmCount).NormalizeClamped(0, OrgasmMax);
                AddObservation(ref currentIndex, self, orgasmsRemaining); // 20
                
                float downed = enemy.GetEstimatedDowned().NormalizeClamped(0f, TimeUpperBound);
                AddObservation(ref currentIndex, self, downed); // 21
            }
            else
            {
                AddObservation(ref currentIndex, self, -1f); // 17
                AddObservation(ref currentIndex, self, -1f); // 18
                AddObservation(ref currentIndex, self, -1f); // 19
                AddObservation(ref currentIndex, self, -1f); // 20
                AddObservation(ref currentIndex, self, -1f); // 21
            }

            float guardedDuration = -1f;
            foreach (StatusInstance status in enemy._statuses)
                if (status is Guarded)
                {
                    guardedDuration = status.Duration.NormalizeClamped(0f, TimeUpperBound);
                    break;
                }

            AddObservation(ref currentIndex, self, guardedDuration); // 22

            float markedDuration = -1f;
            foreach (StatusInstance status in enemy._statuses)
                if (status is Marked)
                {
                    markedDuration = status.Duration.NormalizeClamped(0f, TimeUpperBound);
                    break;
                }

            AddObservation(ref currentIndex, self, markedDuration); // 23
            
            float ripostingDuration = -1f;
            float riposteDamageMultiplier = -1f;
            foreach (StatusInstance status in enemy._statuses)
                if (status is Riposte riposteInstance)
                {
                    ripostingDuration = status.Duration.NormalizeClamped(0f, TimeUpperBound);
                    riposteDamageMultiplier = riposteInstance.RiposteMultiplier.NormalizeClamped(0f, 2f);
                    break;
                }

            AddObservation(ref currentIndex, self, ripostingDuration); // 24
            AddObservation(ref currentIndex, self, riposteDamageMultiplier); // 25
            
            float stealthDuration = -1f;
            foreach (StatusInstance status in enemy._statuses)
                if (status is Stealth)
                {
                    stealthDuration = status.Duration.NormalizeClamped(0f, TimeUpperBound);
                    break;
                }

            AddObservation(ref currentIndex, self, stealthDuration); // 26
            
            for (int skillIndex = 0; skillIndex < castableSkills.Length; skillIndex++)
            {
                (ISkill skill, Lease<bool> validTargets) = castableSkills.Rented[skillIndex];
                
                if (skill == null || validTargets == null)
                {
                    AddEmptyTargetObservation(ref currentIndex);
                    continue;
                }

                bool canTarget = castableSkills.Rented[skillIndex].Item2.Rented[enemyIndex + 4];
                if (canTarget)
                {
                    float skillCharge = skill.BaseCharge.NormalizeClamped(0f, TimeUpperBound);
                    AddObservation(ref currentIndex, self, skillCharge); // 1
                    
                    float skillRecovery = skill.BaseRecovery.NormalizeClamped(0f, TimeUpperBound);
                    AddObservation(ref currentIndex, self, skillRecovery); // 2
                    
                    float skillHitChance = (skill.BaseAccuracy + self.GetAccuracy - enemy.GetDodge).NormalizeClamped(0f, 1f);
                    AddObservation(ref currentIndex, self, skillHitChance); // 3

                    float skillCritChance = (skill.CanCrit ? skill.BaseCriticalChance + self.GetCriticalChance : 0).NormalizeClamped(0f, 1f);
                    AddObservation(ref currentIndex, self, skillCritChance); // 4
                    
                    self.ClearReusableBuffsAndDebuffs();
                    
                    EffectValuesStruct estimatedEffects = new EffectValuesStruct(self._reusableBuffsAndDebuffs, self, enemy)
                    {
                        AverageDamage = skill.GetEstimatedAverageDamageWithoutResilience(self, enemy)
                    };

                    
                    foreach (StatusScript statusScript in skill.TargetEffects)
                    {
                    }
                    
                    estimatedEffects.NormalizeValues();
                    
                    foreach (float value in estimatedEffects)  // 5 - 44
                    {
                        AddObservation(ref currentIndex, self, value);
                    }

                    for (int i = 0; i < 4; i++) // 45 - 48
                    {
                        bool collateral = skill.TargetPlacement.collaterals[i];
                        AddObservation(ref currentIndex, self, collateral);
                    }
                }
                else
                {
                    AddEmptyTargetObservation(ref currentIndex);
                }
            }
            
            for (int i = castableSkills.Length; i < 4; i++)
                AddEmptyTargetObservation(ref currentIndex);

            void AddEmptyTargetObservation(ref int currentIndex)
            {
                Array.Fill(self._observations, -1f, currentIndex, TargetObservationCount);
                currentIndex += TargetObservationCount;
            }
        }
        
        private static void AddObservation(ref int currentIndex, Character self, float value)
        {
            self._observations[currentIndex] = value;
            currentIndex++;
        }
        
        private static void AddObservation(ref int currentIndex, Character self, bool value)
        {
            self._observations[currentIndex] = value ? 1f : 0f;
            currentIndex++;
        }
    }
}*/