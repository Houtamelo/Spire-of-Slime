using System.Collections.Generic;
using JetBrains.Annotations;

namespace Core.Combat.Scripts.Skills.Action
{
    public readonly struct CasterContext
    {
        public readonly bool AnyHit;
        public bool Missed => AnyHit == false;
            
        public readonly bool AnyCritical;
        public readonly ActionResult[] Results;
        
        public CasterContext([NotNull] ActionResult[] results)
        {
            Results = results;
            AnyHit = false;
            AnyCritical = false;
            foreach (ActionResult result in results)
            {
                if (result.Hit && (result.Caster != result.Target || result.Skill.IsPositive))
                    AnyHit = true;
                    
                AnyCritical |= result.Critical;
            }
        }

        public void Deconstruct(out bool success, out bool critical, out IList<ActionResult> results)
        {
            success = AnyHit;
            critical = AnyCritical;
            results = Results;
        }
    }
}