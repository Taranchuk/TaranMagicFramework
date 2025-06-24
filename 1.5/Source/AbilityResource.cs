using System.Linq;
using UnityEngine;
using Verse;

namespace TaranMagicFramework
{
    public class AbilityResource : IExposable, ILoadReferenceable
    {
        public Pawn pawn;
        public AbilityResourceDef def;
        public CompAbilities compAbilities;
        public int id;

        public float energy;
        public float EnergyPct => energy / MaxEnergy;
        public int MaxEnergy => (int)TMagicUtils.GetStatValueForStat(compAbilities, def.maxEnergyStat);
        public virtual int EnergyFillTickInterval => 60;
        public virtual void Tick()
        {
            if (pawn.IsHashIntervalTick(EnergyFillTickInterval))
            {
                var newEnergy = EnergyGainPerTick();
                newEnergy *= EnergyFillTickInterval;
                energy += newEnergy;
                var maxEnergy = MaxEnergy;
                if (energy > maxEnergy)
                {
                    energy = maxEnergy;
                }
                if (energy < 0)
                {
                    foreach (var ability in compAbilities.AllLearnedAbilitiesWithResource(def))
                    {
                        if (ability.Active)
                        {
                            ability.End();
                        }
                    }
                }
                energy = Mathf.Max(0, energy);
            }
        }

        public virtual float EnergyGainPerTick()
        {
            float value = 0f;
            value += BaseEnergyGainPerTick;
            foreach (var classData in compAbilities.abilityClasses.Where(x => x.Key.abilityResource == def))
            {
                foreach (var ability in classData.Value.LearnedAbilities)
                {
                    if (ability.Active && ability.ResourceRegenRate.HasValue)
                    {
                        value += ability.ResourceRegenRate.Value;
                    }
                }
            }
            if (def.energyRegenMultiplierStat != null)
            {
                value *= TMagicUtils.GetStatValueForStat(compAbilities, def.energyRegenMultiplierStat);
            }
            return value;
        }
        protected virtual float BaseEnergyGainPerTick => def.baseEnergyGainPerTick;

        public void Init(CompAbilities compAbilities, AbilityResourceDef def, Pawn pawn)
        {
            this.compAbilities = compAbilities;
            this.def = def;
            this.pawn = pawn;
        }
        public void ExposeData()
        {
            Scribe_Defs.Look(ref def, "def");
            Scribe_References.Look(ref pawn, "pawn");
            Scribe_Values.Look(ref energy, "energy");
            Scribe_Values.Look(ref id, "id");
        }

        public string GetUniqueLoadID()
        {
            return "TMF_AbilityResource" + id;
        }
    }
}
