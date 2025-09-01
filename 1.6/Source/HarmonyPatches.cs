using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.Profile;
using static Verse.DamageWorker;
using static Verse.PawnCapacityUtility;

namespace TaranMagicFramework
{
    [StaticConstructorOnStartup]
    public static class TraitStorage
    {
        public static List<TraitDef> allAbilityTraits = new();
        static TraitStorage()
        {
            allAbilityTraits = DefDatabase<TraitDef>.AllDefs.Where(x => x.HasModExtension<AbilityExtension>()).ToList();
        }
    }

    [HarmonyPatch]
    public static class CharacterEditorPatch
    {
        public static MethodBase method;
        [HarmonyPrepare]
        public static bool Prepare()
        {
            method = AccessTools.Method("CharacterEditor.TraitTool:ListOfTraitDef");
            return method != null;
        }

        [HarmonyTargetMethod]
        public static MethodBase GetMethod()
        {
            return method;
        }
        public static void Prefix()
        {
            GenerateTraitsFor_Patch.RestoreDatabase();
        }
    }

    [HarmonyPatch(typeof(PawnGenerator), "GenerateTraits")]
    public static class GenerateTraitsFor_Patch
    {
        [HarmonyPriority(Priority.First)]
        public static void Prefix()
        {
            ClearDatabase();
        }
        public static void ClearDatabase()
        {
            foreach (var def in TraitStorage.allAbilityTraits)
            {
                if (def != null)
                {
                    DefDatabase<TraitDef>.Remove(def);
                }
            }
        }

        [HarmonyPriority(Priority.Last)]
        public static void Postfix(Pawn pawn, PawnGenerationRequest request)
        {
            var abilityTraits = GenerateTraitsFor(pawn, request);
            foreach (var trait in abilityTraits)
            {
                pawn.story.traits.GainTrait(trait);
            }
            RestoreDatabase();
        }

        public static List<Trait> GenerateTraitsFor(Pawn pawn, PawnGenerationRequest? req = null)
        {
            List<Trait> list = new List<Trait>();
            var availableAbilityTraits = TraitStorage.allAbilityTraits
             .Where(x => x.GetModExtension<AbilityExtension>().customTraitGeneration is false).ToList();
            foreach (var newTraitDef in availableAbilityTraits)
            {
                if (Rand.Chance(newTraitDef.GetGenderSpecificCommonality(pawn.gender)))
                {
                    if (pawn.story.traits.HasTrait(newTraitDef) || PawnGenerator.TraitListHasDef(list, newTraitDef))
                    {
                        continue;
                    }
                    if (req.HasValue)
                    {
                        PawnGenerationRequest value = req.Value;
                        if (value.KindDef.disallowedTraits.NotNullAndContains(newTraitDef) || (value.KindDef.disallowedTraitsWithDegree != null && value.KindDef.disallowedTraitsWithDegree.Any((TraitRequirement t) => t.def == newTraitDef && !t.degree.HasValue)) || (value.KindDef.requiredWorkTags != 0 && (newTraitDef.disabledWorkTags & value.KindDef.requiredWorkTags) != 0) || (newTraitDef == TraitDefOf.Gay && !value.AllowGay) || (value.ProhibitedTraits != null && value.ProhibitedTraits.Contains(newTraitDef)) || (value.Faction != null && Faction.OfPlayerSilentFail != null && value.Faction.HostileTo(Faction.OfPlayer) && !newTraitDef.allowOnHostileSpawn))
                        {
                            continue;
                        }
                    }
                    if (pawn.story.traits.allTraits.Any((Trait tr) => newTraitDef.ConflictsWith(tr)) || (newTraitDef.requiredWorkTypes != null && pawn.OneOfWorkTypesIsDisabled(newTraitDef.requiredWorkTypes)) || pawn.WorkTagIsDisabled(newTraitDef.requiredWorkTags) || (newTraitDef.forcedPassions != null && pawn.workSettings != null && newTraitDef.forcedPassions.Any((SkillDef p) => p.IsDisabled(pawn.story.DisabledWorkTagsBackstoryTraitsAndGenes, pawn.GetDisabledWorkTypes(permanentOnly: true)))))
                    {
                        continue;
                    }
                    int degree = PawnGenerator.RandomTraitDegree(newTraitDef);
                    if ((pawn.story.Childhood == null || !pawn.story.Childhood.DisallowsTrait(newTraitDef, degree)) && (pawn.story.Adulthood == null || !pawn.story.Adulthood.DisallowsTrait(newTraitDef, degree)))
                    {
                        Trait trait = new Trait(newTraitDef, degree);
                        if ((pawn.kindDef.disallowedTraitsWithDegree.NullOrEmpty() || !pawn.kindDef.disallowedTraitsWithDegree.Any((TraitRequirement t) => t.Matches(trait))) && (pawn.mindState == null || pawn.mindState.mentalBreaker == null || !((pawn.mindState.mentalBreaker.BreakThresholdMinor + trait.OffsetOfStat(StatDefOf.MentalBreakThreshold)) * trait.MultiplierOfStat(StatDefOf.MentalBreakThreshold) > 0.5f)))
                        {
                            list.Add(trait);
                        }
                    }
                }
            }
            return list;
        }

        private static Exception Finalizer(Exception __exception)
        {
            RestoreDatabase();
            if (__exception != null)
            {
                return __exception;
            }
            return null;
        }

