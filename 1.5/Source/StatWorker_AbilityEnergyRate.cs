using RimWorld;
using Verse;

namespace TaranMagicFramework
{
    public class StatWorker_AbilityEnergyRate : StatWorker
    {
        public override void FinalizeValue(StatRequest req, ref float val, bool applyPostProcess)
        {
            var comp = TMagicUtils.staticCompForStat ?? req.Thing?.TryGetComp<CompAbilities>();
            if (comp != null)
            {
                var extension = stat.GetModExtension<StatDefExtension>();
                foreach (var ability in comp.AllLearnedAbilitiesWithResource(extension.abilityResource))
                {
                    if (ability.Active && ability.AbilityTier.energyRateOffset != 0)
                    {
                        val += ability.AbilityTier.energyRateOffset;
                    }
                }
            }
            base.FinalizeValue(req, ref val, applyPostProcess);
        }
    }
}
