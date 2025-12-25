using RimWorld;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using Verse;
using Verse.AI.Group;
using Verse.Sound;

namespace TaranMagicFramework
{
    public class HediffProps
    {
        public HediffDef hediff;
        public int durationTicks;
        public float severity;
        public bool onTarget;
    }

    public class XPGain
    {
        public AbilityClassDef abilityClass;
        public float xpGain;
        public float masteryGain;
        public int ticksInterval;
    }

    public class AbilityTierDef : Def
    {
        public string nextTierDescription;
        public string iconTexPath;
        public bool autoGain;
        public bool isLearnable = true;
        public List<AbilityDef> abilitiesToUnlock;
        public List<AbilityDef> abilitiesToUpgrade;
        public ThingDef equipmentSource;
        public AcquireRequirement acquireRequirement;
        public List<StatModifier> statOffsets;
        public List<StatModifier> statFactors;
        public List<PawnCapacityModifier> capMods;
        public List<VerbProperties> verbProperties;
        public List<Tool> tools;
        public TargetingParameters targetingParameters;
        public SoundDef soundOnCast;
        public HediffProps hediffProps;
        public OverlayProps overlayProps;
        public CompProperties_Glower glowProps;
        public float energyCost;
        public int cooldownTicks;
        public bool cooldownOnEnd = true;
        public bool cooldownOnStart;
        public float maxEnergyOffset;
        public float energyRateOffset;
        public float effectRadius;
        public float AOERadius;
        public float movementSpeedFactor = -1;
        public int durationTicks = -1;
        public int minEnergy = -1;
        public float? regenRate;
        public int maxCharge;
        public int initialCharges;
        public bool cooldownOnEmptyCharges;
        public int rechargeDuration;
        public int castDurationTicks;
        public bool requireLineOfSight;
        public ThingDef thingToSpawn;
        public JobDef jobDef;
        public bool canQueueCastJob = true;
        public bool instantAction = true;
        public float xpGainOnCast;
        public XPGain xpGainProps;
        public XPGain xpGainWhileActive;
        public bool activateOnGain;
        public List<HediffDef> cannotBeActiveWithHediffs;
        public List<HediffDef> hideWithHediffs;
        public List<HediffDef> visibleWithHediffs;
        public List<AbilityDef> cannotBeActiveWithOtherAbilitiesInUse;
        public List<AbilityDef> hideWithAbilities;
        public List<AbilityDef> requiresActiveAbilities;
        public List<AbilityDef> requiresActiveAbilitiesOneOf;
        public bool violent;
        public Type aiAbilityWorker;
        private AIAbilityWorker _aiAbilityWorker;
        public string letterTitleKeyGained;
        public string letterDescKeysGained;
        public int goodwillImpact;
        public bool canUseWhenDowned;
        public bool preventsFromBeingDowned;
        public AIAbilityWorker AIAbilityWorker
        {
            get
            {
                if (_aiAbilityWorker == null)
                {
                    _aiAbilityWorker = (AIAbilityWorker)Activator.CreateInstance(aiAbilityWorker);
                }
                return _aiAbilityWorker;
            }
        }

        public virtual TargetingParameters TargetingParameters(Pawn pawn)
        {
            if (targetingParameters != null)
            {
                targetingParameters.validator = delegate (TargetInfo x)
                {
                    if (effectRadius > 0)
                    {
                        if (x.Cell.DistanceTo(pawn.Position) > effectRadius)
                        {
                            return false;
                        }
                    }
                    if (requireLineOfSight)
                    {
                        if (!GenSight.LineOfSight(x.Cell, pawn.Position, pawn.Map))
                        {
                            return false;
                        }
                    }
                    return true;
                };
            }
            return  targetingParameters;
        }

