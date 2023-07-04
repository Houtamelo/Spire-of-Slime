using System.Collections.Generic;

namespace Core.Combat.Scripts.Skills.Action
{
    public readonly struct CasterContext
    {
        public readonly bool AnyHit;
        public bool Missed => AnyHit == false;
            
        public readonly bool AnyCritical;
        public readonly ActionResult[] Results;
        
        public CasterContext(ActionResult[] results)
        {
            Results = results;
            AnyHit = false;
            AnyCritical = false;
            foreach (ActionResult result in results)
            {
                if (result.Hit && (result.Caster != result.Target || result.Skill.AllowAllies))
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