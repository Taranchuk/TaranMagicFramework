using System.Linq;
using Verse;

namespace TaranMagicFramework
{
    public class AIAbilityWorker_TargetSelfHostileNearby : AIAbilityWorker
    {
        public override bool CanActivate(Ability ability, out TargetData targetData)
        {
            if (base.CanActivate(ability, out targetData))
            {
                if (ability.AbilityTier.AOERadius > 0)
                {
                    if (ability.GetHostileTargets().Any(x => x.Position.DistanceTo(ability.pawn.Position)
<= ability.AbilityTier.AOERadius))
                    {
                        targetData = new(null, ability.pawn);
                        return true;
                    }
                }
                else
                {
                    if (ability.GetHostileTargets().Any(x => x.Position.DistanceTo(ability.pawn.Position) <= 1.5f))
                    {
                        targetData = new(null, ability.pawn);
                        return true;
                    }
                }
            }
            return false;
        }

        public override float Priority(Ability ability)
        {
            return 9f;
        }
    }
}