        public virtual void Start(Ability ability)
        {
            if (xpGainOnCast != 0)
            {
                ability.abilityClass.GainXP(xpGainOnCast);
            }

            if (xpGainProps != null)
            {
                if (xpGainProps.xpGain > 0)
                {
                    if (xpGainProps.abilityClass != null)
                    {
                        if (xpGainProps.abilityClass != null && xpGainProps.abilityClass != ability.abilityClass.def)
                        {
                            var otherAbilityClass = ability.abilityClass.compAbilities.GetAbilityClass(xpGainProps.abilityClass);
                            if (otherAbilityClass != null)
                            {
                                otherAbilityClass.GainXP(xpGainProps.xpGain);
                            }
                        }
                        else
                        {
                            ability.abilityClass.GainXP(xpGainProps.xpGain);
                        }
                    }
                }

                if (xpGainProps.masteryGain > 0)
                {
                    ability.GainMasteryPoints(xpGainProps.masteryGain);
                }

            }

            if (soundOnCast != null)
            {
                if (soundOnCast.sustain)
                {
                    ability.soundPlaying = soundOnCast.TrySpawnSustainer(SoundInfo.InMap(ability.pawn, MaintenanceType.PerTick));
                }
                else
                {
                    soundOnCast.PlayOneShot(ability.pawn);
                }
            }

            if (thingToSpawn != null)
            {
                ability.SpawnItem(thingToSpawn);
            }
            if (hediffProps != null)
            {
                ability.AddHediff(hediffProps);
            }

            if (ability.pawn.MapHeld != null && overlayProps != null && overlayProps.overlayOnStart)
            {
                ability.MakeAnimation(overlayProps);
            }

            if (glowProps != null && ability.ShouldBeLitNow)
            {
                ability.UpdateGlower();
            }
            if (cooldownOnStart)
            {
                ability.SetCooldown(ability.CooldownTicks);
            }
            if (goodwillImpact != 0 && ability.curTarget.Thing is Pawn pawnTarget && pawnTarget.HomeFaction is not null 
                && pawnTarget.HomeFaction != ability.pawn.Faction && pawnTarget.HomeFaction.HostileTo(ability.pawn.Faction) is false)
            {
                ability.pawn.Faction.TryAffectGoodwillWith(pawnTarget.HomeFaction, goodwillImpact, canSendMessage: true, canSendHostilityLetter: true, HistoryEventDefOf.UsedHarmfulAbility);
            }
            if (preventsFromBeingDowned && ability.pawn.Downed)
            {
                ability.pawn.health.healthState = PawnHealthState.Mobile;
                PortraitsCache.SetDirty(ability.pawn);
                GlobalTextureAtlasManager.TryMarkPawnFrameSetDirty(ability.pawn);
                if (ability.pawn.guest != null)
                {
                    ability.pawn.guest.Notify_PawnUndowned();
                }
                ability.pawn.GetLord()?.Notify_PawnUndowned(ability.pawn);
            }
        }
        public virtual float EnergyCostFor(Pawn pawn, AbilityResourceDef abilityResourceDef, bool doMasteryCheck = true)
        {
            if (abilityResourceDef.energyUsageMultiplierStat != null)
            {
                return energyCost * pawn.GetStatValue(abilityResourceDef.energyUsageMultiplierStat);
            }
            return energyCost;
        }

        public override void PostLoad()
        {
            base.PostLoad();
            if (!iconTexPath.NullOrEmpty())
            {
                LongEventHandler.ExecuteWhenFinished(() => icon = ContentFinder<Texture2D>.Get(iconTexPath));
            }
        }

        public string GetTierInfo(Pawn pawn, AbilityClassDef abilityClassDef, Ability ability, AbilityDef def, bool includeDescription, bool showSkillPoints)
        {
            var sb = new StringBuilder();
            foreach (var data in GetTierData(pawn, abilityClassDef, ability, def, includeDescription, showSkillPoints))
            {
                sb.AppendLine(data);
            }
            return sb.ToString().TrimEndNewlines();
        }

