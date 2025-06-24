using RimWorld;
using System.Linq;
using Verse;

namespace TaranMagicFramework
{
    public class StatWorker_MaxAbilityEnergy : StatWorker
    {
        public override void FinalizeValue(StatRequest req, ref float val, bool applyPostProcess)
        {
            var comp = TMagicUtils.staticCompForStat ?? req.Thing?.TryGetComp<CompAbilities>();
            var extension = stat.GetModExtension<StatDefExtension>();
            foreach (var ability in comp.AllLearnedAbilitiesWithResource(extension.abilityResource))
            {
                if (ability.MaxEnergyOffset != 0)
                {
                    val += ability.MaxEnergyOffset;
                }
            }
            foreach (var abilityClass in comp.AllUnlockedAbilityClassesWithResource(extension.abilityResource))
            {
                if (abilityClass.def.maxEnergyOffsetPerLevel != 0)
                {
                    val += abilityClass.def.maxEnergyOffsetPerLevel * abilityClass.level;
                }
            }
            base.FinalizeValue(req, ref val, applyPostProcess);
        }
    }
}
