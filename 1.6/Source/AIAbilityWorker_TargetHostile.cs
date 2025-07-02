using RimWorld;
using System.Linq;
using Verse;

namespace TaranMagicFramework
{
    public class AIAbilityWorker_TargetHostile : AIAbilityWorker
    {
        public override bool CanActivate(Ability ability, out TargetData targetData)
        {
            if (base.CanActivate(ability, out targetData))
            {
                if (ability.Verbs.Any())
                {
                    var verbs = ability.Verbs.InRandomOrder().ToList();
                    foreach (var verb in verbs)
                    {
                        foreach (var target in ability.GetHostileTargets())
                        {
                            if (verb.Available() && verb.CanHitTarget(target))
                            {
                                var projectile = verb.GetProjectile();
                                if (projectile != null && projectile.projectile.explosionRadius > 0)
                                {
                                    var friendlyPawns = GenRadial.RadialDistinctThingsAround(target.Position, ability.pawn.Map,
                                        projectile.projectile.explosionRadius, true).OfType<Pawn>().Where(x => x.Faction != null
                                        && x.HostileTo(ability.pawn) is false);
                                    if (friendlyPawns.Any())
                                    {
                                        continue;
                                    }
                                }
                                targetData = new(verb, target);
                                return true;
                            }
                        }
                    }
                }
                else
                {
                    var targetParameters = ability.TargetingParameters;
                    if (targetParameters != null)
                    {
                        foreach (var target in ability.GetHostileTargets())
                        {
                            if (target != null && targetParameters.CanTarget(target))
                            {
                                targetData = new(null, target);
                                return true;
                            }
                        }
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
