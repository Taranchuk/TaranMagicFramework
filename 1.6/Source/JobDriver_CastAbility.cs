using System.Collections.Generic;
using Verse;
using Verse.AI;

namespace TaranMagicFramework
{

    public class JobDriver_CastAbility : JobDriver
    {
        public override string GetReport()
        {
            var ability = Job.ability;
            if (ability.curTarget != null && ability.curTarget.HasThing)
            {
                return "TMF.CastingAbilityOn".Translate(ability.def.label, ability.curTarget.Thing.Label);
            }
            return "TMF.CastingAbility".Translate(ability.def.label);
        }

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            var ability = Job.ability;
            if (ability.curTarget != null)
            {
                if (pawn.CanReserve(ability.curTarget))
                {
                    return pawn.Reserve(ability.curTarget, Job);
                }
            }
            return true;
        }
        public JobAbility Job => job as JobAbility;
        public override IEnumerable<Toil> MakeNewToils()
        {
            if (Job.ability.CastTicks > 0)
            {
                yield return Toils_General.Wait(Job.ability.CastTicks, TargetIndex.A)
                    .WithProgressBarToilDelay(TargetIndex.C).FailOn(() =>
                    {
                        var targetParameters = Job.ability.TargetingParameters;
                        if (targetParameters?.validator != null && Job.targetA.IsValid)
                        {
                            if (Job.targetA.HasThing)
                            {
                                if (targetParameters.validator(Job.targetA.ToTargetInfo(Job.targetA.Thing.Map)) is false)
                                {
                                    return true;
                                }
                            }
                            else
                            {
                                if (targetParameters.validator(Job.targetA.ToTargetInfo(pawn.Map)) is false)
                                {
                                    return true;
                                }
                            }
                        }
                        return false;
                    });
            }

            yield return Toils_General.Do(delegate
            {
                Job.ability.Start();
            });
        }
    }
}
