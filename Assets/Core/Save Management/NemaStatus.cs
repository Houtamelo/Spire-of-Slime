using System.Text;
using Core.Save_Management.SaveObjects;
using Core.Utils.Patterns;
using Utils.Patterns;

namespace Core.Save_Management
{
    public readonly struct NemaStatus
    {
        private static readonly StringBuilder StringBuilder = new();
        
        public readonly (ClampedPercentage previous, ClampedPercentage current) Exhaustion;
        public readonly (bool previous, bool current) SetToClearMist;
        
        public readonly (bool previous, bool current) IsInCombat;
        private readonly (bool previous, bool current) _isStanding;
        public (bool previous, bool current) IsStanding => (IsInCombat.previous && _isStanding.previous, IsInCombat.current && _isStanding.current);

        public NemaStatus((ClampedPercentage previous, ClampedPercentage current) exhaustion, (bool previous, bool current) setToClearMist, (bool previous, bool current) isInCombat, (bool previous, bool current) isStanding)
        {
            Exhaustion = exhaustion;
            SetToClearMist = setToClearMist;
            IsInCombat = isInCombat;
            _isStanding = isStanding;
        }

        public override string ToString()
        {
            StringBuilder.Clear();
            StringBuilder.AppendLine($"Exhaustion: {Exhaustion.previous.ToString()} -> {Exhaustion.current.ToString()}");
            StringBuilder.AppendLine($"SetToClearMist: {SetToClearMist.previous.ToString()} -> {SetToClearMist.current.ToString()}");
            StringBuilder.AppendLine($"IsInCombat: {IsInCombat.previous.ToString()} -> {IsInCombat.current.ToString()}");
            StringBuilder.AppendLine($"IsStanding: {IsStanding.previous.ToString()} -> {IsStanding.current.ToString()}");
            return StringBuilder.ToString();
        }

        public (ExhaustionEnum previous, ExhaustionEnum current) GetEnum()
        {
            ExhaustionEnum previous = (float)Exhaustion.previous switch
            {
                > Save.HighExhaustion   => ExhaustionEnum.High,
                > Save.MediumExhaustion => ExhaustionEnum.Medium,
                > Save.LowExhaustion    => ExhaustionEnum.Low, 
                _                             => ExhaustionEnum.None
            };
            
            ExhaustionEnum current = (float)Exhaustion.current switch
            {
                > Save.HighExhaustion   => ExhaustionEnum.High,
                > Save.MediumExhaustion => ExhaustionEnum.Medium,
                > Save.LowExhaustion    => ExhaustionEnum.Low, 
                _                             => ExhaustionEnum.None
            };
            
            return (previous, current);
        }

    }
}