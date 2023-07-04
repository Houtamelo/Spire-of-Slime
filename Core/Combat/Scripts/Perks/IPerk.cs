using Core.Combat.Scripts.Behaviour;
using Save_Management;

namespace Core.Combat.Scripts.Perks
{
    public interface IPerk
    {
        CleanString Key { get; }
        string DisplayName { get; }
        bool IsPositive { get; }
        string Description { get; }
        string FlavorText { get; }
        PerkInstance CreateInstance(CharacterStateMachine character);
    }
}