using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace TaranMagicFramework
{
    [HotSwappable]
    [StaticConstructorOnStartup]
    public static class TMagicUtils
    {
        public static bool debug => false;

        public static void Message(string msg, Pawn pawn = null)
        {
            if (debug)
            {
                if (pawn != null)
                {
                    msg += " - pawn: " + pawn.NameFullColored;
                }
                Log.Message(msg);
                Log.ResetMessageCount();
            }
        }
        private static Dictionary<AbilityTabDef, InspectTabBase> sharedInstances = new Dictionary<AbilityTabDef, InspectTabBase>();
        static TMagicUtils()
        {
            foreach (var statDef in DefDatabase<StatDef>.AllDefs)
            {
                if (statDef.category?.GetModExtension<StatDefExtension>() != null)
                {
                    statDef.immutable = false;
                }
            }
            new Harmony("TaranMagicFrameworkMod").PatchAll();
            foreach (var race in DefDatabase<ThingDef>.AllDefs.Where(x => (x.race?.Humanlike ?? false) && x.IsCorpse is false))
            {
                race.comps?.Add(new CompProperties_Abilities());
                foreach (var tabDef in DefDatabase<AbilityTabDef>.AllDefs)
                {
                    race.inspectorTabs?.Add(tabDef.tabClass);
                    race.inspectorTabsResolved?.Add(GetSharedInstance(tabDef));
                }
            }
        }

        public static bool HasActiveGene(this Pawn pawn, GeneDef geneDef)
        {
            if (pawn?.genes == null) return false;

            foreach (var gene in pawn.genes.GenesListForReading)
            {
                if (gene.def != geneDef) continue;

                if (gene.Active) return true;

                var ext = gene.def.GetModExtension<AbilityExtension>();
                if (ext != null && ext.countAsActive) return true;
            }

            return false;
        }

        public static void AddHediffWithoutAbilityExtension(this Pawn pawn, Hediff hediff)
        {
            AbilityExtension.doNotUnlockAbilities = true;
            try
            {
                pawn.health.AddHediff(hediff);
            }
            catch { }
            AbilityExtension.doNotUnlockAbilities = false;
        }

        public static void RemoveHediffWithoutAbilityExtension(this Pawn pawn, Hediff hediff)
        {
            AbilityExtension.doNotRemoveAbilities = true;
            try
            {
                pawn.health.RemoveHediff(hediff);
            }
            catch { }
            AbilityExtension.doNotRemoveAbilities = false;
        }

        public static List<AbilityExtension> GetAbilityExtensions(this Def def)
        {
            if (def?.modExtensions is null)
            {
                return new List<AbilityExtension>();
            }
            var extensions = new List<AbilityExtension>();
            foreach (var extension in def.modExtensions)
            {
                if (extension is AbilityExtension abilityExtension)
                {
                    extensions.Add(abilityExtension);
                }
            }
            return extensions;
        }

        public static void UnlockOrRecheckAbilities(this Def def, Pawn pawn)
        {
            var comp = pawn.GetComp<CompAbilities>();
            if (comp is null) return;
            var extensions = def.GetAbilityExtensions();
            if (extensions.Any())
            {
                if (comp.CheckIfHasSource(def) is false)
                {
                    foreach (var extension in extensions)
                    {
                        extension.UnlockAbilities(pawn, def);
                        Message("Unlocking abilities for " + pawn.NameFullColored + " - " + def.defName, pawn);
                    }
                }
            }
            else
            {
                comp.RecheckAbilities();
            }
        }

        public static void TryRemoveAbilities(this Def def, Pawn pawn)
        {
            var extensions = def.GetAbilityExtensions();
            if (extensions.Any())
            {
                foreach (var extension in extensions)
                {
                    extension.RemoveAbilities(pawn, def);
                    Message("Removing abilities for " + pawn.NameFullColored + " - " + def.defName, pawn);
                }
            }
            else
            {
                var comp = pawn.GetComp<CompAbilities>();
                comp?.RecheckAbilities();
            }
        }

        public static HashSet<Ability> GetAllAbilities(this AbilityDef abilityDef)
        {
            if (Ability.allAbilitiesByDef.TryGetValue(abilityDef, out var list))
            {
                return list;
            }
            return new HashSet<Ability>();
        }

        public static InspectTabBase GetSharedInstance(AbilityTabDef tabDef)
        {
            if (sharedInstances.TryGetValue(tabDef, out var value))
            {
                return value;
            }
            value = (InspectTabBase)Activator.CreateInstance(tabDef.tabClass, new object[] { tabDef });
            sharedInstances.Add(tabDef, value);
            return value;
        }

        public static CompAbilities staticCompForStat;
        public static float GetStatValueForStat(CompAbilities comp, StatDef stat)
        {
            staticCompForStat = comp;
            var result = comp.parent.GetStatValue(stat, cacheStaleAfterTicks: 0);
            staticCompForStat = null;
            return result;
        }

        public static List<AbilityDef> AllAvailableAbilities(this Pawn pawn)
        {
            var abilities = new List<AbilityDef>();
            var abilityTrees = pawn.GetAllAbilityTrees();
            if (abilityTrees != null)
            {
                foreach (var abilityTree in abilityTrees)
                {
                    abilities.AddRange(abilityTree.AllAbilities);
                }
            }
            return abilities;
        }

        public static IEnumerable<AbilityClass> GetUnlockedAbilityClasses(this Pawn pawn)
        {
            var comp = pawn.GetComp<CompAbilities>();
            if (comp?.abilityClasses != null)
            {
                foreach (var kvp in comp.abilityClasses)
                {
                    if (kvp.Value.Unlocked)
                    {
                        yield return kvp.Value;
                    }
                }
            }
        }
        public static IEnumerable<AbilityClassDef> GetAvailableAbilityClasses(this Pawn pawn)
        {
            return GetAvailableAbilityClassesInner(pawn).Distinct();
        }

        private static IEnumerable<AbilityClassDef> GetAvailableAbilityClassesInner(Pawn pawn)
        {
            if (pawn.story?.traits != null)
            {
                foreach (var trait in pawn.story.traits.allTraits)
                {
                    var extension = trait.def.GetModExtension<AbilityExtension>();
                    if (extension != null && extension.abilityClasses != null)
                    {
                        foreach (var abilityClass in extension.abilityClasses)
                        {
                            yield return abilityClass;
                        }
                    }
                }
            }

            foreach (var hediff in pawn.health.hediffSet.hediffs)
            {
                var extension = hediff.def.GetModExtension<AbilityExtension>();
                if (extension != null && extension.abilityClasses != null)
                {
                    foreach (var abilityClass in extension.abilityClasses)
                    {
                        yield return abilityClass;
                    }
                }
            }
            if (pawn.genes != null)
            {
                foreach (var gene in pawn.genes.GenesListForReading)
                {
                    var extension = gene.def.GetModExtension<AbilityExtension>();
                    if (extension != null && extension.abilityClasses != null)
                    {
                        foreach (var abilityClass in extension.abilityClasses)
                        {
                            yield return abilityClass;
                        }
                    }
                }
            }

            var comp = pawn.GetComp<CompAbilities>();
            if (comp?.abilityClasses != null)
            {
                foreach (var abilityClass in comp.abilityClasses.Keys)
                {
                    yield return abilityClass;
                }
            }
        }

        public static IEnumerable<AbilityTreeDef> GetAllAbilityTrees(this Pawn pawn)
        {
            foreach (var abilityClass in pawn.GetUnlockedAbilityClasses())
            {
                foreach (var abilityTree in abilityClass.UnlockedTrees)
                {
                    yield return abilityTree;
                }
            }
        }

        public static AbilityTreeDef GetUnlockedTree(this Pawn pawn, AbilityTreeDef abilityTreeDef)
        {
            foreach (var abilityClass in pawn.GetUnlockedAbilityClasses())
            {
                foreach (var abilityTree in abilityClass.UnlockedTrees)
                {
                    if (abilityTreeDef == abilityTree)
                    {
                        return abilityTree;
                    }
                }
            }
            return null;
        }
        public static void ResolveAllGraphicsSafely(this Pawn pawn)
        {
            if (pawn != null)
            {
                LongEventHandler.ExecuteWhenFinished(delegate
                {
                    pawn.Drawer.renderer.SetAllGraphicsDirty();
                });
            }
        }

        public static bool IsAbilityUser(this Pawn pawn)
        {
            return pawn.GetAvailableAbilityClasses().Any();
        }

        public static Ability GetAbility(this Pawn pawn, AbilityDef abilityDef)
        {
            foreach (var comp in pawn.AllComps.OfType<CompAbilities>())
            {
                foreach (var abilityClass in comp.abilityClasses.Values)
                {
                    var ability = abilityClass.GetLearnedAbility(abilityDef);
                    if (ability != null)
                    {
                        return ability;
                    }
                }
            }
            return null;
        }

        public static bool HasActiveAbility(this Pawn pawn, AbilityDef abilityDef)
        {
            foreach (var comp in pawn.AllComps.OfType<CompAbilities>())
            {
                foreach (var abilityClass in comp.abilityClasses.Values)
                {
                    var ability = abilityClass.GetLearnedAbility(abilityDef);
                    if (ability != null && ability.Active)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        public static JobAbility MakeJobAbility(Ability ability, LocalTargetInfo targetA)
        {
            JobAbility job = MakeJobAbility(ability);
            job.targetA = targetA;
            return job;
        }
        public static JobAbility MakeJobAbility(Ability ability)
        {
            JobAbility job = new JobAbility();
            job.loadID = Find.UniqueIDsManager.GetNextJobID();
            job.ability = ability;
            job.def = ability.AbilityTier.jobDef;
            return job;
        }

        public static VerbPropertiesAbility VerbPropsAbility(this Verb verb)
        {
            if (verb is Verb_MeleeAttackDamageAbility verbMelee)
            {
                return verbMelee.VerbPropsAbility;
            }
            else if (verb is Verb_ShootAbility verbShoot)
            {
                return verbShoot.VerbPropsAbility;
            }
            throw new NotImplementedException();
        }
    }
}
