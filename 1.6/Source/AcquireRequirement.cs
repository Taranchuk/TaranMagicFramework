using RimWorld;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using Verse;

namespace TaranMagicFramework
{
    public class AbilityClassLevelRequirement
    {
        public AbilityClassDef abilityClass;

        public int minLevel;
        public void LoadDataFromXmlCustom(XmlNode xmlRoot)
        {
            DirectXmlCrossRefLoader.RegisterObjectWantsCrossRef(this, "abilityClass", xmlRoot);
            minLevel = ParseHelper.FromString<int>(xmlRoot.FirstChild.Value);
        }
    }

    public class AbilityLevelRequirement
    {
        public AbilityDef ability;

        public int minLevel;
        public void LoadDataFromXmlCustom(XmlNode xmlRoot)
        {
            DirectXmlCrossRefLoader.RegisterObjectWantsCrossRef(this, "ability", xmlRoot);
            minLevel = ParseHelper.FromString<int>(xmlRoot.FirstChild.Value);
        }
    }

    public class AcquireRequirement
    {
        public AbilityTierDef def;
        public int minLevel;
        public List<AbilityClassLevelRequirement> requiredMinimumLevels;
        public List<AbilityLevelRequirement> requiredMinimumAbilityLevels;
        public List<SkillRequirement> requiredMinimumSkillLevels;

        public int skillPointsToUnlock;

        public float masteryPointsToUnlock;

        public List<AbilityDef> requiresAbilities;
        public List<AbilityDef> requiresAbilitiesOneOf;
        public List<AbilityDef> cannotGainWithAbilities;

        public List<GeneDef> requiredGenes;

        public List<TraitDef> requiredTraits;

        public List<TraitDef> requiredTraitsOneOf;

        public List<HediffDef> requiredHediffs;

        public int minHediffCount = 1;

        public int minAge = -1;

        public string cannotAcquireReasonOverride;

        public bool RequirementSatisfied(AbilityClass abilityClass, Ability ability)
        {
            var result = RequirementSatisfied(abilityClass, ability, out var reason);
            if (result is false)
            {
                TMagicUtils.Message(abilityClass.pawn + " - cannot gain " + def + " because of " + reason);
            }
            else if (def.autoGain is false)
            {
                TMagicUtils.Message(abilityClass.pawn + " - should gain " + def + " with autoGain set to false, so it can be learned via other means only");
            }
            return result;
        }

