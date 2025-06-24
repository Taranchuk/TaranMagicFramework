using RimWorld;
using System;
using Verse;

namespace TaranMagicFramework
{
    public class AbilityResourceDef : Def
    {
        public Type abilityResourceType = typeof(AbilityResource);
        public float baseEnergyGainPerTick;
        public StatDef maxEnergyStat;
        public StatDef energyRegenMultiplierStat;
        public StatDef energyUsageMultiplierStat;
    }
}