        protected virtual IEnumerable<string> GetTierData(Pawn pawn, AbilityClassDef abilityClassDef, Ability ability, AbilityDef def, bool includeDescription, bool showSkillPoints)
        {
            if (includeDescription)
            {
                if (description.NullOrEmpty() is false)
                {
                    yield return description;
                }
                else if (def.description.NullOrEmpty() is false)
                {
                    yield return def.description;
                }
            }

            if (showSkillPoints && acquireRequirement != null)
            {
                if (isLearnable && acquireRequirement.skillPointsToUnlock > 0)
                {
                    yield return "TMF.SkillPointsToLearn".Translate(acquireRequirement.skillPointsToUnlock);
                }
                if (acquireRequirement.masteryPointsToUnlock > 0)
                {
                    yield return "TMF.MasteryPointsToLearn".Translate(acquireRequirement.masteryPointsToUnlock);
                }
            }
            if (abilityClassDef.abilityResource != null)
            {
                var energyCost = EnergyCostFor(pawn, abilityClassDef.abilityResource);
                if (energyCost != 0)
                {
                    yield return "TMF.EnergyCost".Translate(abilityClassDef.abilityResource.label, energyCost);
                }
                if (ability != null && ability.MaxEnergyOffset != 0)
                {
                    yield return "TMF.MaxEnergyOffset".Translate(ability.MaxEnergyOffset.ToStringDecimalIfSmall());
                }
                else if (maxEnergyOffset != 0)
                {
                    yield return "TMF.MaxEnergyOffset".Translate(maxEnergyOffset.ToStringDecimalIfSmall());
                }
                if (ability?.ResourceRegenRate != null)
                {
                    if (ability.ResourceRegenRate.Value > 0)
                    {
                        yield return "TMF.EnergyRegenRate".Translate((ability.ResourceRegenRate.Value * 60f).ToStringDecimalIfSmall());
                    }
                    else
                    {
                        yield return "TMF.EnergyDrainRate".Translate((ability.ResourceRegenRate.Value * 60f).ToStringDecimalIfSmall());
                    }
                }
                else if (regenRate.HasValue)
                {
                    if (regenRate.Value > 0)
                    {
                        yield return "TMF.EnergyRegenRate".Translate((regenRate.Value * 60f).ToStringDecimalIfSmall());
                    }
                    else
                    {
                        yield return "TMF.EnergyDrainRate".Translate((regenRate.Value * 60f).ToStringDecimalIfSmall());
                    }
                }
            }

            if (movementSpeedFactor != -1)
            {
                yield return "TMF.MovementSpeedFactor".Translate(movementSpeedFactor.ToStringPercent());
            }
            if (durationTicks != -1)
            {
                yield return "TMF.Duration".Translate(durationTicks.ToStringSecondsFromTicks());
            }

            if (cooldownTicks != 0)
            {
                yield return "TMF.CooldownTime".Translate(cooldownTicks.ToStringTicksToPeriod());
            }
            if (effectRadius != 0)
            {
                yield return "TMF.Radius".Translate(effectRadius.ToStringDecimalIfSmall());
            }
            if (AOERadius != 0)
            {
                yield return "TMF.AOERadius".Translate(AOERadius.ToStringDecimalIfSmall());
            }

            if (verbProperties != null)
            {
                foreach (var verbProps in verbProperties)
                {
                    if (verbProps is VerbPropertiesAbility verbPropsKI)
                    {
                        if (verbPropsKI.range != 0)
                        {
                            yield return "TMF.Radius".Translate(verbPropsKI.range.ToStringDecimalIfSmall());
                        }
                    }
                }
            }

            if (statOffsets != null)
            {
                foreach (var stat in statOffsets)
                {
                    yield return stat.stat.LabelCap + " " + stat.ValueToStringAsOffset;
                }
            }

            if (statFactors != null)
            {
                foreach (var stat in statFactors)
                {
                    yield return stat.stat.LabelCap + " " + stat.ToStringAsFactor;
                }
            }
            if (capMods != null)
            {
                foreach (var pawnCapacityModifier in capMods)
                {
                    if (pawnCapacityModifier.offset != 0f)
                    {
                        yield return pawnCapacityModifier.capacity.GetLabelFor(pawn).CapitalizeFirst() + " " + (pawnCapacityModifier.offset * 100f).ToString("+#;-#") + "%";
                    }
                    if (pawnCapacityModifier.postFactor != 1f)
                    {
                        yield return pawnCapacityModifier.capacity.GetLabelFor(pawn).CapitalizeFirst() + " x" + pawnCapacityModifier.postFactor.ToStringPercent();
                    }
                    if (pawnCapacityModifier.setMax != 999f)
                    {
                        yield return pawnCapacityModifier.capacity.GetLabelFor(pawn).CapitalizeFirst() + " " + "max".Translate().CapitalizeFirst() + ": " + pawnCapacityModifier.setMax.ToStringPercent();
                    }
                }
            }
            if (ability != null && ability.HasCharge)
            {
                yield return "TMF.CurCharges".Translate(ability.CurCharges, maxCharge);
            }
            else if (maxCharge > 0)
            {
                yield return "TMF.MaxCharges".Translate(maxCharge);
            }

            if (xpGainOnCast != 0)
            {
                yield return "TMF.XPGainOnCast".Translate(xpGainOnCast.ToStringDecimalIfSmall());
            }
            if (xpGainWhileActive != null)
            {
                if (xpGainWhileActive.xpGain > 0)
                {
                    yield return "TMF.XPGainWhileActive".Translate(xpGainWhileActive.xpGain.ToStringDecimalIfSmall(), xpGainWhileActive.ticksInterval.ToStringTicksToPeriod());
                }
                if (xpGainWhileActive.masteryGain > 0)
                {
                    yield return "TMF.MasteryGainWhileActive".Translate(xpGainWhileActive.masteryGain.ToStringDecimalIfSmall(), xpGainWhileActive.ticksInterval.ToStringTicksToPeriod());
                }
            }
        }

        public Texture2D icon;
    }
}