        public static void RestoreDatabase()
        {
            foreach (var def in TraitStorage.allAbilityTraits)
            {
                if (!DefDatabase<TraitDef>.AllDefsListForReading.Any(x => x == def))
                {
                    DefDatabase<TraitDef>.Add(def);
                }
            }
        }
    }

    [HarmonyPatch(typeof(ShotReport), "HitReportFor")]
    public static class ShotReport_HitReportFor
    {
        public static bool accuracy;
        public static void Prefix(ref ShotReport __result, Thing caster, Verb verb, LocalTargetInfo target)
        {
            if (verb.ShouldHaveAccuracy())
            {
                accuracy = true;
            }
            TMagicUtils.Message(verb + " - " + verb.ShouldHaveAccuracy(), caster as Pawn);
        }

        public static bool ShouldHaveAccuracy(this Verb verb)
        {
            return verb is Verb_ShootAbility ||
                (verb.EquipmentSource?.def?.GetModExtension<AbilityExtension>()?.alwaysHitTarget ?? false);
        }
    }

    [HarmonyPatch(typeof(Verb_LaunchProjectile), "TryCastShot")]
    public static class Verb_LaunchProjectile_TryCastShot
    {
        [HarmonyPriority(int.MaxValue)]
        public static void Prefix(Verb_LaunchProjectile __instance)
        {
            if (__instance.ShouldHaveAccuracy())
            {
                ShotReport_HitReportFor.accuracy = true;
            }
            TMagicUtils.Message(__instance + " - " + __instance.ShouldHaveAccuracy(), __instance.CasterPawn);
        }
        public static void Postfix(Verb_LaunchProjectile __instance)
        {
            ShotReport_HitReportFor.accuracy = false;
        }
    }

    [HarmonyPatch(typeof(ShotReport), "AimOnTargetChance_StandardTarget", MethodType.Getter)]
    public static class ShotReport_AimOnTargetChance_StandardTarget
    {
        public static void Postfix(ref float __result)
        {
            if (ShotReport_HitReportFor.accuracy)
            {
                __result = 1f;
            }
            TMagicUtils.Message("accuracy: " + __result);

        }
    }

    [HarmonyPatch(typeof(DamageResult), "AssociateWithLog")]
    public static class DamageResult_AssociateWithLog
    {
        public static void Prefix(LogEntry_DamageResult log)
        {
            if (log is BattleLogEntry_RangedImpact battleLog && battleLog.weaponDef is null)
            {
                battleLog.weaponDef = battleLog.initiatorPawn.def;
            }
        }
    }


    [HarmonyPatch(typeof(Pawn), "Kill")]
    public static class Pawn_Kill_Patch
    {
        private static void Postfix(Pawn __instance, DamageInfo? dinfo, Hediff exactCulprit = null)
        {
            if (__instance.Dead)
            {
                var comp = __instance.GetComp<CompAbilities>();
                if (comp != null)
                {
                    foreach (var ability in comp.AllLearnedAbilities)
                    {
                        ability.Notify_CasterKilled();
                        if (ability.Active)
                        {
                            ability.End();
                        }
                    }
                }
            }
        }
    }

    [HarmonyPatch(typeof(Pawn_HealthTracker), "MakeDowned")]
    public static class Pawn_HealthTracker_MakeDowned_Patch
    {
        private static void Postfix(Pawn ___pawn, DamageInfo? dinfo, Hediff hediff)
        {
            if (___pawn.Downed && hediff?.def != HediffDefOf.Anesthetic)
            {
                var comp = ___pawn.GetComp<CompAbilities>();
                if (comp != null && ___pawn.health.hediffSet.hediffs.Any(x => x is Hediff_Injury))
                {
                    foreach (var ability in comp.AllLearnedAbilities)
                    {
                        ability.Notify_CasterDowned();
                        if (ability.Active)
                        {
                            ability.End();
                        }
                    }
                }
            }
        }
    }

    [HarmonyPatch(typeof(StatWorker), "GetValueUnfinalized")]
    public static class StatWorker_GetValueUnfinalized_Patch
    {
        private class StatBonusCacheEntry
        {
            public float offset;
            public float factor;
            public int tick;
        }

        private static readonly ConcurrentDictionary<(Pawn, StatDef), StatBonusCacheEntry> _statBonusCache = new ConcurrentDictionary<(Pawn, StatDef), StatBonusCacheEntry>();

        public static void ApplyOffsets(ref float __result, StatWorker __instance, StatRequest req)
        {
            if (req.Thing is Pawn pawn)
            {
                var (offset, _) = GetOrCalculateStatBonuses(pawn, __instance.stat);
                __result += offset;
            }
        }

        public static void Postfix(ref float __result, StatWorker __instance, StatRequest req)
        {
            if (req.Thing is Pawn pawn)
            {
                var (_, factor) = GetOrCalculateStatBonuses(pawn, __instance.stat);
                __result *= factor;
            }
        }

