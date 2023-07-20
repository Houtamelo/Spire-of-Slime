using System.Text;
using Core.Combat.Scripts.Behaviour;
using Core.Combat.Scripts.Effects.BaseTypes;
using Core.Combat.Scripts.Managers.Enumerators;
using Core.Utils.Math;
using JetBrains.Annotations;

namespace Core.Combat.Scripts.Effects.Types.Mist
{
	public record MistStatusRecord(TSpan AccumulatedRawTime, TSpan AccumulatedProcessedTime, TSpan Duration, bool Permanent) : StatusRecord(Duration, Permanent)
	{
		public override bool IsDataValid<T>(StringBuilder errors, T allCharacters) => true;

		public override void Deserialize([NotNull] CharacterStateMachine owner, DirectCharacterEnumerator allCharacters)
		{
			MistStatus instance = new(record: this, owner);
			owner.StatusReceiverModule.AddStatus(instance, owner);
			owner.LustModule.Value.SubscribeComposure(instance.ComposureDebuff, allowDuplicates: false);
		}
	}
}