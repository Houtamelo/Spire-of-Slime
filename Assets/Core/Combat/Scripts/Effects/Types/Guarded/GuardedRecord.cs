using System;
using System.Text;
using Core.Combat.Scripts.Behaviour;
using Core.Combat.Scripts.Effects.BaseTypes;
using Core.Combat.Scripts.Managers.Enumerators;
using Core.Utils.Extensions;
using Core.Utils.Math;
using JetBrains.Annotations;
using UnityEngine;

namespace Core.Combat.Scripts.Effects.Types.Guarded
{
	public record GuardedRecord(TSpan Duration, bool Permanent, Guid Caster) : StatusRecord(Duration, Permanent)
	{
		public override bool IsDataValid<T>(StringBuilder errors, [NotNull] T allCharacters)
		{
			foreach (CharacterRecord character in allCharacters)
			{
				if (character.Guid == Caster)
					return true;
			}
            
			errors.AppendLine("Invalid ", nameof(GuardedRecord), " data. ", nameof(Caster), "'s Guid: ", Caster.ToString(), " could not be mapped to a character.");
			return false;
		}

		public override void Deserialize(CharacterStateMachine owner, DirectCharacterEnumerator allCharacters)
		{
			foreach (CharacterStateMachine character in allCharacters)
			{
				if (character.Guid == Caster)
				{
					Guarded instance = new(record: this, owner, character);
					owner.StatusReceiverModule.AddStatus(instance, character);
					return;
				}
			}
            
			Debug.LogWarning($"Invalid {nameof(GuardedRecord)} data. {nameof(Caster)}'s Guid: {Caster.ToString()} could not be mapped to a character.");
		}
	}
}