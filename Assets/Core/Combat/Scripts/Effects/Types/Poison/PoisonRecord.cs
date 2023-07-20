using System;
using System.Text;
using Core.Combat.Scripts.Behaviour;
using Core.Combat.Scripts.Effects.BaseTypes;
using Core.Combat.Scripts.Managers.Enumerators;
using Core.Utils.Extensions;
using Core.Utils.Math;
using UnityEngine;

namespace Core.Combat.Scripts.Effects.Types.Poison
{
	public record PoisonRecord(TSpan Duration, bool Permanent, int DamagePerTime, TSpan AccumulatedTime, Guid Caster) : StatusRecord(Duration, Permanent)
	{
		public override bool IsDataValid<T>(StringBuilder errors, T allCharacters)
		{
			if (DamagePerTime == 0)
			{
				errors.AppendLine("Invalid ", nameof(PoisonRecord), " data. ", nameof(DamagePerTime), " is 0.");
				return false;
			}
            
			foreach (CharacterRecord character in allCharacters)
			{
				if (character.Guid == Caster)
					return true;
			}
            
			errors.AppendLine("Invalid ", nameof(PoisonRecord), " data. ", nameof(Caster), "'s Guid: ", Caster.ToString(), " could not be mapped to a character.");
			return false;
		}

		public override void Deserialize(CharacterStateMachine owner, DirectCharacterEnumerator allCharacters)
		{
			CharacterStateMachine caster = null;
			foreach (CharacterStateMachine character in allCharacters)
			{
				if (character.Guid == Caster)
				{
					caster = character;
					break;
				}
			}
            
			if (caster == null)
			{
				Debug.LogWarning($"Could not find caster with Guid: {Caster}.");
				return;
			}
            
			Poison instance = new(record: this, owner, caster);
			owner.StatusReceiverModule.AddStatus(instance, owner);
		}
	}
}