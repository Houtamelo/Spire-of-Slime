using Core.Combat.Scripts.Perks;
using Core.Utils.Collections;

namespace Core.Combat.Scripts.Interfaces.Modules
{
    public interface IPerksModule : IModule
    {
        FixedEnumerable<PerkInstance> GetAll { get; }
        void Add(PerkInstance perk);
        void Remove(PerkInstance perk);
        void RemoveAll();
    }
}