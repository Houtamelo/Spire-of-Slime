using System.Text;
using Core.Combat.Scripts.Behaviour;
using Core.Combat.Scripts.Enums;
using Core.Combat.Scripts.Managers.Enumerators;
using Core.Utils.Extensions;
using Core.Utils.Math;

namespace Core.Combat.Scripts.Effects.Types.BuffOrDebuff
{
	public record StunRedundancyRecord(TSpan Duration, TSpan InitialDuration, bool Permanent, int Delta) : BuffOrDebuffRecord(Duration, Permanent, CombatStat.StunMitigation, Delta)
	{
		public override bool IsDataValid<T>(StringBuilder errors, T allCharacters)
		{
			if (Delta == 0)
			{
				errors.AppendLine("Invalid ", nameof(StunRedundancyRecord), " data. ", nameof(Delta), " cannot be 0.");
				return false;
			}
            
			return true;
		}

		public override void Deserialize(CharacterStateMachine owner, DirectCharacterEnumerator allCharacters)
		{
			StunRedundancyBuff stunRedundancyBuff = new(record: this, owner);
			owner.StatusReceiverModule.AddStatus(stunRedundancyBuff, owner);
			stunRedundancyBuff.Subscribe();
		}
	}
}