        private static (float offset, float factor) GetOrCalculateStatBonuses(Pawn pawn, StatDef stat)
        {
            int currentTick = Find.TickManager.TicksGame;
            var cacheKey = (pawn, stat);

            if (_statBonusCache.TryGetValue(cacheKey, out StatBonusCacheEntry cachedEntry) && currentTick - cachedEntry.tick < 60)
            {
                return (cachedEntry.offset, cachedEntry.factor);
            }

            float totalOffset = 0f;
            float totalFactor = 1f;
            var comp = pawn.GetComp<CompAbilities>();
            if (comp != null)
            {
                foreach (var abilityClass in comp.AllUnlockedAbilityClasses)
                {
                    if (abilityClass.def.statBonusesPerLevel != null)
                    {
                        foreach (var entry in abilityClass.def.statBonusesPerLevel)
                        {
                            if (abilityClass.level >= entry.minLevel)
                            {
                                totalOffset += entry.statOffsets?.GetStatOffsetFromList(stat) ?? 0f;
                                totalFactor *= entry.statFactors?.GetStatFactorFromList(stat) ?? 1f;
                            }
                        }
                    }

                    foreach (var abilityTree in abilityClass.UnlockedTrees)
                    {
                        if (abilityTree.statBonusesPerLevel != null)
                        {
                            foreach (var entry in abilityTree.statBonusesPerLevel)
                            {
                                if (abilityClass.level >= entry.minLevel)
                                {
                                    totalOffset += entry.statOffsets?.GetStatOffsetFromList(stat) ?? 0f;
                                    totalFactor *= entry.statFactors?.GetStatFactorFromList(stat) ?? 1f;
                                }
                            }
                        }
                    }
                }

                foreach (var ability in comp.AllLearnedAbilities)
                {
                    var abilityTier = ability.AbilityTier;
                    totalOffset += abilityTier.statOffsets?.GetStatOffsetFromList(stat) ?? 0f;
                    totalFactor *= abilityTier.statFactors?.GetStatFactorFromList(stat) ?? 1f;

                    if (stat == StatDefOf.MoveSpeed && ability.Active && abilityTier.movementSpeedFactor != -1f)
                    {
                        totalFactor *= abilityTier.movementSpeedFactor;
                    }
                }
            }

            var newEntry = new StatBonusCacheEntry
            {
                offset = totalOffset,
                factor = totalFactor,
                tick = currentTick
            };

            _statBonusCache[cacheKey] = newEntry;
            return (newEntry.offset, newEntry.factor);
        }

        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var codes = instructions.ToList();
            var applyOffsetsMethod = AccessTools.Method(typeof(StatWorker_GetValueUnfinalized_Patch), nameof(ApplyOffsets));
            bool patched = false;