        public bool RequirementSatisfied(AbilityClass abilityClass, Ability ability, out string failReason)
        {
            failReason = cannotAcquireReasonOverride;

            if (abilityClass.def.usesSkillPointSystem)
            {
                if (abilityClass.skillPoints < skillPointsToUnlock)
                {
                    return false;
                }
                if (abilityClass.curSpentSkillPoints + skillPointsToUnlock > abilityClass.MaxLevel)
                {
                    return false;
                }
            }
            if (masteryPointsToUnlock > 0 && ability != null)
            {
                if (masteryPointsToUnlock > ability.masteryPoints)
                {
                    return false;
                }
            }

            if (requiredMinimumLevels != null && !requiredMinimumLevels
                .All(x => abilityClass.compAbilities.GetAbilityClass(x.abilityClass)?.level >= x.minLevel))
            {
                if (requiredMinimumLevels.Count > 1)
                {
                    failReason ??= "TMF.RequiresAbilityClassLevels".Translate(string.Join(", ", requiredMinimumLevels.Select(x => x.abilityClass.label + ": " + x.minLevel)));
                }
                else
                {
                    failReason ??= "TMF.RequiresAbilityClassLevel".Translate(requiredMinimumLevels[0].abilityClass.label + ": " + requiredMinimumLevels[0].minLevel);
                }
                return false;
            }
            if (requiredMinimumAbilityLevels != null && !requiredMinimumAbilityLevels
    .All(x => abilityClass.GetLearnedAbility(x.ability)?.level >= x.minLevel))
            {
                if (requiredMinimumAbilityLevels.Count > 1)
                {
                    failReason ??= "TMF.RequiresAbilityLevels".Translate(string.Join(", ", requiredMinimumAbilityLevels.Select(x => x.ability.label + ": " + x.minLevel + 1)));
                }
                else
                {
                    failReason ??= "TMF.RequiresAbilityLevel".Translate(requiredMinimumAbilityLevels[0].ability.label + ": " + requiredMinimumLevels[0].minLevel + 1);
                }
                return false;
            }
            if (requiredMinimumSkillLevels != null && !requiredMinimumSkillLevels
                .All(x => abilityClass.pawn.skills?.GetSkill(x.skill)?.Level >= x.minLevel))
            {
                if (requiredMinimumSkillLevels.Count > 1)
                {
                    failReason ??= "TMF.RequiresSkillLevels".Translate(string.Join(", ", requiredMinimumSkillLevels.Select(x => x.skill.label + ": " + x.minLevel)));
                }
                else
                {
                    failReason ??= "TMF.RequiresSkillLevel".Translate(requiredMinimumSkillLevels[0].skill.label + ": " + requiredMinimumSkillLevels[0].minLevel);
                }
                return false;
            }
            if (requiresAbilities != null && !requiresAbilities.All(x => abilityClass.Learned(x)))
            {
                failReason ??= RequiresDefs(requiresAbilities.Cast<Def>().ToList(), "TMF.RequiresAbility", "TMF.RequiresAbilities");
                return false;
            }
            if (requiresAbilitiesOneOf != null && !requiresAbilitiesOneOf.Any(x => abilityClass.Learned(x)))
            {
                failReason ??= RequiresDefs(requiresAbilitiesOneOf.Cast<Def>().ToList(), "TMF.RequiresAbility", "TMF.RequiresAbilitiesOneOf");
                return false;
            }
            if (cannotGainWithAbilities != null && cannotGainWithAbilities.Any(x => abilityClass.Learned(x)))
            {
                failReason ??= "TMF.CannotGainWithAbility".Translate(cannotGainWithAbilities
                    .First(x => abilityClass.Learned(x)).label);
                return false;
            }
            if (requiredGenes != null && !requiredGenes.All(x => abilityClass.pawn.genes?.HasActiveGene(x) ?? false))
            {
                failReason ??= RequiresDefs(requiredGenes.Cast<Def>().ToList(), "TMF.RequiresGene", "TMF.RequiresGenes");
                return false;
            }
            if (requiredHediffs != null && !requiredHediffs.All(x => abilityClass.pawn.health.hediffSet.hediffs.Count(y => x == y.def) >= minHediffCount))
            {
                failReason ??= RequiresDefs(requiredHediffs.Cast<Def>().ToList(), "TMF.RequiresHealthCondition", "TMF.RequiresHealthConditions");
                return false;
            }
            if (requiredTraits != null && !requiredTraits.All(x => abilityClass.pawn.story?.traits?.HasTrait(x) ?? false))
            {
                failReason ??= RequiresDefs(requiredTraits.Cast<Def>().ToList(), "TMF.RequiresTrait", "TMF.RequiresTraits");
                return false;
            }
            if (requiredTraitsOneOf != null && !requiredTraitsOneOf.Any(x => abilityClass.pawn.story?.traits?.HasTrait(x) ?? false))
            {
                failReason ??= RequiresDefs(requiredTraitsOneOf.Cast<Def>().ToList(), "TMF.RequiresTrait", "TMF.RequiresTraitsOneOf");
                return false;
            }
            if (minAge != -1 && abilityClass.pawn.ageTracker.AgeBiologicalYearsFloat < minAge)
            {
                failReason ??= "TMF.RequiresMinAge".Translate(minAge);
                return false;
            }
            if (abilityClass.level < minLevel)
            {
                failReason ??= "TMF.RequiresAbilityClassLevel".Translate(abilityClass.def.label + ": " + minLevel);
                return false;
            }
            failReason = "";
            return true;
        }

        private string RequiresDefs(List<Def> defs, string baseReason, string baseReasonPlural)
        {
            if (defs.Count > 1)
            {
                return baseReasonPlural.Translate(string.Join(", ", defs.Select(x => x.label)));
            }
            return baseReason.Translate(defs[0].label);
        }
    }
}
