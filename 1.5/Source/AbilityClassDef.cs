using RimWorld;
using System;
using System.Collections.Generic;
using Verse;

namespace TaranMagicFramework
{
    public class AbilityClassDef : Def
    {
        public Type abilityClassType = typeof(AbilityClass);
        public List<AbilityTreeDef> abilityTrees;
        public int maxLevel = int.MaxValue;
        public bool usesSkillPointSystem;
        public bool usesLevelSystem = true;
        public bool usesXPSystem = true;
        public bool unlockedByDefault = true;
        public AbilityResourceDef abilityResource;
        public float maxEnergyOffsetPerLevel;
        public List<StatBonus> statBonusesPerLevel;
        public StatDef castTimeMultiplierStat;
        public StatDef cooldownTicksMultiplierStat;
        public StatDef damageMultiplierStat;
        public StatDef damageResistanceMultiplierStat;
        public StatDef xpGainMultiplierStat;
        public List<string> exclusionTags;
    }
}
