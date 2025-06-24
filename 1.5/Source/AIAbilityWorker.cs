using System;
using System.Collections.Generic;

namespace TaranMagicFramework
{
    public abstract class AIAbilityWorker
    {
        public virtual bool CanActivate(Ability ability, out TargetData targetData)
        {
            targetData = new TargetData();
            return ability.pawn.InMentalState is false;
        }

        public abstract float Priority(Ability ability);
    }
}
