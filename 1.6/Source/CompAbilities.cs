using RimWorld;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using UnityEngine;
using Verse;

namespace TaranMagicFramework
{
    public class CompProperties_Abilities : CompProperties
    {
        public CompProperties_Abilities()
        {
            compClass = typeof(CompAbilities);
        }
    }

    [HotSwappable]
    public class CompAbilities : ThingComp
    {
        public List<TraitDef> abilitySourcesTraits = new();
        public List<GeneDef> abilitySourcesGenes = new();
        public List<HediffDef> abilitySourcesHediffs = new();
        public List<ThingDef> abilitySourcesThings = new();
        public Pawn Pawn => parent as Pawn;
        public Dictionary<AbilityClassDef, AbilityClass> abilityClasses = new();
        public Dictionary<AbilityResourceDef, AbilityResource> abilityResources = new();
        public CompProperties_Abilities Props => props as CompProperties_Abilities;
        public List<Ability> AllLearnedAbilities => abilityClasses.Values.SelectMany(x => x.LearnedAbilities).ToList();
        public AbilityClass GetAbilityClass(AbilityClassDef def)
        {
            if (abilityClasses.TryGetValue(def, out var abilityClass))
            {
                return abilityClass;
            }
            return null;
        }
        public AbilityClass CreateAbilityClass(AbilityClassDef def, bool unlockClass = false)
        {
            var abilityClass = Activator.CreateInstance(def.abilityClassType) as AbilityClass;
            abilityClass.id = GameComponent_MagicFramework.Instance.GetNextAbilityClassID();
            abilityClasses[def] = abilityClass;
            abilityClass.Init(this, def, Pawn, GetAbilityResource(def.abilityResource), unlockClass);
            if (abilityClass.abilityResource != null)
            {
                abilityClass.abilityResource.energy = abilityClass.abilityResource.MaxEnergy;
            }
            return abilityClass;
        }

        public List<Ability> AllLearnedAbilitiesWithResource(AbilityResourceDef resource)
        {
            return abilityClasses.Where(x => x.Key.abilityResource == resource).SelectMany(x => x.Value.LearnedAbilities).ToList();
        }
        public List<AbilityClass> AllUnlockedAbilityClasses => abilityClasses.Values.Where(x => x.Unlocked).ToList();
        public List<AbilityClass> AllUnlockedAbilityClassesWithResource(AbilityResourceDef resource)
        {
            return abilityClasses.Values.Where(x => x.def.abilityResource == resource && x.Unlocked).ToList();
        }

        public AbilityResource GetAbilityResource(AbilityResourceDef def)
        {
            if (def is null) return null;
            if (!abilityResources.TryGetValue(def, out var abilityResource))
            {
                abilityResource = CreateAbilityResource(def);
            }
            return abilityResource;
        }

        public AbilityResource CreateAbilityResource(AbilityResourceDef def)
        {
            var abilityResource = Activator.CreateInstance(def.abilityResourceType) as AbilityResource;
            abilityResource.Init(this, def, Pawn);
            abilityResource.id = GameComponent_MagicFramework.Instance.GetNextAbilityResourceID();
            abilityResources[def] = abilityResource;
            return abilityResource;
        }

