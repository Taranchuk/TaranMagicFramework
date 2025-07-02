using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace TaranMagicFramework
{
    public class Verb_ShootAbility : Verb_Shoot
    {
        public Ability ability;
        public Mote_Animation warmupAnimation;
        public Mote_Animation burstAnimation;
        public VerbPropertiesAbility VerbPropsAbility => verbProps as VerbPropertiesAbility;

        public static Verb_ShootAbility shooting;
        public override bool Available()
        {
            return base.Available() && (ability.Active || ability.CanBeActivated(ability.EnergyCost, out _, 
                canBeActivatedValidator: ability.CanBeActivatedValidator()));
        }

        public override void OrderForceTarget(LocalTargetInfo target)
        {
            Job job2 = GetVerbJob(target);
            CasterPawn.jobs.TryTakeOrderedJob(job2, JobTag.Misc);
        }

        public Job GetVerbJob(LocalTargetInfo target)
        {
            Job job2 = JobMaker.MakeJob(JobDefOf.UseVerbOnThingStatic);
            job2.verbToUse = this;
            job2.targetA = target;
            return job2;
        }

        [HarmonyPatch(typeof(Verb), "VerbTick")]
        public static class Verb_VerbTick_Patch
        {
            public static void Postfix(Verb __instance)
            {
                if (__instance is Verb_ShootAbility verb_ShootAbility)
                {
                    verb_ShootAbility.VerbTickPostfix();
                }
            }
        }

        public void VerbTickPostfix()
        {
            if (burstAnimation != null && state != VerbState.Bursting)
            {
                burstAnimation.endLoop = true;
            }
            if (!WarmingUp && state == VerbState.Idle && ability.Active)
            {
                ability.End();
            }
        }

        public override void WarmupComplete()
        {
            base.WarmupComplete();
            if (state == VerbState.Bursting)
            {
                if (VerbPropsAbility.burstAnimation != null && burstAnimation is null)
                {
                    burstAnimation = ThingMaker.MakeThing(VerbPropsAbility.burstAnimation) as Mote_Animation;
                    burstAnimation.exactPosition = caster.Position.ToVector3Shifted();
                    burstAnimation.Scale = 1f;
                    if (VerbPropsAbility.burstAnimation.rotatable)
                    {
                        Vector3 vec = CurrentTarget.CenterVector3 - Caster.DrawPos;
                        vec.y = 0f;
                        vec.Normalize();
                        burstAnimation.angle = vec.ToAngleFlat();
                    }

                    if (VerbPropsAbility.burstAnimation.angleOffset != 0)
                    {
                        if (burstAnimation.angle > 20f && burstAnimation.angle < 160f)
                        {
                            burstAnimation.angle += VerbPropsAbility.burstAnimation.angleOffset;
                        }
                        else if (burstAnimation.angle > 200f && burstAnimation.angle < 340f)
                        {
                            burstAnimation.angle -= 180f;
                            burstAnimation.angle -= VerbPropsAbility.burstAnimation.angleOffset;
                        }
                        else
                        {
                            burstAnimation.angle += VerbPropsAbility.burstAnimation.angleOffset;
                        }
                    }


                    burstAnimation.Attach(caster, Vector3.zero);
                    GenSpawn.Spawn(burstAnimation, caster.Position, caster.Map);
                }
            }
            else if (burstAnimation != null)
            {
                burstAnimation.endLoop = true;
            }
        }

        public override bool TryCastShot()
        {
            shooting = this;
            var result = (ability.abilityResource is null || ability.abilityResource.energy >= ability.EnergyCost) && base.TryCastShot();
            shooting = null;
            return result;
        }

        [HarmonyPatch(typeof(Verb), "TryCastNextBurstShot")]
        public static class VerbPatch_ShootAbility
        {
            public static void Postfix(Verb __instance)
            {
                if (__instance is Verb_ShootAbility shootAbility)
                {
                    if (shootAbility.ability.abilityResource != null)
                    {
                        shootAbility.ability.ConsumeEnergy(shootAbility.ability.EnergyCost);
                        if (shootAbility.ability.abilityResource.energy < shootAbility.ability.EnergyCost)
                        {
                            __instance.state = VerbState.Idle;
                        }
                    }
                }
            }
        }

        public override bool TryStartCastOn(LocalTargetInfo castTarg, LocalTargetInfo destTarg, bool surpriseAttack = false, bool canHitNonTargetPawns = true, bool preventFriendlyFire = false, bool nonInterruptingSelfCast = false)
        {
            if (VerbPropsAbility.warmupAnimation != null)
            {
                if (caster == null)
                {
                    Log.Error("Verb " + GetUniqueLoadID() + " needs caster to work (possibly lost during saving/loading).");
                    return false;
                }
                if (!caster.Spawned)
                {
                    return false;
                }
                if (state == VerbState.Bursting || !CanHitTarget(castTarg))
                {
                    return false;
                }
                if (CausesTimeSlowdown(castTarg))
                {
                    Find.TickManager.slower.SignalForceNormalSpeed();
                }
                this.surpriseAttack = surpriseAttack;
                canHitNonTargetPawnsNow = canHitNonTargetPawns;
                this.preventFriendlyFire = preventFriendlyFire;
                currentTarget = castTarg;
                currentDestination = destTarg;
                if (CasterIsPawn && verbProps.warmupTime > 0f)
                {
                    if (!TryFindShootLineFromTo(caster.Position, castTarg, out var resultingLine))
                    {
                        return false;
                    }
                    CasterPawn.Drawer.Notify_WarmingCastAlongLine(resultingLine, caster.Position);
                    float statValue = CasterPawn.GetStatValue(StatDefOf.AimingDelayFactor);
                    int ticks = (verbProps.warmupTime * statValue).SecondsToTicks();
                    CasterPawn.stances.SetStance(new Stance_WarmupAbility(VerbPropsAbility, CasterPawn, ticks, castTarg, this));
                    if (verbProps.stunTargetOnCastStart && castTarg.Pawn != null)
                    {
                        castTarg.Pawn.stances.stunner.StunFor(ticks, null, addBattleLog: false);
                    }
                }
                else
                {
                    WarmupComplete();
                }
                ability.Start(consumeEnergy: false);
                return true;
            }
            bool result = base.TryStartCastOn(castTarg, destTarg, surpriseAttack, canHitNonTargetPawns, preventFriendlyFire, nonInterruptingSelfCast); ;
            if (result)
            {
                ability.Start(consumeEnergy: false);
            }
            return result;
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_References.Look(ref ability, "ability");
        }
    }
}
