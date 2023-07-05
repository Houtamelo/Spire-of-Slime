using System.Collections.Generic;
using System.Text;
using Core.Combat.Scripts;
using Core.Combat.Scripts.Animations;
using Core.Combat.Scripts.Behaviour;
using Core.Combat.Scripts.Interfaces.Events;
using Core.Combat.Scripts.Managers.Enumerators;
using Core.Combat.Scripts.Perks;
using Core.Combat.Scripts.Skills.Action;
using Core.Save_Management.SaveObjects;
using Core.Utils.Patterns;
using UnityEngine;
using Utils.Patterns;

namespace Core.Combat.Data.Enemies.Bell_Plant.Skills.Lure
{
    public class LureTemporaryPerkScript : PerkScriptable
    {
        public override PerkInstance CreateInstance(CharacterStateMachine character)
        {
            LureTemporaryPerkInstance instance = new(character, Key);
            character.PerksModule.Add(instance);
            return instance;
        }
    }
    
    public record LureTemporaryPerkRecord(CleanString Key) : PerkRecord(Key)
    {
        public override bool IsDataValid(StringBuilder errors, ICollection<CharacterRecord> allCharacters) => true;

        public override PerkInstance CreateInstance(CharacterStateMachine owner, CharacterEnumerator allCharacters)
        {
            LureTemporaryPerkInstance instance = new(owner, record: this);
            owner.PerksModule.Add(instance);
            return instance;
        }
    }

    public class LureTemporaryPerkInstance : PerkInstance, ISelfAttackedListener
    {
        // ReSharper disable once InconsistentNaming
        private const string Param_LureSplash = "LureSplashFX";
        private static readonly int LureSplashID = Animator.StringToHash(Param_LureSplash);

        public LureTemporaryPerkInstance(CharacterStateMachine owner, CleanString key) : base(owner, key)
        {
        }
        
        public LureTemporaryPerkInstance(CharacterStateMachine owner, LureTemporaryPerkRecord record) : base(owner, record)
        {
        }

        protected override void OnSubscribe()
        {
            Owner.Events.SelfAttackedListeners.Add(this);
        }

        protected override void OnUnsubscribe()
        {
            Owner.Events.SelfAttackedListeners.Remove(this);
        }

        public override PerkRecord GetRecord() => new LureTemporaryPerkRecord(Key);

        public void OnSelfAttacked(ref ActionResult result)
        {
            if (result.Caster.PositionHandler.IsLeftSide == result.Target.PositionHandler.IsLeftSide || result.Skill.AllowAllies || result.Caster.LustModule.IsNone || result.Missed)
                return;
            
            result.Caster.LustModule.Value.ChangeLust(delta: +12);
            
            if (Owner.Display.TrySome(out CharacterDisplay display) == false)
                return;
            
            CombatAnimation animation = new(Param_LureSplash, Option<CasterContext>.None, Option<TargetContext>.Some(new TargetContext(result)), LureSplashID);
            display.SetAnimationWithoutNotifyStatus(animation);
        }
    }
}