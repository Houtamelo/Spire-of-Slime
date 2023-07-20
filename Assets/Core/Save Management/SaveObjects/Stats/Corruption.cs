using Core.Combat.Scripts;
using JetBrains.Annotations;

namespace Core.Save_Management.SaveObjects
{
    public static class Corruption
    {
        private static class CorruptionThresholds
        {
            public const int Max = 100;
            public const int High = 75;
            public const int Medium = 50;
            public const int Low = 25;
        }

        private static class LustThresholds
        {
            public const int Max = 200;
            public const int High = 180;
            public const int Medium = 140;
            public const int Low = 100;
        }
        
        private static class TemptationThresholds
        {
            public const int Max = 100;
            public const int High = 90;
            public const int Medium = 70;
            public const int Low = 50;
        }
        
        private static class RaceExpThresholds
        {
            public const int Max = 400;
            public const int High = 200;
            public const int Medium = 150;
            public const int Low = 100;
            public const int VeryLow = 50;
        }
        
        public static Threshold DesireThreshold([NotNull] IReadonlyCharacterStats stats, Race partnerRace)
        {
            int score = 0;

            score += stats.Lust switch
            {
                >= LustThresholds.Max    => 6,
                >= LustThresholds.High   => 5,
                >= LustThresholds.Medium => 3,
                >= LustThresholds.Low    => 2,
                _                        => 0
            };

            score += stats.Temptation switch
            {
                >= TemptationThresholds.Max    => 6,
                >= TemptationThresholds.High   => 5,
                >= TemptationThresholds.Medium => 3,
                >= TemptationThresholds.Low    => 2,
                _                              => 0
            };
            
            stats.SexualExpByRace.TryGetValue(partnerRace, out int raceExpCount);

            score += raceExpCount switch
            {
                >= RaceExpThresholds.Max     => 6,
                >= RaceExpThresholds.High    => 4,
                >= RaceExpThresholds.Medium  => 3,
                >= RaceExpThresholds.Low     => 2,
                >= RaceExpThresholds.VeryLow => 1,
                _                            => 0
            };

            score += stats.Corruption switch
            {
                >= CorruptionThresholds.Max    => 5,
                >= CorruptionThresholds.High   => 3,
                >= CorruptionThresholds.Medium => 2,
                >= CorruptionThresholds.Low    => 1,
                _                              => 0
            };

            return score switch
            {
                >= 16 => Threshold.Max,
                >= 12 => Threshold.High,
                >= 8  => Threshold.Medium,
                >= 4  => Threshold.Low,
                _     => Threshold.VeryLow
            };
        }
        
        public static Threshold CorruptionOnlyThreshold([NotNull] IReadonlyCharacterStats stats)
        {
            return stats.Corruption switch
            {
                >= CorruptionThresholds.Max    => Threshold.Max,
                >= CorruptionThresholds.High   => Threshold.High,
                >= CorruptionThresholds.Medium => Threshold.Medium,
                >= CorruptionThresholds.Low    => Threshold.Low,
                _                              => Threshold.VeryLow
            };
        }

        public enum Threshold
        {
            VeryLow,
            Low,
            Medium,
            High,
            Max
        }
    }
}