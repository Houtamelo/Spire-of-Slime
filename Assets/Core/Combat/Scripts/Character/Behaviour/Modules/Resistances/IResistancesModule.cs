using Core.Combat.Scripts.Effects.Interfaces;
using Core.Utils.Math;

namespace Core.Combat.Scripts.Behaviour.Modules
{
    public abstract record ResistancesRecord : ModuleRecord
    {
        public abstract IResistancesModule Deserialize(CharacterStateMachine owner);
    }
    
    public interface IResistancesModule : IModule   
    {
        public const int MinResistance = -300;
        public const int MaxResistance = 300;

        public static int ClampResistance(int resistance) => resistance.Clamp(MinResistance, MaxResistance);
        
        int BaseDebuffResistance { get; set; }
        void SubscribeDebuffResistance(IBaseAttributeModifier modifier, bool allowDuplicates);
        void UnsubscribeDebuffResistance(IBaseAttributeModifier modifier);
        protected int GetDebuffResistanceInternal();
        public sealed int GetDebuffResistance() => ClampResistance(GetDebuffResistanceInternal());

        int BaseMoveResistance { get; set; }
        void SubscribeMoveResistance(IBaseAttributeModifier modifier, bool allowDuplicates);
        void UnsubscribeMoveResistance(IBaseAttributeModifier modifier);
        protected int GetMoveResistanceInternal();
        public sealed int GetMoveResistance() => ClampResistance(GetMoveResistanceInternal());

        int BasePoisonResistance { get; set; }
        void SubscribePoisonResistance(IBaseAttributeModifier modifier, bool allowDuplicates);
        void UnsubscribePoisonResistance(IBaseAttributeModifier modifier); 
        protected int GetPoisonResistanceInternal();
        public sealed int GetPoisonResistance() => ClampResistance(GetPoisonResistanceInternal());
        
        ResistancesRecord GetRecord();
    }
}