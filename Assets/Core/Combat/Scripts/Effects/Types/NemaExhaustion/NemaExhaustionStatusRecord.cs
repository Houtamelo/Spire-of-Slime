using System.Text;
using Core.Combat.Scripts.Behaviour;
using Core.Combat.Scripts.Behaviour.Modules;
using Core.Combat.Scripts.Effects.BaseTypes;
using Core.Combat.Scripts.Managers.Enumerators;
using Core.Utils.Math;
using JetBrains.Annotations;

namespace Core.Combat.Scripts.Effects.Types.NemaExhaustion
{
	public record NemaExhaustionStatusRecord() : StatusRecord(Duration: new TSpan(ticks: long.MaxValue / 2), Permanent: true)
	{
		public override bool IsDataValid<T>(StringBuilder errors, T allCharacters) => true;

		public override void Deserialize([NotNull] CharacterStateMachine owner, DirectCharacterEnumerator allCharacters)
		{
			NemaExhaustion instance = new(record: this, owner);
			owner.StatusReceiverModule.AddStatus(instance, owner);

			IStatsModule statsModule = owner.StatsModule;
			statsModule.SubscribeSpeed(instance.SpeedDebuff, allowDuplicates: false);
			statsModule.SubscribeDodge(instance.DodgeDebuff, allowDuplicates: false);
			statsModule.SubscribeAccuracy(instance.AccuracyDebuff, allowDuplicates: false);

			IResistancesModule resistancesModule = owner.ResistancesModule;
			resistancesModule.SubscribeDebuffResistance(instance.ResistanceDebuff, allowDuplicates: false);
			resistancesModule.SubscribeMoveResistance(instance.ResistanceDebuff, allowDuplicates: false);
			resistancesModule.SubscribePoisonResistance(instance.ResistanceDebuff, allowDuplicates: false);
            
			owner.StunModule.SubscribeStunMitigation(instance.StunMitigationDebuff, allowDuplicates: false);
            
			if (owner.LustModule.TrySome(out ILustModule lustModule))
				lustModule.SubscribeComposure(instance.ComposureDebuff, allowDuplicates: false);
		}
	}
}