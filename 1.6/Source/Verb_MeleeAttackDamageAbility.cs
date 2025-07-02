using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

namespace TaranMagicFramework
{
    public class Verb_MeleeAttackDamageAbility : Verb_MeleeAttackDamage
    {
        public Ability ability;
        public VerbPropertiesAbility VerbPropsAbility => verbProps as VerbPropertiesAbility;
        public override bool Available()
        {
            return base.Available() && (ability.Active || ability.CanBeActivated(ability.EnergyCost, out _, 
                canBeActivatedValidator: ability.CanBeActivatedValidator()));
        }

        public override DamageWorker.DamageResult ApplyMeleeDamageToTarget(LocalTargetInfo target)
        {
            if (VerbPropsAbility.animationOnTargetWhenHit != null && target.HasThing)
            {
                var overlay = ability.MakeAnimation(VerbPropsAbility.animationOnTargetWhenHit);
                overlay.Attach(target.Thing);
                GenSpawn.Spawn(overlay, target.Thing.Position, target.Thing.Map);
            }
            return base.ApplyMeleeDamageToTarget(target);
        }

        public override void OrderForceTarget(LocalTargetInfo target)
        {
            Job job = GetVerbJob(target);
            CasterPawn.jobs.TryTakeOrderedJob(job, JobTag.Misc);
        }

        public Job GetVerbJob(LocalTargetInfo target)
        {
            Job job = JobMaker.MakeJob(JobDefOf.AttackMelee, target);
            job.playerForced = true;
            job.verbToUse = this;
            job.maxNumMeleeAttacks = VerbPropsAbility.maxNumMeleeAttacks;
            Pawn pawn = target.Thing as Pawn;
            if (pawn != null)
            {
                job.killIncappedTarget = pawn.Downed;
            }
            ability.Start();
            return job;
        }

        public override bool TryCastShot()
        {
            surpriseAttack = true;
            return (ability.abilityResource is null || ability.abilityResource.energy >= ability.EnergyCost) && base.TryCastShot();
        }
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_References.Look(ref ability, "ability");
        }
    }
}
