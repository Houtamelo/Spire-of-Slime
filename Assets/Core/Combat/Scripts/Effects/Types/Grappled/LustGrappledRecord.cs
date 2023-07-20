using System;
using System.Text;
using Core.Combat.Scripts.Animations;
using Core.Combat.Scripts.Behaviour;
using Core.Combat.Scripts.Effects.BaseTypes;
using Core.Combat.Scripts.Managers.Enumerators;
using Core.Combat.Scripts.Skills.Action;
using Core.Utils.Extensions;
using Core.Utils.Math;
using UnityEngine;

namespace Core.Combat.Scripts.Effects.Types.Grappled
{
	public record LustGrappledRecord(TSpan Duration, bool Permanent, int LustPerTime, int TemptationDeltaPerTime, TSpan AccumulatedTime, string TriggerName, Guid Restrainer)
		: StatusRecord(Duration, Permanent)
	{
		public override bool IsDataValid<T>(StringBuilder errors, T allCharacters)
		{
			if (string.IsNullOrEmpty(TriggerName))
			{
				errors.AppendLine("Invalid ", nameof(LustGrappledRecord), " data. ", nameof(TriggerName), " cannot be null or empty.");
				return false;
			}

			foreach (CharacterRecord character in allCharacters)
			{
				if (Restrainer == character.Guid)
					return true;
			}
            
			errors.AppendLine("Invalid ", nameof(LustGrappledRecord), " data. ", nameof(Restrainer), "'s Guid: ", Restrainer.ToString(), " could not be mapped to a character.");
			return false;
		}

		public override void Deserialize(CharacterStateMachine owner, DirectCharacterEnumerator allCharacters)
		{
			CharacterStateMachine restrainer = null;
			foreach (CharacterStateMachine character in allCharacters)
			{
				if (character.Guid == Restrainer)
				{
					restrainer = character;
					break;
				}
			}

			if (restrainer == null)
			{
				Debug.LogWarning($"Could not find a character with Guid: {Restrainer}");
				return;
			}

			Utils.Patterns.Option<float> graphicalX = owner.Script.GetSexGraphicalX(TriggerName);
			if (graphicalX.IsNone)
			{
				Debug.LogWarning($"Could not find a graphical X for {TriggerName}");
				return;
			}

			LustGrappled instance = new(record: this, graphicalX.Value, owner, restrainer);
			owner.StatusReceiverModule.AddStatus(instance, restrainer);
			restrainer.StatusReceiverModule.TrackRelatedStatus(instance);
            
			if (owner.Display.TrySome(out DisplayModule ownerDisplay))
				ownerDisplay.AnimateGrappled();

			if (restrainer.Display.TrySome(out DisplayModule restrainerDisplay))
			{
				CombatAnimation animation = new(TriggerName, Utils.Patterns.Option<CasterContext>.None, Utils.Patterns.Option<TargetContext>.None);
				restrainerDisplay.AnimateGrappling(animation);
			}
		}
	}
}