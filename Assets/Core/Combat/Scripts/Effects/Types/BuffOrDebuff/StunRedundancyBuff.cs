using Core.Combat.Scripts.Behaviour;
using Core.Combat.Scripts.Effects.BaseTypes;
using Core.Combat.Scripts.Enums;
using Core.Utils.Math;
using Core.Utils.Patterns;
using JetBrains.Annotations;
using UnityEngine;

namespace Core.Combat.Scripts.Effects.Types.BuffOrDebuff
{
	public class StunRedundancyBuff : BuffOrDebuff
	{
		private readonly TSpan _initialDuration;

		public override int GetDelta
		{
			get
			{
				if (Permanent)
					return Delta;
                
				ClampedPercentage durationPercentage = (ClampedPercentage)(Duration.FloatSeconds / _initialDuration.FloatSeconds);
				return (Delta * durationPercentage.value).FloorToInt();
			}
		}

		public StunRedundancyBuff(TSpan duration, bool isPermanent, CharacterStateMachine owner, int delta) : base(duration, isPermanent, owner, attribute: CombatStat.StunMitigation, delta) => _initialDuration = duration;

		public static Option<StatusInstance> CreateFromAppliedStun(TSpan duration, bool isPermanent, CharacterStateMachine owner, int delta)
		{
			if ((duration.Ticks <= 0 && !isPermanent) || delta == 0)
			{
				Debug.LogWarning($"Invalid parameters for {nameof(BuffOrDebuff)} effect. Duration: {duration.Seconds.ToString()}, Permanent: {isPermanent.ToString()}, Delta: {delta.ToString()}");
				return Option.None;
			}

			StunRedundancyBuff stunRedundancyBuff = new(duration, isPermanent, owner, delta);
			stunRedundancyBuff.Subscribe();
			owner.StatusReceiverModule.AddStatus(stunRedundancyBuff, owner);
			return stunRedundancyBuff;
		}
        
		public StunRedundancyBuff([NotNull] StunRedundancyRecord record, CharacterStateMachine owner) : base(record, owner) => _initialDuration = record.InitialDuration;

		public void AddRedundancy(int value)
		{
			Delta += value;
			Duration = _initialDuration;
		}

		/// <summary> Duration does not deplete if Owner is stunned. </summary>
		public override void Tick(TSpan timeStep)
		{
			if (Owner.StunModule.GetRemaining().Ticks > 0)
				return;
            
			base.Tick(timeStep);
		}

		[NotNull]
		public override StatusRecord GetRecord() => new StunRedundancyRecord(Duration, _initialDuration, Permanent, GetDelta);
	}
}