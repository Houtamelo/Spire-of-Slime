using System.Collections.Generic;
using System.Text;
using Core.Combat.Scripts;
using Core.Combat.Scripts.Behaviour;
using Core.Combat.Scripts.Effects.BaseTypes;
using Core.Combat.Scripts.Effects.Types.BuffOrDebuff;
using Core.Combat.Scripts.Enums;
using Core.Combat.Scripts.Managers.Enumerators;
using Core.Combat.Scripts.Perks;
using Core.Combat.Scripts.Skills;
using Core.Combat.Scripts.Skills.Interfaces;
using Core.Main_Database.Combat;
using Core.Save_Management.SaveObjects;
using Core.Utils.Extensions;
using ListPool;

namespace Core.Main_Characters.Ethel.Combat.Perks.Poison
{
    public class PoisonCoating : PerkScriptable
    {
        public override PerkInstance CreateInstance(CharacterStateMachine character)
        {
            PoisonCoatingInstance instance = new(character, Key);
            character.PerksModule.Add(instance);
            return instance;
        }
    }
    
    public record PoisonCoatingRecord(CleanString Key) : PerkRecord(Key)
    {
        public override bool IsDataValid(StringBuilder errors, ICollection<CharacterRecord> allCharacters)
        {
            if (PerkDatabase.GetPerk(Key).IsNone)
            {
                errors.AppendLine("Invalid ", nameof(PoisonCoatingRecord), " data. ", nameof(Key), " with key: ", Key.ToString(), " does not exist in database.");
                return false;
            }
            
            return true;
        }

        public override PerkInstance CreateInstance(CharacterStateMachine owner, CharacterEnumerator allCharacters)
        {
            PoisonCoatingInstance instance = new(owner, record: this);
            owner.PerksModule.Add(instance);
            return instance;
        }
    }

    public class PoisonCoatingInstance : PerkInstance, ISkillModifier
    {
        public string SharedId => nameof(PoisonCoatingInstance);
        public int Priority => 0;
        private static readonly BuffOrDebuffScript Debuff = new(false, 5, 1f, CombatStat.PoisonResistance, -0.4f);

        public PoisonCoatingInstance(CharacterStateMachine owner, CleanString key) : base(owner, key)
        {
        }
        
        public PoisonCoatingInstance(CharacterStateMachine owner, PoisonCoatingRecord record) : base(owner, record)
        {
        }

        protected override void OnSubscribe()
        {
            Owner.SkillModule.SkillModifiers.Add(this);
        }

        protected override void OnUnsubscribe()
        {
            Owner.SkillModule.SkillModifiers.Remove(this);
        }

        public override PerkRecord GetRecord() => new PoisonCoatingRecord(Key);

        public void Modify(ref SkillStruct skillStruct)
        {
            CleanString key = skillStruct.Skill.Key;
            if (key != EthelSkills.Clash && key != EthelSkills.Sever && key != EthelSkills.Pierce)
                return;
            
            ref ValueListPool<IActualStatusScript> targetEffects = ref skillStruct.TargetEffects;
            targetEffects.Add(Debuff);
        }
    }
}