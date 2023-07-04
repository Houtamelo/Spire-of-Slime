namespace Core.Combat.Scripts.Effects
{
    public enum EffectType
    {
        Buff = 0,
        Debuff = 1,
        Poison = 2,
        Arousal = 4,
        Riposte = 6,
        OvertimeHeal = 7,
        Marked = 8,
        Stun = 9,
        Guarded = 10,
        Move = 11,
        LustGrappled = 16,
        Perk = 18,
        HiddenPerk = 24, // only difference is that this shows a question mark icon
        Heal = 19,
        Lust = 20,
        NemaExhaustion = 21,
        Mist = 22,
        Summon = 23, 
        Temptation = 25,
    }
}