        public override void CompTick()
        {
            base.CompTick();
            foreach (var resource in abilityResources.Values)
            {
                resource.Tick();
            }

            foreach (var abilityClass in abilityClasses.Values)
            {
                abilityClass.Tick();
            }

            if (Find.TickManager.TicksGame % 2500 == 0)
            {
                RecheckAbilities();
            }
        }

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            if (Pawn.Faction == Faction.OfPlayer && Pawn.IsAbilityUser())
            {
                foreach (var abilityResource in abilityResources.Values)
                {
                    yield return new Gizmo_EnergyStatus
                    {
                        abilityResource = abilityResource
                    };
                    if (DebugSettings.godMode)
                    {
                        yield return new Command_Action
                        {
                            defaultLabel = abilityResource.def.label + " - DEV: Fill",
                            action = delegate
                            {
                                abilityResource.energy = abilityResource.MaxEnergy;
                            }
                        };
                        yield return new Command_Action
                        {
                            defaultLabel = abilityResource.def.label + " - DEV: Empty",
                            action = delegate
                            {
                                abilityResource.energy = 0;
                            }
                        };
                    }
                }
                if (DebugSettings.godMode)
                {
                    yield return new Command_Action
                    {
                        defaultLabel = "DEV: End all animations",
                        action = delegate
                        {
                            var things = parent.Map.listerThings.AllThings.OfType<Mote_Animation>().ToList();
                            foreach (var thing in things)
                            {
                                thing.Destroy();
                            }
                        }
                    };
                }

                foreach (var abilityClass in abilityClasses.Values.OrderBy(x => x.def.index))
                {
                    if (abilityClass.Unlocked)
                    {
                        foreach (var gizmo in abilityClass.AbilityGizmos())
                        {
                            GizmoPostProcess(gizmo);
                            yield return gizmo;
                        }

                        if (DebugSettings.godMode)
                        {
                            if (abilityClass.def.usesXPSystem)
                            {
                                yield return new Command_Action
                                {
                                    defaultLabel = abilityClass.def.label + " - DEV: Gain 10 xp",
                                    action = delegate
                                    {
                                        abilityClass.GainXP(10f);
                                    }
                                };
                                yield return new Command_Action
                                {
                                    defaultLabel = abilityClass.def.label + " - DEV: Gain 100 xp",
                                    action = delegate
                                    {
                                        abilityClass.GainXP(100f);
                                    }
                                };
                                yield return new Command_Action
                                {
                                    defaultLabel = abilityClass.def.label + " - DEV: Gain 1000 xp",
                                    action = delegate
                                    {
                                        abilityClass.GainXP(1000f);
                                    }
                                };
                            }

                            yield return new Command_Action
                            {
                                defaultLabel = abilityClass.def.label + " - DEV: Learn all abilities",
                                action = delegate
                                {
                                    foreach (var abilityTree in abilityClass.def.abilityTrees)
                                    {

                                        foreach (var ability in abilityTree.AllAbilities)
                                        {
                                            if (abilityClass.Learned(ability) is false || abilityClass.FullyLearned(ability) is false)
                                            {
                                                if (!abilityClass.TreeUnlocked(abilityTree))
                                                {
                                                    abilityClass.UnlockTree(abilityTree);
                                                }
                                                abilityClass.LearnAbility(ability, false, ability.abilityTiers.Count - 1);
                                            }
                                        }
                                    }
                                }
                            };

                            yield return new Command_Action
                            {
                                defaultLabel = abilityClass.def.label + " - DEV: end all abilities",
                                action = delegate
                                {
                                    foreach (var ability in abilityClass.LearnedAbilities)
                                    {
                                        if (ability.Active)
                                        {
                                            ability.End();
                                        }
                                    }
                                }
                            };

                            yield return new Command_Action
                            {
                                defaultLabel = abilityClass.def.label + " - DEV: reset all cooldowns",
                                action = delegate
                                {
                                    foreach (var ability in abilityClass.LearnedAbilities)
                                    {
                                        ability.SetCooldown(0);
                                    }
                                }
                            };
                        }
                    }
                    else
                    {
                        if (DebugSettings.godMode)
                        {
                            yield return new Command_Action
                            {
                                defaultLabel = abilityClass.def.label + " - DEV: Unlock class",
                                action = delegate
                                {
                                    abilityClass.Unlocked = true;
                                }
                            };
                        }
                    }
                }
            }
        }

        private void GizmoPostProcess(Gizmo gizmo)
        {
        }

        public override void PostDeSpawn(Map map, DestroyMode mode = DestroyMode.Vanish)
        {
            base.PostDeSpawn(map, mode);
            foreach (var ability in AllLearnedAbilities)
            {
                ability.PostDeSpawn(map);
            }
        }

