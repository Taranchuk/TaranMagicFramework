using UnityEngine;
using Verse;

namespace TaranMagicFramework
{
    [HotSwappable]
    public class Projectile_Explosive_Ability : Projectile_Explosive
    {
        public Ability ability;

        public override void Launch(Thing launcher, Vector3 origin, LocalTargetInfo usedTarget, LocalTargetInfo intendedTarget, ProjectileHitFlags hitFlags, bool preventFriendlyFire = false, Thing equipment = null, ThingDef targetCoverDef = null)
        {
            if (Verb_ShootAbility.shooting is not null)
            {
                origin += Verb_ShootAbility.shooting.VerbPropsAbility.warmupAnimationOffset.RotatedBy(launcher.Rotation);
            }
            base.Launch(launcher, origin, usedTarget, intendedTarget, hitFlags, preventFriendlyFire, equipment, targetCoverDef);
        }

        public override void Impact(Thing hitThing, bool blockedByShield = false)
        {
            if (hitThing is null)
            {
                var thing = Position.GetThingList(Map).FirstOrDefault(x => x == intendedTarget.Thing);
                if (thing != null)
                {
                    hitThing = thing;
                }
            }

            if (hitThing != intendedTarget.Thing)
            {
                foreach (var thing in GenRadial.RadialDistinctThingsAround(Position, Map, 3f, true))
                {
                    if (thing == intendedTarget.Thing)
                    {
                        if (Vector3.Distance(thing.DrawPos.Yto0(), ExactPosition.Yto0()) <= 0.5f)
                        {
                            hitThing = thing;
                        }
                    }
                }
            }
            base.Impact(hitThing, blockedByShield);
        }
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_References.Look(ref ability, "ability");
        }
    }
}
