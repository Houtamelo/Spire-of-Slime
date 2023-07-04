namespace Core.Combat.Scripts.Enums
{
    public readonly struct FullCharacterState
    {
        public bool Corpse { get; init; }
        public bool Defeated { get; init; }
        public bool Grappled { get; init; }
        public bool Downed { get; init; }
        public bool Stunned { get; init; }
        public bool Grappling { get; init; }
        public bool Charging { get; init; }
        public bool Recovering { get; init; }
        public bool Idle { get; init; }
        public CharacterState Main { get; init; }
    }
}