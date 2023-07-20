using System.Text;
using Core.Combat.Scripts.Behaviour;
using Core.Combat.Scripts.Effects.BaseTypes;
using Core.Combat.Scripts.Managers.Enumerators;
using Core.Utils.Extensions;
using Core.Utils.Math;
using JetBrains.Annotations;

namespace Core.Combat.Scripts.Effects.Types.OvertimeHeal
{
	public record OvertimeHealRecord(TSpan Duration, bool Permanent, int HealPerTime, TSpan AccumulatedTime) : StatusRecord(Duration, Permanent)
	{
		public override bool IsDataValid<T>(StringBuilder errors, T allCharacters)
		{
			if (HealPerTime == 0)
			{
				errors.AppendLine("Invalid ", nameof(OvertimeHealRecord), " data. ", nameof(HealPerTime), " cannot be 0.");
				return false;
			}
            
			return true;
		}

		public override void Deserialize([NotNull] CharacterStateMachine owner, DirectCharacterEnumerator allCharacters)
		{
			OvertimeHeal instance = new(record: this, owner);
			owner.StatusReceiverModule.AddStatus(instance, owner);
		}
	}
}