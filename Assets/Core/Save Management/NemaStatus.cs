using System.Text;
using Core.Save_Management.SaveObjects;
using Core.Utils.Extensions;
using Core.Utils.Patterns;

namespace Core.Save_Management
{
    public readonly struct NemaStatus
    {
        private static readonly StringBuilder Builder = new();
        
        public readonly (int previous, int current) Exhaustion;
        public readonly (bool previous, bool current) SetToClearMist;
        
        public readonly (bool previous, bool current) IsInCombat;
        private readonly (bool previous, bool current) _isStanding;
        public (bool previous, bool current) IsStanding => (IsInCombat.previous && _isStanding.previous, IsInCombat.current && _isStanding.current);

        public NemaStatus((int previous, int current) exhaustion, (bool previous, bool current) setToClearMist, (bool previous, bool current) isInCombat, (bool previous, bool current) isStanding)
        {
            Exhaustion = exhaustion;
            SetToClearMist = setToClearMist;
            IsInCombat = isInCombat;
            _isStanding = isStanding;
        }

        public override string ToString()
        {
            Builder.Clear();
            Builder.AppendLine("Exhaustion: ",     Exhaustion.previous.ToString(),     " -> ", Exhaustion.current.ToString());
            Builder.AppendLine("SetToClearMist: ", SetToClearMist.previous.ToString(), " -> ", SetToClearMist.current.ToString());
            Builder.AppendLine("IsInCombat: ",     IsInCombat.previous.ToString(),     " -> ", IsInCombat.current.ToString());
            Builder.AppendLine("IsStanding: ",     IsStanding.previous.ToString(),     " -> ", IsStanding.current.ToString());
            return Builder.ToString();
        }

        public (ExhaustionEnum previous, ExhaustionEnum current) GetEnum()
        {
            ExhaustionEnum previous = Exhaustion.previous switch
            {
                >= HighExhaustion   => ExhaustionEnum.High,
                >= MediumExhaustion => ExhaustionEnum.Medium,
                >= LowExhaustion    => ExhaustionEnum.Low, 
                _                   => ExhaustionEnum.None
            };
            
            ExhaustionEnum current = Exhaustion.current switch
            {
                >= HighExhaustion   => ExhaustionEnum.High,
                >= MediumExhaustion => ExhaustionEnum.Medium,
                >= LowExhaustion    => ExhaustionEnum.Low, 
                _                   => ExhaustionEnum.None
            };
            
            return (previous, current);
        }

        public const int HighExhaustion = 100;
        public const int MediumExhaustion = 70;
        public const int LowExhaustion = 40;
    }
}