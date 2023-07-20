using System.Text;
using Core.Combat.Scripts.Behaviour;
using Core.Combat.Scripts.Behaviour.Modules;
using Core.Combat.Scripts.Effects.BaseTypes;
using Core.Combat.Scripts.Managers.Enumerators;
using Core.Combat.Scripts.Perks;
using Core.Main_Database.Combat;
using Core.Save_Management.SaveObjects;
using Core.Utils.Extensions;
using Core.Utils.Math;
using Core.Utils.Patterns;
using JetBrains.Annotations;
using UnityEngine;

namespace Core.Combat.Scripts.Effects.Types.Perk
{
	public record PerkStatusRecord(TSpan Duration, bool Permanent, CleanString PerkKey, bool IsHidden) : StatusRecord(Duration, Permanent)
	{
		public override bool IsDataValid<T>(StringBuilder errors, T allCharacters)
		{
			if (PerkDatabase.GetPerk(PerkKey).IsNone)
			{
				errors.AppendLine("Invalid ", nameof(PerkStatusRecord), " data. ", nameof(PerkKey), " with key: ", PerkKey.ToString(), " does not exist in database.");
				return false;
			}
            
			return true;
		}

		public override void Deserialize([NotNull] CharacterStateMachine owner, DirectCharacterEnumerator allCharacters)
		{
			PerkInstance perk = null;
			foreach (PerkInstance perkInstance in owner.PerksModule.GetAll)
			{
				if (perkInstance.Key == PerkKey)
				{
					perk = perkInstance;
					break;
				}
			}
            
			if (perk == null)
			{
				Debug.LogWarning($"Could not find perk with key {PerkKey} in {nameof(IPerksModule)}");
				return;
			}
            
			Option<PerkScriptable> perkScript = PerkDatabase.GetPerk(PerkKey);
			if (perkScript.IsNone)
			{
				Debug.LogWarning($"Could not find perk with key {PerkKey} in {nameof(PerkDatabase)}");
				return;
			}
            
			PerkStatus instance = new(record: this, owner, perk);
			owner.StatusReceiverModule.AddStatus(instance, owner);
		}
	}
}