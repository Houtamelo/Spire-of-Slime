using System.Text;
using Core.Combat.Scripts.Behaviour;
using Core.Combat.Scripts.Effects.BaseTypes;
using Core.Combat.Scripts.Enums;
using Core.Combat.Scripts.Managers.Enumerators;
using Core.Utils.Extensions;
using Core.Utils.Math;
using JetBrains.Annotations;

namespace Core.Combat.Scripts.Effects.Types.BuffOrDebuff
{
	public record BuffOrDebuffRecord(TSpan Duration, bool Permanent, CombatStat Attribute, int Delta) : StatusRecord(Duration, Permanent)
	{
		public override bool IsDataValid<T>(StringBuilder errors, T allCharacters)
		{
			if (Delta == 0)
			{
				errors.AppendLine("Invalid ", nameof(BuffOrDebuffRecord), " data. ", nameof(Delta), " cannot be 0.");
				return false;
			}
            
			return true;
		}

		public override void Deserialize([NotNull] CharacterStateMachine owner, DirectCharacterEnumerator allCharacters)
		{
			BuffOrDebuff buffOrDebuff = new(record: this, owner);
			owner.StatusReceiverModule.AddStatus(buffOrDebuff, owner);
			buffOrDebuff.Subscribe();
		}
	}
}