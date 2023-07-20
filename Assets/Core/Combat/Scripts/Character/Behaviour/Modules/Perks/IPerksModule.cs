using Core.Combat.Scripts.Managers.Enumerators;
using Core.Combat.Scripts.Perks;
using Core.Utils.Collections;

namespace Core.Combat.Scripts.Behaviour.Modules
{
    public abstract record PerksModuleRecord : ModuleRecord
    {
        public abstract IPerksModule Deserialize(CharacterStateMachine owner);
        public abstract void AddSerializedPerks(CharacterStateMachine owner, DirectCharacterEnumerator allCharacters);
    }
    
    public interface IPerksModule : IModule
    {
        FixedEnumerator<PerkInstance> GetAll { get; }
        
        void Add(PerkInstance perk);
        void Remove(PerkInstance perk);
        void RemoveAll();
        
        PerksModuleRecord GetRecord();
    }
}