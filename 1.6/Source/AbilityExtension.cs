using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace TaranMagicFramework
{
    public class AbilityClassLevelRange
    {
        public AbilityClassDef def;
        public IntRange levelRange;
    }
    public class AbilityTreesUnlock
    {
        public List<AbilityTreeDef> abilityTreeDefs;
        public IntRange countToUnlock;
    }

    public class AbilityToLearn
    {
        public int minHediffCount = 1;
        public AbilityDef ability;
        public int level = 0;
        public int upgradeTo;
    }

    [HotSwappable]
    public class AbilityExtension : DefModExtension
    {
        public List<GeneDef> doNotRemoveAbilitiesWithGenes;
        public List<HediffDef> doNotRemoveAbilitiesWithHediffs;
        public List<AbilityClassDef> abilityClasses;
        public List<AbilityClassLevelRange> abilityClassLevelRanges;
        public List<AbilityClassLevelRange> abilityClassSkillPointsGain;
        public List<AbilityTreeDef> abilityTrees;
        public List<AbilityToLearn> abilitiesToLearn;
        public List<AbilityDef> abilitiesToDisableWhenRemoved;
        public List<AbilityDef> abilitiesToRemoveWhenPartsRemoved;
        public AbilityTreesUnlock abilityTreesToUnlock;
        public List<GeneticTraitData> forcedTraits;
        public bool customTraitGeneration;
        public bool alwaysHitTarget;

        public static bool doNotRemoveAbilities;
        public void RemoveAbilities(Pawn pawn, Def source)
        {
            if (doNotRemoveAbilities)
            {
                return;
            }
            if (doNotRemoveAbilitiesWithGenes != null)
            {
                if (doNotRemoveAbilitiesWithGenes.Any(x => pawn.genes.HasActiveGene(x)))
                {
                    return;
                }
            }

            if (doNotRemoveAbilitiesWithHediffs != null)
            {
                if (doNotRemoveAbilitiesWithHediffs.Any(x => pawn.health.hediffSet.GetFirstHediffOfDef(x) != null))
                {
                    return;
                }
            }

            var comp = pawn.GetComp<CompAbilities>();
            if (source is TraitDef trait)
            {
                comp.abilitySourcesTraits.Remove(trait);
            }
            else if (source is GeneDef gene)
            {
                comp.abilitySourcesGenes.Remove(gene);
            }
            else if (source is HediffDef hediff)
            {
                comp.abilitySourcesHediffs.Remove(hediff);
            }
            else if (source is ThingDef thing)
            {
                comp.abilitySourcesThings.Remove(thing);
            }

            if (abilityClasses != null)
            {
                foreach (var abilityClassDef in abilityClasses)
                {
                    TMagicUtils.Message("Removing ability class: " + abilityClassDef, pawn);
                    comp.abilityClasses.Remove(abilityClassDef);
                }
            }

            if (abilityTrees != null)
            {
                foreach (var abilityClass in comp.AllUnlockedAbilityClasses)
                {
                    foreach (var tree in abilityTrees)
                    {
                        if (abilityClass.def.abilityTrees.Contains(tree))
                        {
                            abilityClass.UnlockedTrees.Remove(tree);
                        }
                    }
                }
            }

            if (abilitiesToLearn != null)
            {
                foreach (var abilityToLearn in abilitiesToLearn)
                {
                    foreach (var abilityClass in comp.AllUnlockedAbilityClasses)
                    {
                        foreach (var abilityTree in abilityClass.def.abilityTrees)
                        {
                            if (abilityTree.AllAbilities.Contains(abilityToLearn.ability))
                            {
                                var abilityLearned = abilityClass.GetLearnedAbility(abilityToLearn.ability);
                                if (abilityLearned != null)
                                {
                                    TMagicUtils.Message("2 Removing " + abilityLearned + " because of " + source, pawn);
                                    abilityClass.RemoveAbility(abilityLearned);
                                }
                            }
                        }
                    }
                }
            }

            if (abilitiesToDisableWhenRemoved != null)
            {
                if (source is HediffDef hediffDef)
                {
                    var hediff = pawn.health.hediffSet.GetFirstHediffOfDef(hediffDef);
                    if (hediff is null)
                    {
                        foreach (var abilityDef in abilitiesToDisableWhenRemoved)
                        {
                            var ability = pawn.GetAbility(abilityDef);
                            if (ability != null && ability.Active)
                            {
                                ability.End();
                            }
                        }
                    }
                }
            }
        }

        public static bool doNotUnlockAbilities;

        public void UnlockAbilities(Pawn pawn, Def source)
        {
            TMagicUtils.Message("START: UnlockAbilities: " + source, pawn);
            if (doNotUnlockAbilities)
            {
                TMagicUtils.Message("doNotUnlockAbilities: " + source, pawn);
                return;
            }

            var comp = pawn.GetComp<CompAbilities>();
            comp.preventCheck = true;
            try
            {
                UnlockAbilitiesInt(comp, pawn, source);
            }
            catch (Exception ex)
            {
                Log.Error("Error unlocking abilities for " + pawn + ": source: " + source + " error message: " + ex.ToString());
            }
            comp.preventCheck = false;
            comp.RecheckAbilities();
        }

        private void UnlockAbilitiesInt(CompAbilities comp, Pawn pawn, Def source)
        {
            TMagicUtils.Message("UnlockAbilitiesInt: " + source, pawn);
            if (forcedTraits != null)
            {
                for (int j = 0; j < forcedTraits.Count; j++)
                {
                    if (pawn.story.traits.HasTrait(forcedTraits[j].def, forcedTraits[j].degree) is false)
                    {
                        var t = new Trait(forcedTraits[j].def, forcedTraits[j].degree);
                        pawn.story.traits.GainTrait(t, suppressConflicts: true);
                    }
                }
            }

            if (abilityClasses != null)
            {
                TMagicUtils.Message("abilityClasses: " + string.Join(", ", abilityClasses.Select(x => x)), pawn);
                foreach (var abilityClassDef in abilityClasses)
                {
                    if (HasConflictingAbilityClass(comp, abilityClassDef))
                    {
                        continue;
                    }
                    if (!comp.abilityClasses.TryGetValue(abilityClassDef, out var abilityClass))
                    {
                        comp.abilityClasses[abilityClassDef] = abilityClass = comp.CreateAbilityClass(abilityClassDef, unlockClass: true);
                    }
                    if (!abilityClass.Unlocked)
                    {
                        abilityClass.Unlocked = true;
                        TMagicUtils.Message(abilityClass.def + " is unlocked", pawn);
                    }
                    TMagicUtils.Message("comp.AllUnlockedAbilityClasses: " + string.Join(", ", comp.AllUnlockedAbilityClasses.Select(x => x.def)), pawn);
                }
            }

            if (abilityClassLevelRanges != null)
            {
                foreach (var level in abilityClassLevelRanges)
                {
                    if (HasConflictingAbilityClass(comp, level.def))
                    {
                        continue;
                    }
                    if (!comp.abilityClasses.TryGetValue(level.def, out var abilityClass))
                    {
                        comp.abilityClasses[level.def] = abilityClass = comp.CreateAbilityClass(level.def);
                    }
                    if (abilityClass.Unlocked is false)
                    {
                        abilityClass.Unlocked = true;
                    }
                    abilityClass.SetLevel(level.levelRange.RandomInRange);
                }
            }
            if (abilityClassSkillPointsGain != null)
            {
                foreach (var skillPointGain in abilityClassSkillPointsGain)
                {
                    var abilityClass = comp.GetAbilityClass(skillPointGain.def);
                    if (abilityClass != null)
                    {
                        abilityClass.skillPoints += skillPointGain.levelRange.RandomInRange;
                    }
                }
            }

            if (abilityTrees != null)
            {
                TMagicUtils.Message("abilityTrees: " + string.Join(", ", abilityTrees.Select(x => x)), pawn);
                TMagicUtils.Message("2 comp.AllUnlockedAbilityClasses: " + string.Join(", ", comp.AllUnlockedAbilityClasses.Select(x => x.def)), pawn);
                foreach (var abilityClass in comp.AllUnlockedAbilityClasses)
                {
                    foreach (var tree in abilityTrees)
                    {
                        TMagicUtils.Message("Checking: " + tree + " for " + abilityClass?.def, pawn);
                        if (abilityClass.def.abilityTrees.Contains(tree))
                        {
                            if (abilityClass.TreeUnlocked(tree) is false)
                            {
                                abilityClass.UnlockTree(tree);
                                TMagicUtils.Message(tree + " is unlocked", pawn);
                            }
                        }
                    }
                }
            }

            if (abilityTreesToUnlock != null)
            {
                var countToUnlock = abilityTreesToUnlock.countToUnlock.RandomInRange;
                var treesToUnlock = abilityTreesToUnlock.abilityTreeDefs.InRandomOrder().Take(countToUnlock).ToList();
                foreach (var abilityClass in comp.AllUnlockedAbilityClasses)
                {
                    foreach (var tree in treesToUnlock)
                    {
                        if (abilityClass.def.abilityTrees.Contains(tree))
                        {
                            if (abilityClass.TreeUnlocked(tree) is false)
                            {
                                abilityClass.UnlockTree(tree);
                            }
                        }
                    }
                }
            }

            if (abilitiesToLearn != null)
            {
                foreach (var abilityToLearn in abilitiesToLearn)
                {
                    if (source is HediffDef hediff && abilityToLearn.minHediffCount > 1 && pawn.health.hediffSet.hediffs.Where(x => x.def == hediff).Count()
                        < abilityToLearn.minHediffCount)
                    {
                        continue;
                    }
                    TMagicUtils.Message("TryLearnAbility: " + abilityToLearn.ability, pawn);
                    TryLearnAbility(comp, abilityToLearn);
                }
            }
        }

        private static bool HasConflictingAbilityClass(CompAbilities comp, AbilityClassDef abilityClassDef)
        {
            if (abilityClassDef.exclusionTags != null)
            {
                foreach (var tag in abilityClassDef.exclusionTags)
                {
                    foreach (var existingClass in comp.abilityClasses.Keys)
                    {
                        if (abilityClassDef != existingClass && (existingClass.exclusionTags?.Contains(tag) ?? false))
                        {
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        private static void TryLearnAbility(CompAbilities comp, AbilityToLearn abilityToLearn)
        {
            foreach (var abilityClass in comp.AllUnlockedAbilityClasses)
            {
                foreach (var abilityTree in abilityClass.def.abilityTrees)
                {
                    if (abilityTree.AllAbilities.Contains(abilityToLearn.ability))
                    {
                        if (abilityClass.UnlockedTrees.Contains(abilityTree) is false)
                        {
                            abilityClass.UnlockTree(abilityTree);
                        }
                        var ability = abilityClass.GetLearnedAbility(abilityToLearn.ability);
                        if (ability is null)
                        {
                            ability = abilityClass.LearnAbility(abilityToLearn.ability, false, abilityToLearn.level);
                        }
                        if (abilityToLearn.upgradeTo > 0)
                        {
                            for (var i = 0; i < abilityToLearn.upgradeTo; i++)
                            {
                                if (ability.level < ability.def.abilityTiers.Count - 1)
                                {
                                    ability.ChangeLevel(ability.level + 1);
                                }
                            }
                        }
                        return;
                    }
                }
            }
        }
    }
}
