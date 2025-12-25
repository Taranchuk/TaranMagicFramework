using RimWorld;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace TaranMagicFramework
{
    public class AbilityModGroupDef : Def
    {

    }
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
    public class HotSwappableAttribute : Attribute
    {
    }

    [HotSwappable]
    public class Ability : IExposable, ILoadReferenceable
    {
        public int RechargeDuration => AbilityTier.rechargeDuration;
        public virtual int MaxCharge => AbilityTier.maxCharge;
        private int curCharges;
        public virtual int CurCharges { get => curCharges; set => curCharges = value; }
        public int chargingTicks;
        public bool activateOnceSpawned;
        public List<Mote_Animation> animations = new List<Mote_Animation>();
        public CompAbilities compAbilities;
        public AbilityClass abilityClass;
        public AbilityResource abilityResource;
        public Hediff createdHediff;
        public Pawn pawn;
        public AbilityDef def;
        public int level;
        public float masteryPoints;
        public float masteryPointsSinceLastLevel;

        public float GainedMasteryPointsSinceLastLevel => masteryPoints - masteryPointsSinceLastLevel;

        public virtual float RequiredMasteryPointsForNewLevel => MaxLevel == level ? def.abilityTiers[level].acquireRequirement.masteryPointsToUnlock : def.abilityTiers[level + 1].acquireRequirement.masteryPointsToUnlock;

        public static Dictionary<AbilityDef, HashSet<Ability>> allAbilitiesByDef = new();
        public int LevelHumanReadable => level + 1;
        public int MaxLevel => def.abilityTiers.Count - 1;
        public bool FullyLearned => MaxLevel == level;
        public int lastActivatedTick, lastEndedTick;
        public int cooldownPeriod;
        public virtual float MaxEnergyOffset => Active ? AbilityTier.maxEnergyOffset : 0;
        public virtual int CastTicks
        {
            get
            {
                var castTime = AbilityTier.castDurationTicks;
                if (abilityClass.def.castTimeMultiplierStat != null)
                {
                    castTime = (int)(castTime * TMagicUtils.GetStatValueForStat(compAbilities,
                        abilityClass.def.castTimeMultiplierStat));
                }
                return castTime;
            }
        }

        public virtual int CooldownTicks
        {
            get
            {
                var cooldownTicks = AbilityTier.cooldownTicks;
                if (abilityClass.def.cooldownTicksMultiplierStat != null)
                {
                    cooldownTicks = (int)(cooldownTicks * TMagicUtils.GetStatValueForStat(compAbilities,
                        abilityClass.def.cooldownTicksMultiplierStat));
                }
                return cooldownTicks;
            }
        }
        private bool isActive;
        public virtual bool Active
        {
            get
            {
                return isActive;
            }
            set 
            { 

                isActive = value;
            }
        }
        public int abilityID;
        public int learnedFirstTimeTick;
        public Dictionary<int, VerbCollection> verbsByAbilityLevels = new();
        public LocalTargetInfo curTarget;
        public AbilityTierDef AbilityTier => def.abilityTiers[level];
        public virtual bool HasCharge => AbilityTier.maxCharge > 0;

        public bool autocastEnabled;
        public virtual bool AutocastEnabled => autocastEnabled;
        public virtual float EnergyCost => abilityResource != null ? AbilityTier.EnergyCostFor(pawn, abilityResource.def) : 0;
        public virtual float EnergyCostNoMasteryCheck => abilityResource != null ? AbilityTier.EnergyCostFor(pawn, abilityResource.def, false) : 0;
        public virtual bool IsInstantAction => Verbs.Any() is false && AbilityTier.instantAction && AbilityTier.durationTicks == -1;
        public virtual TargetingParameters TargetingParameters => AbilityTier.TargetingParameters(pawn);
        
        public virtual Texture2D AbilityIcon()
        {
            if (def.abilityTiers[level].icon is null)
            {
                return def.icon;
            }
            return def.abilityTiers[level].icon;
        }
        public Texture2D AbilityIcon(Verb verb)
        {
            if (verb != null && verb.UIIcon != BaseContent.BadTex)
            {
                return verb.UIIcon;
            }
            return AbilityIcon();
        }

        public virtual Func<string> CanBeActivatedValidator() => null;
        public virtual bool ShouldShowGizmos
        {
            get
            {
                if (pawn.Drafted && def.showWhenDrafted is false)
                {
                    return false;
                }
                else if (pawn.Drafted is false && def.showWhenNotDrafted is false)
                {
                    return false;
                }
                var equipment = AbilityTier.equipmentSource;
                if (equipment != null)
                {
                    if ((pawn.apparel.WornApparel.Any(x => x.def == equipment)
                        || pawn.equipment.AllEquipmentListForReading.Any(x => x.def == equipment)) is false)
                    {
                        return false;
                    }
                }
                if (pawn.InMentalState && CanUseWhileInMentalState is false)
                {
                    return false;
                }
                if (def.visibleWhenActive.NullOrEmpty() is false)
                {
                    return def.visibleWhenActive.All(x => abilityClass.GetLearnedAbility(x)?.Active ?? false);
                }
                if (AbilityTier.hideWithHediffs.NullOrEmpty() is false)
                {
                    if (AbilityTier.hideWithHediffs.Any(x => pawn.health.hediffSet.GetFirstHediffOfDef(x) != null))
                    {
                        return false;
                    }
                }
                if (AbilityTier.hideWithAbilities.NullOrEmpty() is false)
                {
                    if (AbilityTier.hideWithAbilities.Any(x => abilityClass.GetLearnedAbility(x) != null))
                    {
                        return false;
                    }
                }
                if (AbilityTier.visibleWithHediffs.NullOrEmpty() is false)
                {
                    if (AbilityTier.visibleWithHediffs.Any(x => pawn.health.hediffSet.GetFirstHediffOfDef(x) == null))
                    {
                        return false;
                    }
                }
                return true;
            }
        }

        public virtual float? ResourceRegenRate => AbilityTier.regenRate;
        public virtual void Init()
        {
            InitData();
            verbsByAbilityLevels ??= new Dictionary<int, VerbCollection>();
            verbsByAbilityLevels.RemoveAll(x => x.Value is null);
            UpdateVerbData();
            var abilityTier = AbilityTier;
            CreateVerbs(abilityTier);
            if (abilityClass != null)
            {
                abilityResource = abilityClass.abilityResource;
                if (abilityTier.abilitiesToUnlock != null)
                {
                    foreach (var ability in abilityTier.abilitiesToUnlock)
                    {
                        if (ability != def && !abilityClass.Learned(ability))
                        {
                            abilityClass.LearnAbility(ability, abilityClass.UsesSkillPointSystem, 0);
                        }
                    }
                }

                if (abilityTier.abilitiesToUpgrade != null)
                {
                    foreach (var ability in abilityTier.abilitiesToUpgrade)
                    {
                        if (ability != def)
                        {
                            if (!abilityClass.Learned(ability))
                            {
                                abilityClass.LearnAbility(ability, abilityClass.UsesSkillPointSystem, 0);
                            }
                            else
                            {
                                if (abilityClass.CanUnlockNextTier(ability, out _, out _))
                                {
                                    var otherAbility = abilityClass.GetLearnedAbility(ability);
                                    otherAbility.ChangeLevel(otherAbility.level + 1);
                                }
                            }
                        }
                    }
                }
                foreach (var otherDef in pawn.AllAvailableAbilities())
                {
                    if (otherDef != def && !abilityClass.Learned(otherDef))
                    {
                        if (otherDef.unlockedWhenMasteredList != null
                            && otherDef.unlockedWhenMasteredList.Any(x => x.entry.All(y => abilityClass.FullyLearned(y))))
                        {
                            abilityClass.LearnAbility(otherDef, abilityClass.UsesSkillPointSystem, 0);
                        }
                        else if (otherDef.unlockedWhenMastered != null
                            && otherDef.unlockedWhenMastered.Any(x => abilityClass.FullyLearned(x)))
                        {
                            abilityClass.LearnAbility(otherDef, abilityClass.UsesSkillPointSystem, 0);
                        }
                    }
                }
            }
        }

        private void CreateVerbs(AbilityTierDef abilityTier)
        {
            if (abilityTier.verbProperties != null)
            {
                if (!verbsByAbilityLevels.TryGetValue(level, out var verbCollection))
                {
                    verbsByAbilityLevels[level] = verbCollection = new VerbCollection();
                }
                verbCollection.verbs ??= new List<Verb_ShootAbility>();
                if (!verbCollection.verbs.Any())
                {
                    for (int i = 0; i < abilityTier.verbProperties.Count; i++)
                    {
                        var verbProps = abilityTier.verbProperties[i];
                        var verb = (Verb_ShootAbility)Activator.CreateInstance(verbProps.verbClass);
                        verb.loadID = GetUniqueLoadID() + "_Verb_" + i + "_Level" + level;
                        verb.verbProps = verbProps;
                        verb.verbTracker = pawn?.verbTracker;
                        verb.caster = pawn;
                        verb.ability = this;
                        verbCollection.verbs.Add(verb);
                    }
                }
            }

            if (abilityTier.tools != null)
            {
                if (!verbsByAbilityLevels.TryGetValue(level, out var verbCollection))
                {
                    verbsByAbilityLevels[level] = verbCollection = new VerbCollection();
                }
                verbCollection.meleeVerbs ??= new List<Verb_MeleeAttackDamageAbility>();
                if (verbCollection.meleeVerbs.Any() is false)
                {
                    var pairs = new List<(Tool tool, ManeuverDef maneuver)>();
                    foreach (var tool in abilityTier.tools)
                    {
                        foreach (var maneuver in tool.Maneuvers)
                        {
                            pairs.Add((tool, maneuver));
                        }
                    }

                    for (int i = 0; i < pairs.Count; i++)
                    {
                        var maneuver = pairs[i].maneuver;
                        var verbProps = maneuver.verb;
                        var verb = (Verb_MeleeAttackDamageAbility)Activator.CreateInstance(verbProps.verbClass);
                        verb.loadID = GetUniqueLoadID() + "_" + maneuver.defName + "_MeleeVerb_" + i + "_Level" + level;
                        verb.verbProps = verbProps;
                        verb.tool = pairs[i].tool;
                        verb.verbTracker = pawn?.verbTracker;
                        verb.caster = pawn;
                        verb.ability = this;
                        verbCollection.meleeVerbs.Add(verb);
                    }
                }
            }
        }

        private void InitData()
        {
            compAbilities = pawn.GetComp<CompAbilities>();
            if (!allAbilitiesByDef.TryGetValue(def, out var list))
            {
                allAbilitiesByDef[def] = list = new HashSet<Ability>
                {
                    this
                };
            }
            else
            {
                list.Add(this);
            }
        }

        public virtual void ChangeLevel(int newLevel)
        {
            TMagicUtils.Message(def.label + " - Changing ability tier from " + level + " to " + newLevel, pawn);
            level = newLevel;
            if (PawnUtility.ShouldSendNotificationAbout(pawn) && AbilityTier.letterTitleKeyGained.NullOrEmpty() is false)
            {
                Find.LetterStack.ReceiveLetter(AbilityTier.letterTitleKeyGained.Translate(pawn.Named("PAWN")),
                    AbilityTier.letterDescKeysGained.Translate(pawn.Named("PAWN")), LetterDefOf.PositiveEvent, pawn);
            }
            Init();
        }

        public virtual void OnLearned()
        {
            TMagicUtils.Message("OnLearned: " + this, pawn);

            if (AbilityTier.initialCharges > 0)
            {
                curCharges = AbilityTier.initialCharges;
            }
        }

        public virtual void OnRemoved()
        {
            TMagicUtils.Message("OnRemoved: " + this, pawn);
            if (Active)
            {
                End();
            }
        }

        public Sustainer soundPlaying;
        public virtual void Start(bool consumeEnergy = true)
        {
            TMagicUtils.Message("Starting: " + this, pawn);
            if (def.endAbilitiesWhenActive != null)
            {
                foreach (var otherAbilityDef in def.endAbilitiesWhenActive)
                {
                    var otherAbility = pawn.GetAbility(otherAbilityDef);
                    if (otherAbility != null && otherAbility.Active) 
                    {
                        otherAbility.End();
                    }
                }
            }
            if (HasCharge)
            {
                CurCharges--;
            }
            if (abilityResource != null && consumeEnergy && EnergyCost > 0)
            {
                ConsumeEnergy(EnergyCost);
            }
            RegisterCast();
            lastActivatedTick = Find.TickManager.TicksGame;
            Active = true;
            AbilityTier.Start(this);
            pawn.health.hediffSet.DirtyCache();
            pawn.ResolveAllGraphicsSafely();
            if (IsInstantAction)
            {
                End();
            }
        }

        public virtual Hediff AddHediff(HediffProps hediffProps, Pawn pawnTargetOverride = null)
        {
            var target = pawnTargetOverride != null ? pawnTargetOverride : hediffProps.onTarget ? curTarget.Pawn : pawn;
            createdHediff = HediffMaker.MakeHediff(hediffProps.hediff, target);
            if (hediffProps.severity != 0)
            {
                createdHediff.Severity = hediffProps.severity;
            }
            if (hediffProps.durationTicks != 0)
            {
                createdHediff.TryGetComp<HediffComp_Disappears>().ticksToDisappear = hediffProps.durationTicks;
            }
            if (createdHediff is HediffAbility hediffAbility)
            {
                hediffAbility.ability = this;
            }
            target.health.AddHediff(createdHediff);
            return createdHediff;
        }

        public DamageWorker.DamageResult DoDamage(Thing thing, DamageInfo damageInfo)
        {
            return abilityClass.DoDamage(thing, damageInfo);
        }
        public virtual Thing SpawnItem(ThingDef thingToSpawn)
        {
            var position = curTarget.IsValid ? curTarget.Cell : pawn.Position;
            var thing = GenSpawn.Spawn(thingToSpawn, position, pawn.Map);
            if (thing.def.CanHaveFaction)
            {
                thing.SetFaction(pawn.Faction);
            }
            return thing;
        }

        public virtual AnimationDef AnimationDef(OverlayProps overlayProps) => overlayProps.overlay;
        public virtual Mote_Animation MakeAnimation(OverlayProps overlayProps)
        {
            var animation = MakeAnimation(AnimationDef(overlayProps));
            animations.Add(animation);
            animation.exactPosition = pawn.PositionHeld.ToVector3Shifted();
            animation.Scale = overlayProps.scale;
            animation.expireInTick = overlayProps.duration;
            var target = overlayProps.onTarget ? curTarget.Thing : pawn;
            animation.Attach(target, Vector3.zero);
            GenSpawn.Spawn(animation, target.PositionHeld, target.MapHeld);
            return animation;
        }

        public Mote_Animation MakeAnimation(AnimationDef overlay)
        {
            var mote = ThingMaker.MakeThing(overlay) as Mote_Animation;
            mote.sourceAbility = this;
            return mote;
        }

        private List<Tuple<Effecter, TargetInfo, TargetInfo>> maintainedEffecters = new List<Tuple<Effecter, TargetInfo, TargetInfo>>();
        public void AddEffecterToMaintain(Effecter eff, IntVec3 pos, int ticks, Map map = null)
        {
            eff.ticksLeft = ticks;
            TargetInfo targetInfo = new TargetInfo(pos, map ?? pawn.Map);
            maintainedEffecters.Add(new Tuple<Effecter, TargetInfo, TargetInfo>(eff, targetInfo, targetInfo));
        }

        public void AddEffecterToMaintain(Effecter eff, IntVec3 posA, IntVec3 posB, int ticks, Map map = null)
        {
            eff.ticksLeft = ticks;
            TargetInfo item = new TargetInfo(posA, map ?? pawn.Map);
            TargetInfo item2 = new TargetInfo(posB, map ?? pawn.Map);
            maintainedEffecters.Add(new Tuple<Effecter, TargetInfo, TargetInfo>(eff, item, item2));
        }
        public virtual void End()
        {
            TMagicUtils.Message("Ending: " + this, pawn);
            Active = false;
            soundPlaying?.End();
            soundPlaying = null;
            var abilityTier = AbilityTier;
            if (abilityTier.overlayProps?.destroyOverlaysOnEnd ?? false)
            {
                DestroyOverlay();
            }
            RemoveGlower();
            lastEndedTick = Find.TickManager.TicksGame;

            if (abilityTier.cooldownOnEnd)
            {
                SetCooldown(CooldownTicks);
            }
            if (abilityTier.hediffProps != null && !IsInstantAction)
            {
                if (createdHediff != null)
                {
                    createdHediff.pawn.health.RemoveHediff(createdHediff);
                    createdHediff = null;
                }
            }
            if (def.endAbilitiesWhenEnded != null)
            {
                foreach (var abilityDef in def.endAbilitiesWhenEnded)
                {
                    foreach (var abilityClass in compAbilities.abilityClasses)
                    {
                        var otherAbility = abilityClass.Value.GetLearnedAbility(abilityDef);
                        if (otherAbility != null && otherAbility.Active) 
                        {
                            otherAbility.End();
                        }
                    }
                }
            }
            pawn.health.hediffSet.DirtyCache();
            pawn.ResolveAllGraphicsSafely();
        }

        public virtual void DestroyOverlay()
        {
            foreach (var animation in animations.ToList())
            {
                if (animation != null && animation.expireInTick <= 0 && animation.AnimationDef.maxLoopCount <= 0 
                    && !animation.Destroyed)
                {
                    animation.Destroy();
                }
            }
            animations.RemoveAll(x => x is null || x.Destroyed);
            pawn.ResolveAllGraphicsSafely();
        }

        public virtual void DestroyAllOverlay()
        {
            foreach (var animation in animations.ToList())
            {
                if (animation != null && !animation.Destroyed)
                {
                    animation.Destroy();
                }
            }
            animations.RemoveAll(x => x.Destroyed);
            pawn.ResolveAllGraphicsSafely();
        }
        public virtual void RegisterCast()
        {

        }

        public virtual void PostSpawnSetup(bool respawningAfterLoad)
        {
            dirty = true;
        }

        public virtual void PostDeSpawn(Map map)
        {
            RemoveGlower(map);
        }

        public virtual void PostDestroy(DestroyMode mode, Map previousMap)
        {
            RemoveGlower(previousMap);
        }
        public virtual void ExposeData()
        {
            Scribe_References.Look(ref pawn, "pawn");
            if (pawn != null)
            {
                InitData();
            }
            Scribe_References.Look(ref abilityClass, "abilityClass");
            Scribe_References.Look(ref abilityResource, "abilityResource");
            Scribe_Defs.Look(ref def, "def");
            Scribe_Values.Look(ref abilityID, "abilityID");
            Scribe_Values.Look(ref level, "level");
            Scribe_Values.Look(ref masteryPoints, "masteryPoints");
            Scribe_Values.Look(ref masteryPointsSinceLastLevel, "masteryPointsSinceLastLevel");
            Scribe_Values.Look(ref lastActivatedTick, "lastActivatedTick");
            Scribe_Values.Look(ref lastEndedTick, "lastEndedTick");
            Scribe_Values.Look(ref cooldownPeriod, "cooldownPeriod");
            Scribe_Values.Look(ref isActive, "isActive");
            Scribe_Values.Look(ref autocastEnabled, "autocastEnabled");
            Scribe_Collections.Look(ref verbsByAbilityLevels, "verbsByAbilityLevels", LookMode.Value, LookMode.Deep);
            Scribe_Collections.Look(ref animations, "animations", LookMode.Reference);
            Scribe_References.Look(ref createdHediff, "createdHediff");
            Scribe_Values.Look(ref learnedFirstTimeTick, "learnedFirstTimeTick");
            Scribe_Values.Look(ref activateOnceSpawned, "activateOnceSpawned");
            Scribe_Values.Look(ref curCharges, "curCharges");
            Scribe_Values.Look(ref chargingTicks, "chargingTicks");
            Scribe_TargetInfo.Look(ref curTarget, "curTarget");
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                animations ??= new List<Mote_Animation>();
                verbsByAbilityLevels ??= new Dictionary<int, VerbCollection>();
            }
        }

        public void UpdateVerbData()
        {
            if (pawn != null)
            {
                foreach (var kvp in verbsByAbilityLevels)
                {
                    if (kvp.Value.verbs.Any())
                    {
                        var verbProperties = def.abilityTiers[kvp.Key].verbProperties;
                        for (int i = 0; i < verbProperties.Count; i++)
                        {
                            var verb = kvp.Value.verbs[i];
                            verb.verbProps = verbProperties[i];
                            verb.verbTracker = pawn.verbTracker;
                            verb.caster = pawn;
                            verb.ability = this;
                        }
                    }

                    if (kvp.Value.meleeVerbs.Any())
                    {
                        var pairs = new List<(Tool tool, ManeuverDef maneuver)>();
                        foreach (var tool in def.abilityTiers[kvp.Key].tools)
                        {
                            foreach (var maneuver in tool.Maneuvers)
                            {
                                pairs.Add((tool, maneuver));
                            }
                        }

                        for (int i = 0; i < pairs.Count; i++)
                        {
                            var verb = kvp.Value.meleeVerbs[i];
                            verb.verbProps = pairs[i].maneuver.verb;
                            verb.tool = pairs[i].tool;
                            verb.verbTracker = pawn.verbTracker;
                            verb.caster = pawn;
                            verb.ability = this;
                        }
                    }
                }
            }
        }

        public void ConsumeEnergy(float energyCost)
        {
            abilityResource.energy -= energyCost;
        }

        public virtual void SetCooldown(int cooldownTicks)
        {
            if (HasCharge && AbilityTier.cooldownOnEmptyCharges && curCharges > 0)
            {
                return;
            }
            cooldownPeriod = cooldownTicks;
        }
        public IEnumerable<Verb> Verbs
        {
            get
            {
                if (verbsByAbilityLevels.TryGetValue(level, out var verbCollection))
                {
                    if (verbCollection.verbs != null)
                    {
                        foreach (var verb in verbCollection.verbs)
                        {
                            yield return verb;
                        }
                    }

                    if (verbCollection.meleeVerbs != null)
                    {
                        foreach (var verb in verbCollection.meleeVerbs)
                        {
                            yield return verb;
                        }
                    }
                }
            }
        }


        public virtual IEnumerable<Gizmo> GetGizmos()
        {
            if (Verbs.Any())
            {
                foreach (var verb in Verbs)
                {
                    if (verb.VerbPropsAbility().autocast)
                    {
                        yield return GetCommand_ActivateAbilityAutocastToggle(verb);
                    }
                    else
                    {
                        yield return GetCommandActivateAbility(verb);
                    }
                }
            }
            else
            {
                yield return GetCommandActivateAbility();
            }
        }

        public Command_ActivateAbilityAutocastToggle GetCommand_ActivateAbilityAutocastToggle(Verb verb = null)
        {
            return new Command_ActivateAbilityAutocastToggle(this)
            {
                icon = AbilityIcon(verb),
                defaultLabel = AbilityLabel(),
                defaultDesc = AbilityDescription(),
                action = delegate
                {
                    Cast(verb, LocalTargetInfo.Invalid);
                },
                disabled = !CanBeActivated(EnergyCost, out string failReason, canBeActivatedValidator: CanBeActivatedValidator()),
                disabledReason = failReason,
            };
        }

        public Command_ActivateAbility GetCommandActivateAbility(Verb verb = null)
        {
            return new Command_ActivateAbility(this)
            {
                icon = AbilityIcon(verb),
                defaultLabel = AbilityLabel(),
                defaultDesc = AbilityDescription(),
                action = delegate
                {
                    Cast(verb, LocalTargetInfo.Invalid);
                },
                onHover = delegate
                {
                    if (TargetingParameters is null)
                    {
                        if (AbilityTier.effectRadius > 0)
                        {
                            GenDraw.DrawRadiusRing(pawn.Position, AbilityTier.effectRadius);
                        }
                        else if (AbilityTier.AOERadius > 0)
                        {
                            GenDraw.DrawRadiusRing(pawn.Position, AbilityTier.AOERadius);
                        }
                    }
                },
                disabled = !CanBeActivated(EnergyCost, out string failReason, canBeActivatedValidator: CanBeActivatedValidator()),
                disabledReason = failReason,
            };
        }

        public Command_ToggleAbility GetToggleAbilityGizmo()
        {
            return new Command_ToggleAbility(this)
            {
                icon = AbilityIcon(),
                defaultLabel = AbilityLabel(),
                defaultDesc = AbilityDescription(),
                toggleAction = delegate
                {
                    if (!Active)
                    {
                        DoCast(null);
                    }
                    else
                    {
                        End();
                    }
                },
                isActive = () => Active,
                disabled = !CanBeActivated(EnergyCost, out string failReason, allowDisabling: true, CanBeActivatedValidator()),
                disabledReason = failReason,
            };
        }

        public virtual bool IsValid(LocalTargetInfo target, bool throwMessages) => CanHitTarget(target);

        public virtual bool CanHitTarget(LocalTargetInfo target)
        {
            if (AbilityTier.requireLineOfSight)
            {
                return GenSight.LineOfSight(pawn.Position, target.Cell, pawn.Map);
            }
            return true;
        }
        public virtual void Cast(Verb verb, LocalTargetInfo target)
        {
            if (verb != null)
            {
                Find.Targeter.BeginTargeting(verb);
            }
            else
            {
                var targetParameters = TargetingParameters;
                if (targetParameters != null)
                {
                    Find.Targeter.BeginTargeting(targetParameters, delegate (LocalTargetInfo x)
                    {
                        if (IsValid(x, throwMessages: true))
                        {
                            DoCast(x);
                        }
                    }, highlightAction: delegate (LocalTargetInfo t)
                    {
                        var tier = AbilityTier;
                        if (tier.effectRadius > 0)
                        {
                            GenDraw.DrawRadiusRing(pawn.Position, tier.effectRadius);
                        }
                        if (tier.AOERadius > 0)
                        {
                            GenDraw.DrawRadiusRing(t.Cell, tier.AOERadius);
                        }
                        if (t.IsValid && CanHitTarget(t))
                        {
                            GenDraw.DrawTargetHighlightWithLayer(t.CenterVector3, AltitudeLayer.MetaOverlays);
                        }
                    }, (LocalTargetInfo t) => targetParameters.validator(t.ToTargetInfo(pawn.Map)), pawn);
                }
                else
                {
                    DoCast(null);
                }
            }
        }

        public void DoCast(LocalTargetInfo target)
        {
            curTarget = target;
            if (curTarget.IsValid is false)
            {
                curTarget = pawn.Position;
            }
            if (AbilityTier.jobDef != null)
            {
                var jobDriver = pawn.jobs.curDriver as JobDriver_CastAbility;
                if (jobDriver != null && jobDriver.Job.ability == this)
                {
                    if (AbilityTier.canQueueCastJob is false || AbilityTier.cooldownTicks > 0)
                    {
                        return;
                    }
                }
                pawn.jobs.StartJob(TMagicUtils.MakeJobAbility(this, target), lastJobEndCondition: Verse.AI.JobCondition.InterruptForced, resumeCurJobAfterwards: true);
            }
            else
            {
                Start();
            }
        }

        protected virtual string AbilityDescription()
        {
            return GetAbilityInfo(false);
        }
        public virtual string AbilityLabel()
        {
            string baseLabel = BaseAbilityLabel();
            if (HasCharge)
            {
                baseLabel += " - " + "TMF.Charges".Translate(CurCharges);
            }
            return baseLabel;
        }

        private string BaseAbilityLabel()
        {
            var baseLabel = def.abilityTiers[level].label;
            if (baseLabel.NullOrEmpty())
            {
                baseLabel = def.label;
            }
            return baseLabel.CapitalizeFirst();
        }

        public CompGlower compGlower;
        private bool dirty;
        public bool ShouldBeLitNow
        {
            get
            {
                if (!Active || !pawn.Spawned)
                {
                    return false;
                }
                return true;
            }
        }

        private IntVec3 prevPosition;
        public void RemoveGlower(Map map = null)
        {
            if (compGlower != null)
            {
                if (map != null)
                {
                    map.glowGrid?.DeRegisterGlower(compGlower);
                }
                else
                {
                    pawn.MapHeld?.glowGrid?.DeRegisterGlower(compGlower);
                }
                compGlower = null;
            }
        }
        public void UpdateGlower()
        {
            RemoveGlower();
            compGlower = new CompGlower
            {
                parent = pawn
            };
            compGlower.Initialize(AbilityTier.glowProps);
            compGlower.glowOnInt = true;
            pawn.MapHeld?.mapDrawer.MapMeshDirty(pawn.Position, MapMeshFlagDefOf.Things);
            pawn.MapHeld?.glowGrid.RegisterGlower(compGlower);
        }

        public virtual void Tick()
        {
            if (HasCharge && CurCharges < MaxCharge && RechargeDuration > 0)
            {
                chargingTicks++;
                if (chargingTicks >= RechargeDuration)
                {
                    chargingTicks = 0;
                    CurCharges++;
                }
            }
            if (activateOnceSpawned && pawn.Spawned)
            {
                activateOnceSpawned = false;
                Start();
            }

            foreach (var verb in Verbs)
            {
                verb.VerbTick();
            }

            if (AbilityTier.glowProps != null)
            {
                if (dirty || pawn.Position != prevPosition)
                {
                    if (ShouldBeLitNow)
                    {
                        UpdateGlower();
                    }
                    else
                    {
                        RemoveGlower();
                    }
                    prevPosition = pawn.Position;
                    dirty = false;
                }
            }

            if (Active)
            {
                if (AbilityTier.durationTicks != -1)
                {
                    if (Find.TickManager.TicksGame > lastActivatedTick + AbilityTier.durationTicks)
                    {
                        End();
                    }
                }

                if (AbilityTier.minEnergy != -1)
                {
                    if (abilityResource.energy < AbilityTier.minEnergy)
                    {
                        End();
                    }
                }


                if (AbilityTier.xpGainWhileActive != null)
                {
                    if (Find.TickManager.TicksGame % AbilityTier.xpGainWhileActive.ticksInterval == 0)
                    {
                        if (AbilityTier.xpGainWhileActive.xpGain > 0)
                        {
                            if (AbilityTier.xpGainWhileActive.abilityClass != null && AbilityTier.xpGainWhileActive.abilityClass != abilityClass.def)
                            {
                                var otherAbilityClass = abilityClass.compAbilities.GetAbilityClass(AbilityTier.xpGainWhileActive.abilityClass);
                                if (otherAbilityClass != null)
                                {
                                    otherAbilityClass.GainXP(AbilityTier.xpGainWhileActive.xpGain);
                                }
                            }
                            else
                            {
                                abilityClass.GainXP(AbilityTier.xpGainWhileActive.xpGain);
                            }
                        }
                        if (AbilityTier.xpGainWhileActive.masteryGain > 0)
                        {
                            GainMasteryPoints(AbilityTier.xpGainWhileActive.masteryGain);
                        }
                    }
                }

                if (soundPlaying != null)
                {
                    soundPlaying.Maintain();
                }
            }
            else
            {
                if (soundPlaying != null)
                {
                    soundPlaying.End();
                    soundPlaying = null;
                }
            }

            for (int num2 = maintainedEffecters.Count - 1; num2 >= 0; num2--)
            {
                Effecter item = maintainedEffecters[num2].Item1;
                if (item.ticksLeft > 0)
                {
                    TargetInfo item2 = maintainedEffecters[num2].Item2;
                    TargetInfo item3 = maintainedEffecters[num2].Item3;
                    item.EffectTick(item2, item3);
                    item.ticksLeft--;
                }
                else
                {
                    item.Cleanup();
                    maintainedEffecters.RemoveAt(num2);
                }
            }
        }

        public virtual void GainMasteryPoints(float xp)
        {
            if (def.HasMastery)
            {
                if (level < MaxLevel)
                {
                    masteryPoints += xp;
                    while (masteryPoints >= masteryPointsSinceLastLevel + RequiredMasteryPointsForNewLevel)
                    {
                        masteryPointsSinceLastLevel += RequiredMasteryPointsForNewLevel;
                        ChangeLevel(level + 1);
                        if (level >= MaxLevel)
                        {
                            return;
                        }
                    }
                }
            }
        }

        public virtual void Draw()
        {

        }

        public virtual bool CanUseWhileInMentalState => false;


        public virtual bool CanBeActivated(float energyCost, out string failReason, bool allowDisabling = false, 
            Func<string> canBeActivatedValidator = null)
        {
            failReason = "";
            if (DebugSettings.godMode && this.abilityClass.abilityResource != null)
            {
                this.abilityClass.abilityResource.energy = abilityClass.abilityResource.MaxEnergy;
                return true;
            }
            if (!CanUseWhileInMentalState && pawn.MentalState != null)
            {
                failReason = "TMF.NotControllable".Translate();
                return false;
            }
            if (pawn.Downed && AbilityTier.canUseWhenDowned is false)
            {
                failReason = "TMF.DownedCannotUse".Translate();
                return false;
            }

            if (!Active)
            {
                if (lastActivatedTick > 0)
                {
                    int cooldownTicksRemaining = Find.TickManager.TicksGame - lastActivatedTick;
                    if (cooldownTicksRemaining < cooldownPeriod)
                    {
                        failReason = "AbilityOnCooldown".Translate((lastActivatedTick + cooldownPeriod - Find.TickManager.TicksGame).ToStringTicksToPeriod());
                        return false;
                    }
                }

                if (HasCharge && CurCharges <= 0)
                {
                    failReason = "TMF.MissingCharges".Translate();
                    return false;
                }

                if (abilityResource != null)
                {
                    if (abilityResource.energy < energyCost)
                    {
                        failReason = "TMF.NotEnoughEnergy".Translate(abilityResource.def.label, energyCost);
                        return false;
                    }
                    if (AbilityTier.minEnergy != -1 && abilityResource.energy < AbilityTier.minEnergy)
                    {
                        failReason = "TMF.NotEnoughEnergy".Translate(AbilityTier.minEnergy, energyCost);
                        return false;
                    }
                }


                if (AbilityTier.cannotBeActiveWithOtherAbilitiesInUse != null)
                {
                    var firstActiveAbility = compAbilities.AllLearnedAbilities.FirstOrDefault(x => 
                    AbilityTier.cannotBeActiveWithOtherAbilitiesInUse.Contains(x.def) && x.Active);
                    if (firstActiveAbility != null)
                    {
                        failReason = "TMF.CannotUseThisAbilityWhileOtherIsActive".Translate(firstActiveAbility.BaseAbilityLabel());
                        return false;
                    }
                }

                if (AbilityTier.cannotBeActiveWithHediffs != null)
                {
                    var firstHediff = AbilityTier.cannotBeActiveWithHediffs.FirstOrDefault
                        (x => pawn.health.hediffSet.GetFirstHediffOfDef(x) != null);
                    if (firstHediff != null)
                    {
                        failReason = "TMF.CannotUseThisAbilityWhileOtherIsActive".Translate(firstHediff.LabelCap);
                        return false;
                    }
                }

                if (AbilityTier.requiresActiveAbilities != null)
                {
                    foreach (var abilityDef in AbilityTier.requiresActiveAbilities)
                    {
                        var ability = pawn.GetAbility(abilityDef);
                        if (ability is null)
                        {
                            failReason = "TMF.RequiresOtherActiveAbility".Translate(abilityDef.LabelCap);
                            return false;
                        }
                        else if (ability.Active is false)
                        {
                            failReason = "TMF.RequiresOtherActiveAbility".Translate(ability.BaseAbilityLabel());
                            return false;
                        }
                    }
                }

                if (AbilityTier.requiresActiveAbilitiesOneOf != null)
                {
                    bool hasActiveAbility = false;
                    foreach (var abilityDef in AbilityTier.requiresActiveAbilitiesOneOf)
                    {
                        var ability = pawn.GetAbility(abilityDef);
                        if (ability != null)
                        {
                            if (ability.Active)
                            {
                                hasActiveAbility = true;
                                break;
                            }
                        }
                    }
                    if (!hasActiveAbility)
                    {
                        failReason = "TMF.RequiresOneOfActiveAbility".Translate(string.Join(", ", AbilityTier.requiresActiveAbilitiesOneOf.Select(x => x.LabelCap)));
                        return false;
                    }

                }

                if (canBeActivatedValidator != null)
                {
                    failReason = canBeActivatedValidator();
                    if (!failReason.NullOrEmpty())
                    {
                        return false;
                    }
                }
            }
            else
            {
                if (!allowDisabling)
                {
                    failReason = "TMF.AlreadyUsingThisAbility".Translate();
                    return false;
                }
            }

            failReason = "";
            return true;
        }

        public IEnumerable<Thing> GetHostileTargets()
        {
            return pawn.Map.attackTargetsCache.GetPotentialTargetsFor(pawn)
                    .Select(x => x.Thing).Where(x => x is not Pawn pawn || pawn.Dead is false && pawn.Downed is false).InRandomOrder();
        }

        public virtual void Notify_CasterDowned()
        {

        }

        public virtual void Notify_CasterKilled()
        {

        }

        public string GetAbilityInfo(bool includeNextLevelInfo = true)
        {
            var sb = new StringBuilder();
            sb.AppendLine(BaseAbilityLabel() + "\n");
            if (def.abilityTiers.Count > 1)
            {
                sb.AppendLine("TMF.Level".Translate(LevelHumanReadable));
            }
            bool fullyLearned = abilityClass.FullyLearned(def);
            string info = AbilityTier.GetTierInfo(pawn, abilityClass.def, this, def, true, false);
            if (!info.NullOrEmpty())
            {
                sb.AppendLine(info);
            }
            if (!fullyLearned && includeNextLevelInfo)
            {
                var nextAbilityTier = def.abilityTiers[level + 1];
                info = nextAbilityTier.GetTierInfo(pawn, abilityClass.def, this, def, false, abilityClass.UsesSkillPointSystem);
                if (!info.NullOrEmpty())
                {
                    sb.AppendLine("\n" + "TMF.NextLevel".Translate());
                    if (!AbilityTier.nextTierDescription.NullOrEmpty())
                    {
                        sb.AppendLine(AbilityTier.nextTierDescription);
                    }
                    sb.AppendLine(info);
                }
            }
            return sb.ToString().TrimEndNewlines();
        }
        public string GetUniqueLoadID()
        {
            return $"KIAbility_{def.defName}_{abilityID}";
        }

        public override string ToString()
        {
            return def.defName + GetHashCode();
        }
    }
}