        public override void PostDestroy(DestroyMode mode, Map previousMap)
        {
            base.PostDestroy(mode, previousMap);
            foreach (var ability in AllLearnedAbilities)
            {
                ability.PostDestroy(mode, previousMap);
            }
        }

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            RecheckAbilities();
            foreach (var ability in AllLearnedAbilities)
            {
                ability.PostSpawnSetup(respawningAfterLoad);
            }
        }

        public bool preventCheck;
        public void RecheckAbilities()
        {
            if (preventCheck)
            {
                return;
            }
            TryAutoRemoveAbilities();
            TryAutoGainAbilities();
        }

        private void TryAutoRemoveAbilities()
        {
            var allSources = new List<Def>();
            for (int i = abilitySourcesTraits.Count - 1; i >= 0; i--)
            {
                var source = abilitySourcesTraits[i];
                if (Pawn.story.traits.HasTrait(source) is false)
                {
                    allSources.Add(source);
                }
            }

            for (int i = abilitySourcesHediffs.Count - 1; i >= 0; i--)
            {
                var source = abilitySourcesHediffs[i];
                if (Pawn.health.hediffSet.HasHediff(source) is false)
                {
                    allSources.Add(source);
                }
            }

            for (int i = abilitySourcesThings.Count - 1; i >= 0; i--)
            {
                var source = abilitySourcesThings[i];
                if (Pawn.apparel.WornApparel.Any(x => x.def == source) is false
                    && Pawn.equipment.AllEquipmentListForReading.Any(x => x.def == source) is false)
                {
                    allSources.Add(source);
                }
            }

            for (int i = abilitySourcesGenes.Count - 1; i >= 0; i--)
            {
                var source = abilitySourcesGenes[i];
                if (Pawn.genes.HasActiveGene(source) is false)
                {
                    allSources.Add(source);
                }
            }

            foreach (var source in allSources)
            {
                foreach (var extension in source.GetAbilityExtensions())
                {
                    extension.RemoveAbilities(Pawn, source);
                }
            }

            foreach (var abilityClass in abilityClasses.Values.ToList())
            {
                abilityClass.RemoveIncompatibleAbilities();
                var abilities = abilityClass.LearnedAbilities.ToList();
                if (abilityClass.Unlocked is false)
                {
                    foreach (var ability in abilities)
                    {
                        abilityClass.RemoveAbility(ability);
                    }
                }
                else
                {
                    foreach (var ability in abilities)
                    {
                        if (abilityClass.UnlockedTrees.Any(x => ability.def.abilityTrees.Contains(x)) is false)
                        {
                            abilityClass.RemoveAbility(ability);
                        }
                    }
                }
            }
        }

        private int frameChecking;
        private int curFrame;
        public void TryAutoGainAbilities()
        {
            if (frameChecking >= 3 && curFrame == Time.frameCount)
            {
                return;
            }
            if (curFrame != Time.frameCount)
            {
                frameChecking = 0;
                curFrame = Time.frameCount;
            }
            frameChecking++;
            try
            {
                CheckForGaining();
            }
            catch (Exception e)
            {
                Log.Error("Error in TryAutoGainAbilities: " + e);
            }
        }

        private void CheckForGaining()
        {
            var allSources = new List<Def>();
            if (Pawn.story?.traits != null)
            {
                foreach (var trait in Pawn.story.traits.allTraits)
                {
                    var traitExtension = trait.def.GetModExtension<AbilityExtension>();
                    if (traitExtension != null && abilitySourcesTraits.Contains(trait.def) is false)
                    {
                        allSources.Add(trait.def);
                    }
                }
            }


            foreach (var hediff in Pawn.health.hediffSet.hediffs)
            {
                var hediffExtension = hediff.def.GetModExtension<AbilityExtension>();
                if (hediffExtension != null && abilitySourcesHediffs.Contains(hediff.def) is false)
                {
                    allSources.Add(hediff.def);
                }
            }
            var apparels = Pawn.apparel?.WornApparel?.ToList();
            var gears = Pawn.equipment?.AllEquipmentListForReading;
            var things = new List<Thing>();
            if (apparels != null)
            {
                things.AddRange(apparels);
            }
            if (gears != null)
            {
                things.AddRange(gears);
            }

            foreach (var thing in things)
            {
                var thingExtension = thing.def.GetModExtension<AbilityExtension>();
                if (thingExtension != null && abilitySourcesThings.Contains(thing.def) is false)
                {
                    allSources.Add(thing.def);
                }
            }

            if (Pawn.genes != null)
            {
                foreach (var gene in Pawn.genes.GenesListForReading)
                {
                    var geneExtension = gene.def.GetModExtension<AbilityExtension>();
                    if (geneExtension != null && abilitySourcesGenes.Contains(gene.def) is false)
                    {
                        allSources.Add(gene.def);
                    }
                }
            }

            foreach (var source in allSources)
            {
                if (CheckIfHasSource(source) is false)
                {
                    foreach (var extension in source.GetAbilityExtensions())
                    {
                        extension.UnlockAbilities(Pawn, source);
                    }
                }
            }

            foreach (var abilityClass in Pawn.GetUnlockedAbilityClasses().ToList())
            {
                if (abilityClass.compAbilities is null)
                {
                    abilityClass.compAbilities = this;
                }
                foreach (var abilityTree in abilityClass.UnlockedTrees.ToList())
                {
                    foreach (var abilityDef in abilityTree.AllAbilities.ToList())
                    {
                        while (abilityClass.CanUnlockNextTier(abilityDef, out AbilityTierDef nextAbilityTier, out _)
                            && nextAbilityTier.autoGain)
                        {
                            var ability = abilityClass.GetLearnedAbility(abilityDef);
                            var nextLevel = ability != null ? ability.level + 1 : 0;
                            abilityClass.LearnAbility(abilityDef, abilityClass.def.usesSkillPointSystem, nextLevel);
                            TMagicUtils.Message("Auto gaining " + abilityDef, Pawn);
                        }
                    }
                }
            }
        }

        public bool CheckIfHasSource(Def source)
        {
            if (source is TraitDef trait)
            {
                if (abilitySourcesTraits.Contains(trait))
                {
                    TMagicUtils.Message("comp.abilitySourcesTraits.Contains(trait): " + source, Pawn);
                    return true;
                }
                TMagicUtils.Message("comp.abilitySourcesTraits.Add(trait): " + source, Pawn);
                abilitySourcesTraits.Add(trait);
            }
            else if (source is GeneDef gene)
            {
                if (abilitySourcesGenes.Contains(gene))
                {
                    TMagicUtils.Message("comp.abilitySourcesGenes.Contains(gene): " + source, Pawn);
                    return true;
                }
                abilitySourcesGenes.Add(gene);
            }
            else if (source is HediffDef hediff)
            {
                if (abilitySourcesHediffs.Contains(hediff))
                {
                    TMagicUtils.Message("comp.abilitySourcesHediffs.Contains(hediff): " + source, Pawn);
                    return true;
                }
                abilitySourcesHediffs.Add(hediff);
            }
            else if (source is ThingDef thing)
            {
                if (abilitySourcesThings.Contains(thing))
                {
                    TMagicUtils.Message("comp.abilitySourcesThings.Contains(thing): " + source, Pawn);
                    return true;
                }
                abilitySourcesThings.Add(thing);
            }
            return false;
        }
        public override void PostDraw()
        {
            base.PostDraw();
            foreach (var ability in AllLearnedAbilities)
            {
                ability.Draw();
            }
        }
        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Collections.Look(ref abilityClasses, "TMF_abilityClasses", LookMode.Def, LookMode.Deep);
            Scribe_Collections.Look(ref abilityResources, "TMF_abilityResources", LookMode.Def, LookMode.Deep);
            Scribe_Collections.Look(ref abilitySourcesTraits, "TMF_abilitySourcesTraits", LookMode.Def);
            Scribe_Collections.Look(ref abilitySourcesGenes, "TMF_abilitySourcesGenes", LookMode.Def);
            Scribe_Collections.Look(ref abilitySourcesHediffs, "TMF_abilitySourcesHediffs", LookMode.Def);
            Scribe_Collections.Look(ref abilitySourcesThings, "TMF_abilitySourcesThings", LookMode.Def);

            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                abilityClasses ??= new Dictionary<AbilityClassDef, AbilityClass>();
                abilityResources ??= new Dictionary<AbilityResourceDef, AbilityResource>();

                abilitySourcesTraits ??= new List<TraitDef>();
                abilitySourcesGenes ??= new List<GeneDef>();
                abilitySourcesHediffs ??= new List<HediffDef>();
                abilitySourcesThings ??= new List<ThingDef>();

                foreach (var abilityClassKvp in abilityClasses)
                {
                    abilityClassKvp.Value.Init(this, abilityClassKvp.Key, Pawn, GetAbilityResource(abilityClassKvp.Key.abilityResource));
                }
                foreach (var abilityResourceKvp in abilityResources)
                {
                    abilityResourceKvp.Value.Init(this, abilityResourceKvp.Key, Pawn);
                }
            }
        }
    }
}
