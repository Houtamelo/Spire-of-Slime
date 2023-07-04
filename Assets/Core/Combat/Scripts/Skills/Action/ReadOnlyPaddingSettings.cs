namespace Core.Combat.Scripts.Skills.Action
{
    public readonly struct ReadOnlyPaddingSettings
    {
        public readonly float AllyMiddlePadding;
        public readonly float EnemyMiddlePadding;
        public readonly float InBetweenPadding;

        public ReadOnlyPaddingSettings(ActionPaddingSettings settings)
        {
            AllyMiddlePadding = settings.AllyMiddlePadding;
            EnemyMiddlePadding = settings.EnemyMiddlePadding;
            InBetweenPadding = settings.InBetweenPadding;
        }
        
        public ReadOnlyPaddingSettings(float allyMiddlePadding, float enemyMiddlePadding, float inBetweenPadding)
        {
            AllyMiddlePadding = allyMiddlePadding;
            EnemyMiddlePadding = enemyMiddlePadding;
            InBetweenPadding = inBetweenPadding;
        }
        
        public static implicit operator ReadOnlyPaddingSettings(ActionPaddingSettings settings) => new(settings);
    }
}