            for (int i = 0; i < codes.Count; i++)
            {
                yield return codes[i];

                if (!patched && i > 1 && codes[i].opcode == OpCodes.Brfalse && codes[i - 1].opcode == OpCodes.Ldloc_1)
                {
                    yield return new CodeInstruction(OpCodes.Ldloca_S, 0);
                    yield return new CodeInstruction(OpCodes.Ldarg_0);
                    yield return new CodeInstruction(OpCodes.Ldarg_1);
                    yield return new CodeInstruction(OpCodes.Call, applyOffsetsMethod);
                    patched = true;
                }
            }
        }
    }

    [HarmonyPatch(typeof(StatWorker), "GetExplanationUnfinalized")]
    public static class StatWorker_GetExplanationUnfinalized_Patch
    {
        public static void Postfix(ref string __result, StatWorker __instance, StatRequest req, ToStringNumberSense numberSense)
        {
            bool hasAbilityModifiers = false;
            var stringBuilder = new StringBuilder();
            var explanation = new StringBuilder();
            Pawn pawn = req.Thing as Pawn;
            if (pawn != null)
            {
                var comp = pawn.GetComp<CompAbilities>();
                if (comp != null)
                {
                    if (comp != null)
                    {

                        foreach (var abilityClass in comp.AllUnlockedAbilityClasses)
                        {
                            if (abilityClass.def.statBonusesPerLevel != null)
                            {
                                foreach (var entry in abilityClass.def.statBonusesPerLevel)
                                {
                                    if (abilityClass.level >= entry.minLevel)
                                    {
                                        if (entry.statOffsets != null && entry.statOffsets.Exists(x => x.stat == __instance.stat))
                                        {
                                            hasAbilityModifiers = true;
                                            string valueToStringAsOffset = entry.statOffsets.First((StatModifier se) => se.stat == __instance.stat).ValueToStringAsOffset;
                                            explanation.AppendLine("    " + abilityClass.def.LabelCap + "(" + entry.minLevel + "): " + valueToStringAsOffset);
                                        }
                                        if (entry.statFactors != null && entry.statFactors.Exists(x => x.stat == __instance.stat))
                                        {
                                            hasAbilityModifiers = true;
                                            string toStringAsFactor = entry.statFactors.First((StatModifier se) => se.stat == __instance.stat).ToStringAsFactor;
                                            explanation.AppendLine("    " + abilityClass.def.LabelCap + "(" + entry.minLevel + "): " + toStringAsFactor);
                                        }
                                    }
                                }
                            }

                            foreach (var abilityTree in abilityClass.UnlockedTrees)
                            {
                                if (abilityTree.statBonusesPerLevel != null)
                                {
                                    foreach (var entry in abilityTree.statBonusesPerLevel)
                                    {
                                        if (abilityClass.level >= entry.minLevel)
                                        {
                                            if (entry.statOffsets != null && entry.statOffsets.Exists(x => x.stat == __instance.stat))
                                            {
                                                hasAbilityModifiers = true;
                                                string valueToStringAsOffset = entry.statOffsets.First((StatModifier se) => se.stat == __instance.stat).ValueToStringAsOffset;
                                                explanation.AppendLine("    " + abilityTree.LabelCap + "(" + entry.minLevel + "): " + valueToStringAsOffset);
                                            }
                                            if (entry.statFactors != null && entry.statFactors.Exists(x => x.stat == __instance.stat))
                                            {
                                                hasAbilityModifiers = true;
                                                string toStringAsFactor = entry.statFactors.First((StatModifier se) => se.stat == __instance.stat).ToStringAsFactor;
                                                explanation.AppendLine("    " + abilityTree.LabelCap + "(" + entry.minLevel + "): " + toStringAsFactor);
                                            }
                                        }
                                    }
                                }
                            }
                        }

                        foreach (var ability in comp.AllLearnedAbilities)
                        {
                            var abilityTier = ability.AbilityTier;
                            if (abilityTier.statOffsets != null && abilityTier.statOffsets.Exists(x => x.stat == __instance.stat))
                            {
                                hasAbilityModifiers = true;
                                string valueToStringAsOffset = abilityTier.statOffsets.First((StatModifier se) => se.stat == __instance.stat).ValueToStringAsOffset;
                                explanation.AppendLine("    " + ability.AbilityLabel() + ": " + valueToStringAsOffset);
                            }
                            if (abilityTier.statFactors != null && abilityTier.statFactors.Exists(x => x.stat == __instance.stat))
                            {
                                hasAbilityModifiers = true;
                                string toStringAsFactor = abilityTier.statFactors.First((StatModifier se) => se.stat == __instance.stat).ToStringAsFactor;
                                explanation.AppendLine("    " + ability.AbilityLabel() + ": " + toStringAsFactor);
                            }
                        }
                    }
                }
            }

            if (hasAbilityModifiers)
            {
                stringBuilder.AppendLine("TMF.StatsReport_Abilities".Translate());
                stringBuilder.AppendLine(explanation.ToString());
                __result += "\n" + stringBuilder.ToString();
            }
        }
    }

    [HarmonyPatch(typeof(PawnCapacityUtility), "CalculateCapacityLevel")]
    public static class PawnCapacityUtility_CalculateCapacityLevel_Patch
    {
        public static void Postfix(ref float __result, HediffSet diffSet, PawnCapacityDef capacity, List<CapacityImpactor> impactors = null, bool forTradePrice = false)
        {
            var comp = diffSet.pawn.GetComp<CompAbilities>();
            if (comp != null)
            {
                foreach (var ability in comp.AllLearnedAbilities)
                {
                    if (ability.AbilityTier.capMods != null)
                    {
                        var pawnCapacityModifier = ability.AbilityTier.capMods.FirstOrDefault(x => x.capacity == capacity);
                        if (pawnCapacityModifier != null)
                        {
                            impactors?.Add(new CapacityImpactorAbility
                            {
                                capacity = capacity,
                                ability = ability,
                            });
                            var sb = new StringBuilder();
                            if (pawnCapacityModifier.offset != 0f)
                            {
                                __result += pawnCapacityModifier.offset;
                            }
                            if (pawnCapacityModifier.postFactor != 1f)
                            {
                                __result *= pawnCapacityModifier.postFactor;
                            }
                            if (pawnCapacityModifier.setMax != 999f)
                            {
                                float maxValue = pawnCapacityModifier.EvaluateSetMax(diffSet.pawn);
                                if (maxValue < __result)
                                {
                                    __result = maxValue;
                                }
                            }
                        }
                    }
                }
            }
        }
    }

    public class CapacityImpactorAbility : CapacityImpactorCapacity
    {
        public Ability ability;
        public override string Readable(Pawn pawn)
        {
            var pawnCapacityModifier = ability.AbilityTier.capMods.FirstOrDefault(x => x.capacity == capacity);
            var sb = new StringBuilder();
            if (pawnCapacityModifier.offset != 0f)
            {
                sb.Append(pawnCapacityModifier.capacity.GetLabelFor(pawn).CapitalizeFirst() + " " + (pawnCapacityModifier.offset * 100f).ToString("+#;-#") + "%");
            }
            if (pawnCapacityModifier.postFactor != 1f)
            {
                sb.Append(pawnCapacityModifier.capacity.GetLabelFor(pawn).CapitalizeFirst() + " x" + pawnCapacityModifier.postFactor.ToStringPercent());
            }
            if (pawnCapacityModifier.setMax != 999f)
            {
                sb.Append(pawnCapacityModifier.capacity.GetLabelFor(pawn).CapitalizeFirst() + " " + "max".Translate().CapitalizeFirst() + ": " + pawnCapacityModifier.setMax.ToStringPercent());
            }
            return "TMF.AbilityCapacityEffects".Translate(ability.AbilityLabel(), sb.ToString());
        }
    }

    [HarmonyBefore("legodude17.mvcf")]
    [HarmonyPatch(typeof(Pawn), nameof(Pawn.TryGetAttackVerb))]
    public static class TryGetAttackVerb_Patch
    {
        [HarmonyPostfix]
        public static void Postfix(Pawn __instance, ref Verb __result, Thing target)
        {
            if (__instance.IsColonistPlayerControlled)
            {
                var comp = __instance.TryGetComp<CompAbilities>();
                if (comp == null || __instance.Spawned is false)
                {
                    return;
                }
                var verbs = comp.AllLearnedAbilities.Where(x => x.AutocastEnabled).SelectMany(x => x.Verbs).Where(x => x.Available()).ToList();
                if (verbs.NullOrEmpty())
                {
                    return;
                }

                if (target != null)
                {
                    if (verbs.Select(ve => new Tuple<Verb, float>(ve, ve.VerbPropsAbility().autocastChance)).AddItem(new Tuple<Verb, float>(__result, 1f))
                          .TryRandomElementByWeight(t => t.Item2, out var result))
                    {
                        __result = result.Item1;
                    }
                }
                else
                {
                    var verb = verbs.AddItem(__result).MaxBy(ve => ve.verbProps.range);
                    __result = verb;
                }
            }
        }
    }

    [HarmonyPatch(typeof(Pawn_JobTracker), nameof(Pawn_JobTracker.DetermineNextJob))]
    public static class Pawn_JobTracker_DetermineNextJob_Patch
    {
        public static bool Prefix(Pawn_JobTracker __instance, ref ThinkResult __result)
        {
            if (__instance.pawn.IsColonist || __instance.pawn.Spawned is false)
            {
                return true;
            }
            var comp = __instance.pawn.TryGetComp<CompAbilities>();
            if (comp == null)
            {
                return true;
            }
            bool debug = false;
            var abilities = comp.AllLearnedAbilities.Where(x => x.AbilityTier.aiAbilityWorker != null
            && (!x.Active && x.CanBeActivated(x.EnergyCost, out _, canBeActivatedValidator: x.CanBeActivatedValidator())));
            if (debug) Log.Message("Checking pawn: " + __instance.pawn + " - " + string.Join(", ", comp.abilityResources.Select(x => x.Key + " - " + x.Value.energy)));
            foreach (var abilityGroup in abilities.GroupBy(x => x.AbilityTier.AIAbilityWorker.Priority(x)).OrderByDescending(x => x.Key))
            {
                foreach (var ability in abilityGroup.InRandomOrder())
                {
                    if (debug) Log.Message("Ability energy: " + ability.abilityResource.energy + " - " + ability.EnergyCost);
                    if (ability.AbilityTier.AIAbilityWorker.CanActivate(ability, out var targetData))
                    {
                        if (targetData.verb != null)
                        {
                            if (targetData.verb is Verb_ShootAbility verb_ShootAbility)
                            {
                                var job = verb_ShootAbility.GetVerbJob(targetData.target);
                                __result = new ThinkResult(job, null);
                                if (debug) Log.Message(abilityGroup.Key + " - casting: " + ability.AbilityLabel() + " on " + targetData.target.Thing);
                                if (debug) Find.LetterStack.ReceiveLetter("Casting: " + ability.AbilityLabel() + " on " + targetData.target.Thing, "", LetterDefOf.NeutralEvent, ability.pawn);
                                return false;
                            }
                            else if (targetData.verb is Verb_MeleeAttackDamageAbility verb_MeleeAttack)
                            {
                                var job = verb_MeleeAttack.GetVerbJob(targetData.target);
                                __result = new ThinkResult(job, null);
                                if (debug) Log.Message(abilityGroup.Key + " - casting: " + ability.AbilityLabel() + " on " + targetData.target.Thing);
                                if (debug) Find.LetterStack.ReceiveLetter("Casting: " + ability.AbilityLabel() + " on " + targetData.target.Thing, "", LetterDefOf.NeutralEvent, ability.pawn);
                                return false;
                            }
                        }
                        else
                        {
                            ability.curTarget = targetData.target;
                            if (ability.AbilityTier.jobDef != null)
                            {
                                var job = TMagicUtils.MakeJobAbility(ability, targetData.target);
                                __result = new ThinkResult(job, null);
                                if (debug) Log.Message(abilityGroup.Key + " - casting: " + ability.AbilityLabel() + " on " + targetData.target.Thing);
                                if (debug) Find.LetterStack.ReceiveLetter("Casting: " + ability.AbilityLabel() + " on " + targetData.target.Thing, "", LetterDefOf.NeutralEvent, ability.pawn);
                                return false;
                            }
                            else
                            {
                                ability.Start();
                                if (debug) Log.Message(abilityGroup.Key + " - casting: " + ability.AbilityLabel() + " on " + targetData.target.Thing);
                                if (debug) Find.LetterStack.ReceiveLetter("Casting: " + ability.AbilityLabel() + " on " + targetData.target.Thing, "", LetterDefOf.NeutralEvent, ability.pawn);
                            }
                        }
                    }
                    else
                    {
                        if (debug) Log.Message(abilityGroup.Key + " - Checked ability, cannot activate: " + ability.def);
                    }
                }
            }
            return true;
        }
    }

    [HarmonyPatch(typeof(TraitSet), "GainTrait")]
    public static class TraitSet_GainTrait_Patch
    {
        [HarmonyPriority(int.MaxValue)]
        public static void Postfix(Pawn ___pawn, Trait trait)
        {
            if (___pawn.story.traits.allTraits.Any(x => x.def == trait.def))
            {
                trait.def.UnlockOrRecheckAbilities(___pawn);
            }
        }
    }

    [HarmonyPatch(typeof(TraitSet), "RemoveTrait")]
    public static class TraitSet_RemoveTrait_Patch
    {
        [HarmonyPriority(int.MaxValue)]
        public static void Postfix(Pawn ___pawn, Trait trait)
        {
            if (___pawn.story.traits.allTraits.Any(x => x.def == trait.def) is false)
            {
                trait.def.TryRemoveAbilities(___pawn);
            }
        }
    }

    [HarmonyPatch(typeof(Pawn_GeneTracker), "AddGene", new Type[] { typeof(Gene), typeof(bool) })]
    public static class Pawn_GeneTracker_AddGene_Patch
    {
        [HarmonyPriority(int.MaxValue)]
        public static void Postfix(Pawn ___pawn, Gene __result)
        {
            if (__result != null && ___pawn.genes.GenesListForReading.Any(x => x.def == __result.def))
            {
                __result.def.UnlockOrRecheckAbilities(___pawn);
            }
        }
    }

    [HarmonyPatch(typeof(Pawn_GeneTracker), "RemoveGene")]
    public static class Pawn_GeneTracker_RemoveGene_Patch
    {
        [HarmonyPriority(int.MaxValue)]
        public static void Postfix(Pawn ___pawn, Gene gene)
        {
            if (gene != null && ___pawn.genes.GenesListForReading.Any(x => x.def == gene.def) is false)
            {
                gene.def.TryRemoveAbilities(___pawn);
            }
        }
    }

    [HarmonyPatch(typeof(HediffSet), "AddDirect")]
    public static class HediffSet_AddDirect_Patch
    {
        [HarmonyPriority(int.MaxValue)]
        private static void Postfix(HediffSet __instance, Pawn ___pawn, Hediff hediff)
        {
            if (__instance.GetFirstHediffOfDef(hediff.def) != null)
            {
                hediff.def.UnlockOrRecheckAbilities(___pawn);
            }
        }
    }

    [HarmonyPatch(typeof(Hediff), "PostRemoved")]
    public static class Hediff_PostRemoved_Patch
    {
        [HarmonyPriority(int.MaxValue)]
        private static void Postfix(Hediff __instance)
        {
            if (__instance.pawn != null && __instance.pawn.health.hediffSet.GetFirstHediffOfDef(__instance.def) is null)
            {
                __instance.def.TryRemoveAbilities(__instance.pawn);
            }
        }
    }

    [HarmonyPatch(typeof(Pawn_EquipmentTracker), "AddEquipment")]
    public static class Pawn_EquipmentTracker_AddEquipment_Patch
    {
        public static void Postfix(Pawn_EquipmentTracker __instance, ThingWithComps newEq)
        {
            if (__instance.pawn.equipment.AllEquipmentListForReading.Any(x => x.def == newEq.def))
            {
                newEq.def.UnlockOrRecheckAbilities(__instance.pawn);
            }
        }
    }

    [HarmonyPatch(typeof(Pawn_EquipmentTracker), "TryDropEquipment")]
    public static class Pawn_EquipmentTracker_TryDropEquipment_Patch
    {
        public static void Postfix(Pawn_EquipmentTracker __instance, ThingWithComps eq, ThingWithComps resultingEq, IntVec3 pos, bool forbid = true)
        {
            if (__instance.pawn.equipment.AllEquipmentListForReading.Any(x => x.def == eq.def) is false)
            {
                eq.def.TryRemoveAbilities(__instance.pawn);
            }
        }
    }

    [HarmonyPatch(typeof(PawnGenerator), "TryGenerateNewPawnInternal")]
    public static class PawnGenerator_TryGenerateNewPawnInternal_Patch
    {
        public static void Postfix(Pawn __result)
        {
            if (__result != null && __result.DevelopmentalStage != DevelopmentalStage.Newborn)
            {
                var extensions = __result.kindDef.GetAbilityExtensions();
                foreach (var extension in extensions)
                {
                    extension.UnlockAbilities(__result, null);
                }
            }
        }
    }

    [HarmonyPatch(typeof(ListerThings), "EverListable")]
    public static class ListerThings_EverListable_Patch
    {
        public static void Postfix(ThingDef def, ref bool __result)
        {
            if (def is AnimationDef)
            {
                __result = true;
            }
        }
    }

    [HarmonyPatch(typeof(ThingDef), "HasThingIDNumber", MethodType.Getter)]
    public static class ThingDef_HasThingIDNumber_Patch
    {
        public static void Postfix(ThingDef __instance, ref bool __result)
        {
            if (__instance is AnimationDef)
            {
                __result = true;
            }
        }
    }

    [HarmonyPatch(typeof(InspectTabManager), "GetSharedInstance")]
    public static class InspectTabManager_GetSharedInstance_Patch
    {
        public static bool Prefix(Type tabType, ref InspectTabBase __result)
        {
            foreach (var tabDef in DefDatabase<AbilityTabDef>.AllDefs)
            {
                if (tabType == tabDef.tabClass)
                {
                    __result = TMagicUtils.GetSharedInstance(tabDef);
                    return false;
                }
            }
            return true;
        }
    }

    [HarmonyPatch(typeof(StatWorker), "ShouldShowFor")]
    public static class StatWorker_ShouldShowFor_Patch
    {
        public static bool Prefix(StatWorker __instance, StatRequest req, ref bool __result)
        {
            if (__instance.stat?.category?.GetModExtension<StatDefExtension>()?.isAbilityStatCategory ?? false)
            {
                if (req.Thing is Pawn pawn && pawn.IsAbilityUser())
                {
                    __result = true;
                    return false;
                }
                else
                {
                    __result = false;
                    return false;
                }
            }
            return true;
        }
    }

    [HarmonyPatch]
    public static class Verb_LaunchProjectile_TryCastShot_Patch
    {
        public static MethodBase yayoMethod = AccessTools.Method("yayoCombat.patch_Verb_LaunchProjectile_TryCastShot:Prefix");

        [HarmonyTargetMethods]
        public static IEnumerable<MethodBase> TargetMethods()
        {
            yield return AccessTools.Method(typeof(Verb_LaunchProjectile), "TryCastShot");
            if (yayoMethod != null)
            {
                yield return yayoMethod;
            }
        }
        private static IEnumerable<CodeInstruction> Transpiler(MethodBase __originalMethod, IEnumerable<CodeInstruction> instructions, ILGenerator ilGen)
        {
            bool found = false;
            var applyEffects = AccessTools.Method(typeof(Verb_LaunchProjectile_TryCastShot_Patch), nameof(ApplyEffects));
            var codes = instructions.ToList();
            for (var i = 0; i < codes.Count; i++)
            {
                yield return codes[i];
                if (!found && codes[i].opcode == OpCodes.Stloc_S && codes[i].operand is LocalBuilder lb && lb.LocalIndex == 7)
                {
                    found = true;
                    if (__originalMethod == yayoMethod)
                    {
                        yield return new CodeInstruction(OpCodes.Ldarg_1);
                    }
                    else
                    {
                        yield return new CodeInstruction(OpCodes.Ldarg_0);

                    }
                    yield return new CodeInstruction(OpCodes.Ldloc_S, 7);
                    yield return new CodeInstruction(OpCodes.Call, applyEffects);
                }
            }
        }

        public static void ApplyEffects(Verb_LaunchProjectile verb, Projectile projectile)
        {
            if (verb is Verb_ShootAbility verbShootAbility)
            {
                if (projectile is Bullet_Ability bulletAbility)
                {
                    bulletAbility.ability = verbShootAbility.ability;
                }
                else if (projectile is Projectile_Explosive_Ability explosive_Ability)
                {
                    explosive_Ability.ability = verbShootAbility.ability;
                }
            }
        }
    }

    [HarmonyPatch(typeof(Bullet), "Impact")]
    public static class Bullet_Impact_Patch
    {
        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator ilGen)
        {
            bool found = false;
            var applyEffects = AccessTools.Method(typeof(Bullet_Impact_Patch), nameof(ApplyEffects));
            var takeDamage = AccessTools.PropertyGetter(typeof(Projectile), nameof(Projectile.DamageAmount));

            var codes = instructions.ToList();
            for (var i = 0; i < codes.Count; i++)
            {
                yield return codes[i];
                if (!found && codes[i].Calls(takeDamage))
                {
                    found = true;
                    yield return new CodeInstruction(OpCodes.Ldarg_0);
                    yield return new CodeInstruction(OpCodes.Ldarg_1);
                    yield return new CodeInstruction(OpCodes.Call, applyEffects);
                }
            }
        }

        public static int ApplyEffects(int damageAmount, Bullet projectile, Thing hitThing)
        {
            if (projectile is Bullet_Ability bulletAbility && bulletAbility.ability != null)
            {
                damageAmount = (int)bulletAbility.ability.abilityClass.AdjustDamage(damageAmount, hitThing);
            }
            return damageAmount;
        }
    }

    [HarmonyPatch(typeof(Projectile_Explosive), "Explode")]
    public static class Projectile_Explosive_Explode_Patch
    {
        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator ilGen)
        {
            bool found = false;
            var applyEffects = AccessTools.Method(typeof(Projectile_Explosive_Explode_Patch), nameof(ApplyEffects));
            var takeDamage = AccessTools.PropertyGetter(typeof(Projectile), nameof(Projectile.DamageAmount));

            var codes = instructions.ToList();
            for (var i = 0; i < codes.Count; i++)
            {
                yield return codes[i];
                if (!found && codes[i].Calls(takeDamage))
                {
                    found = true;
                    yield return new CodeInstruction(OpCodes.Ldarg_0);
                    yield return new CodeInstruction(OpCodes.Call, applyEffects);
                }
            }
        }

        public static int ApplyEffects(int damageAmount, Projectile_Explosive projectile)
        {
            if (projectile is Projectile_Explosive_Ability bulletAbility && bulletAbility.ability != null)
            {
                damageAmount = (int)bulletAbility.ability.abilityClass.AdjustDamage(damageAmount, null);
            }
            return damageAmount;
        }
    }

    [HarmonyPatch(typeof(Projectile), "Launch", new Type[] { typeof(Thing), typeof(Vector3), typeof(LocalTargetInfo), typeof(LocalTargetInfo),
        typeof(ProjectileHitFlags), typeof(bool), typeof(Thing), typeof(ThingDef) })]

    public static class Projectile_Launch_Patch
    {
        public static void Prefix(Projectile __instance, Thing launcher, Vector3 origin, ref LocalTargetInfo usedTarget, LocalTargetInfo intendedTarget)
        {
            if (__instance is Projectile_Explosive_Ability || __instance is Bullet_Ability)
            {
                usedTarget = intendedTarget;
            }
        }
    }

    [HarmonyPatch(typeof(JobDriver), "EndJobWith")]
    public static class JobDriver_EndJobWith_Patch
    {
        public static void Prefix(JobDriver __instance)
        {
            if (__instance is JobDriver_AttackMelee attackMelee)
            {
                if (attackMelee.job?.verbToUse is Verb_MeleeAttackDamageAbility verbAbility)
                {
                    verbAbility.ability.End();
                }
            }
        }
    }

    [HarmonyPatch(typeof(Tool), "AdjustedCooldown", new Type[] { typeof(Thing) })]
    public static class Tool_AdjustedCooldown_Patch
    {
        public static bool Prepare() => ModsConfig.IsActive("OskarPotocki.VanillaFactionsExpanded.Core") is false;
        public static void Postfix(Thing ownerEquipment, ref float __result)
        {
            if (ownerEquipment?.ParentHolder is Pawn_EquipmentTracker eq)
            {
                __result /= eq.pawn.GetStatValue(TMF_DefOf.VEF_MeleeAttackSpeedFactor);
            }
        }
    }

    [HarmonyPatch(typeof(VerbProperties), "AdjustedMeleeDamageAmount", new Type[] { typeof(Verb), typeof(Pawn) })]
    internal static class AdjustedMeleeDamageAmount_Patch
    {
        public static void Postfix(Verb ownerVerb, Pawn attacker, ref float __result)
        {
            if (ownerVerb.verbProps.AdjustedLinkedBodyPartsGroup(ownerVerb.tool) != null)
            {
                __result += attacker.GetStatValue(TMF_DefOf.TMF_MeleeAttackDamageOffset);
            }
        }
    }

    [HarmonyPatch(typeof(VerbProperties), "AdjustedArmorPenetration", new Type[] { typeof(Verb), typeof(Pawn) })]
    internal static class AdjustedArmorPenetration_Patch
    {
        public static void Postfix(Verb ownerVerb, Pawn attacker, ref float __result)
        {
            if (ownerVerb.verbProps.AdjustedLinkedBodyPartsGroup(ownerVerb.tool) != null)
            {
                __result += attacker.GetStatValue(TMF_DefOf.TMF_MeleeAttackArmorPenetrationOffset);
            }
        }
    }
    [HarmonyPatch(typeof(Verb_MeleeAttackDamage), "DamageInfosToApply")]
    public static class Patch_DamageInfosToApply
    {
        public static HashSet<DamageInfo> meleeDamages = new HashSet<DamageInfo>();
        private static IEnumerable<DamageInfo> Postfix(IEnumerable<DamageInfo> __result, Verb __instance, LocalTargetInfo target)
        {
            foreach (var damageInfo in __result)
            {
                meleeDamages.Add(damageInfo);
                yield return damageInfo;
            }
        }

        public static bool IsMeleeAttack(this DamageInfo dinfo)
        {
            return dinfo.Weapon is null || dinfo.Weapon?.race != null
                || dinfo.Weapon.IsMeleeWeapon || meleeDamages.Contains(dinfo);
        }
    }

    [HarmonyPatch(typeof(Thing), "TakeDamage")]
    public static class Thing_TakeDamage_Patch
    {
        public static void Prefix(Thing __instance, DamageInfo dinfo)
        {
            if (dinfo.Instigator is Pawn attacker && __instance is Pawn victim)
            {
                if (dinfo.IsMeleeAttack())
                {
                    if (dinfo.Def != DamageDefOf.Stun)
                    {
                        var stunRate = attacker.GetStatValue(TMF_DefOf.TMF_MeleeAttackStunRate);
                        if (Rand.Chance(stunRate))
                        {
                            victim.stances?.stunner?.StunFor(60, attacker);
                        }
                    }
                }
            }
        }
    }

    [HarmonyPatch(typeof(MemoryUtility), "UnloadUnusedUnityAssets")]
    public static class MemoryUtility_UnloadUnusedUnityAssets_Patch
    {
        public static void Postfix()
        {
            Ability.allAbilitiesByDef.Clear();
        }
    }

    public class UISettings : IExposable
    {
        public Dictionary<AbilityDef, bool> abilityStates = new Dictionary<AbilityDef, bool>();
        public void ExposeData()
        {
            Scribe_Collections.Look(ref abilityStates, "abilityStates", LookMode.Def, LookMode.Value);
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                abilityStates ??= new Dictionary<AbilityDef, bool>();
            }
        }
    }

    [HarmonyPatch(typeof(Pawn), "ExposeData")]
    public static class Pawn_ExposeData_Patch
    {
        public static void Postfix(Pawn __instance)
        {
            var uiSettingsData = __instance.GetUISettingsData();
            Scribe_Deep.Look(ref uiSettingsData, "uiSettingsData");
            if (uiSettingsData != null)
            {
                pawnUISettingsData[__instance] = uiSettingsData;
            }
        }


        public static Dictionary<Pawn, UISettings> pawnUISettingsData = new Dictionary<Pawn, UISettings>();
        public static UISettings GetUISettingsData(this Pawn pawn)
        {
            if (!pawnUISettingsData.TryGetValue(pawn, out var data) || data is null)
            {
                pawnUISettingsData[pawn] = data = new UISettings();
            }
            return data;
        }
    }

    [HarmonyPatch(typeof(Pawn_HealthTracker), "ShouldBeDowned")]
    public static class Pawn_HealthTracke_ShouldBeDownedr_Patch
    {
        public static bool Prefix(Pawn ___pawn)
        {
            var comp = ___pawn.GetComp<CompAbilities>();
            if (comp != null && comp.AllLearnedAbilities.Any(x => x.Active && x.AbilityTier.preventsFromBeingDowned))
            {
                return false;
            }
            return true;
        }
    }
}
