using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace TaranMagicFramework
{
    public class AbilityClass : IExposable, ILoadReferenceable
    {
        public AbilityClassDef def;
        public AbilityResource abilityResource;
        public CompAbilities compAbilities;
        private bool unlocked = true;
        public bool Unlocked
        {
            get
            {
                return unlocked;
            }
            set
            {
                unlocked = value;
                if (value)
                {
                    TMagicUtils.Message("Unlocking ability class " + def, pawn);
                }
                else
                {
                    TMagicUtils.Message("Locking ability class " + def, pawn);
                }
                compAbilities.RecheckAbilities();
            }
        }

        private List<AbilityTreeDef> unlockedTrees = new List<AbilityTreeDef>();
        public List<AbilityTreeDef> UnlockedTrees
        {
            get
            {
                return unlockedTrees;
            }
        }
        public bool TreeUnlocked(AbilityTreeDef abilityTreeDef)
        {
            return UnlockedTrees.Contains(abilityTreeDef);
        }
        public void UnlockTree(AbilityTreeDef abilityTreeDef)
        {
            lock (unlockedTrees)
            {
                if (unlockedTrees.Contains(abilityTreeDef))
                {
                    TMagicUtils.Message($"UnlockTree({abilityTreeDef.defName}): already present in unlockedTrees. unlockedTrees hash {unlockedTrees.GetHashCode()}. | AbilityClass def: {this.def} AbilityClass hash: {this.GetHashCode()} | Tree def: {abilityTreeDef} Tree hash:: {abilityTreeDef.GetHashCode()}", pawn);
                    return;
                }
                unlockedTrees.Add(abilityTreeDef);
                TMagicUtils.Message($"Unlocking tree {abilityTreeDef.defName}. unlockedTrees now: [{string.Join(", ", unlockedTrees.Select(t => t.defName))}] unlockedTrees hash {unlockedTrees.GetHashCode()} | AbilityClass def: {this.def} AbilityClass hash: {this.GetHashCode()} | Tree def: {abilityTreeDef} Tree hash:: {abilityTreeDef.GetHashCode()}", pawn);
                compAbilities.RecheckAbilities();
            }
        }

        public void LockTree(AbilityTreeDef abilityTreeDef)
        {
            lock (unlockedTrees)
            {
                unlockedTrees.Remove(abilityTreeDef);
                TMagicUtils.Message($"Locking tree {abilityTreeDef.defName}. unlockedTrees now: [{string.Join(", ", unlockedTrees.Select(t => t.defName))}]", pawn);
                compAbilities.RecheckAbilities();
            }
        }

        public int id;
        public Pawn pawn;
        public int MaxLevel => def.maxLevel;
        public int skillPoints;
        public int curSpentSkillPoints;
        public int level;
        public float xpPoints;
        public float xpSinceLastLevel;
        public float GainedXPSinceLastLevel => xpPoints - xpSinceLastLevel;
        public virtual float RequiredXPForNewLevel => (level + 1) * 50f;
        public bool UsesSkillPointSystem => def.usesSkillPointSystem;

        public Dictionary<AbilityDef, Ability> learnedAbilities = new();
        public Dictionary<AbilityDef, Ability> removedAbilities = new();
        public IEnumerable<Ability> LearnedAbilities
        {
            get
            {
                learnedAbilities ??= new Dictionary<AbilityDef, Ability>();
                return learnedAbilities.Values.ToList();
            }
        }

        public virtual void Tick()
        {
            foreach (var ability in LearnedAbilities)
            {
                ability.Tick();
            }
        }

        public Ability GetLearnedAbility(AbilityDef abilityDef)
        {
            if (abilityDef == null)
            {
                return null;
            }
            if (!learnedAbilities.TryGetValue(abilityDef, out var ability))
            {
                return null;
            }
            return ability;
        }

        public virtual Ability LearnAbility(AbilityDef abilityDef, bool spendSkillPoints, int level = 0)
        {
            if (learnedAbilities.TryGetValue(abilityDef, out var ability))
            {
                if (ability.level == level)
                {
                    return ability;
                }
                ability.ChangeLevel(level);
            }
            else
            {
                if (!removedAbilities.TryGetValue(abilityDef, out ability))
                {
                    ability = CreateAbility(abilityDef);
                }
                else
                {
                    removedAbilities.Remove(abilityDef);
                }
                learnedAbilities[abilityDef] = ability;
                OnLearned(ability);
                TMagicUtils.Message("Learning " + ability, pawn);
                ability.ChangeLevel(level);
                if (ability.AbilityTier.activateOnGain)
                {
                    ability.Start();
                }
            }
            if (spendSkillPoints)
            {
                if (abilityDef.abilityTiers[level].acquireRequirement != null)
                {
                    int skillPointsToBuy = abilityDef.abilityTiers[level].acquireRequirement.skillPointsToUnlock;
                    skillPoints -= skillPointsToBuy;
                    curSpentSkillPoints += skillPointsToBuy;
                }
            }
            return ability;
        }
        public Ability CreateAbility(AbilityDef abilityDef)
        {
            var ability = Activator.CreateInstance(abilityDef.abilityClass) as Ability;
            ability.pawn = pawn;
            ability.def = abilityDef;
            ability.abilityID = GameComponent_MagicFramework.Instance.GetNextAbilityID();
            ability.autocastEnabled = ability.Verbs.FirstOrDefault()?.VerbPropsAbility()?.autocast ?? false;
            ability.learnedFirstTimeTick = Find.TickManager.TicksGame;
            ability.abilityClass = this;
            ability.abilityResource = abilityResource;
            return ability;
        }

        protected virtual void OnLearned(Ability ability)
        {
            if (PawnUtility.ShouldSendNotificationAbout(pawn) && ability.def.letterTitleKeyGained.NullOrEmpty() is false)
            {
                Find.LetterStack.ReceiveLetter(ability.def.letterTitleKeyGained.Translate(pawn.Named("PAWN")),
                    ability.def.letterDescKeysGained.Translate(pawn.Named("PAWN")), LetterDefOf.PositiveEvent, pawn);
            }
            ability.OnLearned();
        }

        public void RemoveIncompatibleAbilities()
        {
            var allAbilities = pawn.AllAvailableAbilities();
            if (allAbilities != null && learnedAbilities != null)
            {
                var abilitiesToRemove = learnedAbilities.Where(x => !allAbilities.Contains(x.Value.def)).ToList();
                foreach (var ability in abilitiesToRemove)
                {
                    RemoveAbility(ability.Value);
                }
            }
        }

        public void RemoveAbility(Ability ability)
        {
            removedAbilities[ability.def] = ability;
            learnedAbilities.Remove(ability.def);
            ability.OnRemoved();
        }

        public bool Learned(AbilityDef abilityDef)
        {
            return learnedAbilities.ContainsKey(abilityDef);
        }

        public bool FullyLearned(AbilityDef abilityDef)
        {
            if (learnedAbilities.TryGetValue(abilityDef, out var learnedAbility))
            {
                bool result = abilityDef.abilityTiers.Count - 1 == learnedAbility.level;
                return result;
            }
            return false;
        }
        public AbilityTierDef GetAbilityTier(AbilityDef abilityDef, int level)
        {
            return abilityDef.abilityTiers[level];
        }
        public AbilityTierDef GetAbilityTier(AbilityDef abilityDef)
        {
            return abilityDef.abilityTiers[learnedAbilities[abilityDef].level];
        }

        public void UnlockNextTier(AbilityDef abilityDef, bool spendSkillPoints)
        {
            var ability = GetLearnedAbility(abilityDef);
            var level = ability != null ? ability.level + 1 : 0;
            LearnAbility(abilityDef, spendSkillPoints, level);
        }
        public bool CanUnlockNextTier(AbilityDef abilityDef, out AbilityTierDef nextAbilityTier, out bool fullyUnlocked)
        {
            fullyUnlocked = false;
            nextAbilityTier = null;
            if (FullyLearned(abilityDef))
            {
                fullyUnlocked = true;
                return false;
            }
            var ability = GetLearnedAbility(abilityDef);
            nextAbilityTier = ability != null ? abilityDef.abilityTiers[ability.level + 1] : abilityDef.abilityTiers[0];
            if (!nextAbilityTier.isLearnable && !nextAbilityTier.autoGain)
            {
                return false;
            }
            if (nextAbilityTier.acquireRequirement != null 
                && nextAbilityTier.acquireRequirement.RequirementSatisfied(this, ability) is false)
            {
                return false;
            }
            return true;
        }

        public virtual void GainXP(float xp)
        {
            if (def.usesXPSystem)
            {
                if (level < MaxLevel)
                {
                    xpPoints += GetXPGainValue(xp);
                    while (xpPoints >= xpSinceLastLevel + RequiredXPForNewLevel)
                    {
                        xpSinceLastLevel += RequiredXPForNewLevel;
                        SetLevel(level + 1);
                        if (def.usesSkillPointSystem)
                        {
                            skillPoints++;
                        }
                        if (level >= MaxLevel)
                        {
                            return;
                        }
                    }
                }
            }
        }

        public void SetLevel(int newLevel)
        {
            var oldLevel = level;
            level = newLevel;
            if (level > oldLevel)
            {
                OnLevelGained();
            }
            var minXP = 0f;
            for (var i = 0; i < level + 1; i++)
            {
                minXP += i * 50f;
            }
            if (xpPoints < minXP)
            {
                xpPoints = minXP;
                xpSinceLastLevel = xpPoints;
            }

            foreach (StatDef item in DefDatabase<StatDef>.AllDefsListForReading)
            {
                item.Worker.TryClearCache();
            }
        }

        protected virtual float GetXPGainValue(float xp)
        {
            if (def.xpGainMultiplierStat != null)
            {
                return xp * pawn.GetStatValue(def.xpGainMultiplierStat);
            }
            return xp;
        }

        protected virtual void OnLevelGained()
        {
            compAbilities.RecheckAbilities();
            if (PawnUtility.ShouldSendNotificationAbout(pawn))
            {
                Messages.Message("TMF.PawnLevelUp".Translate(pawn.Named("PAWN"), def.label), pawn, MessageTypeDefOf.PositiveEvent);
            }
        }

        public void Init(CompAbilities compAbilities, AbilityClassDef def, Pawn pawn, AbilityResource abilityResource, 
            bool unlockClass = false)
        {
            this.compAbilities = compAbilities;
            this.def = def;
            this.pawn = pawn;
            this.abilityResource = abilityResource;
            if (unlockClass || def.unlockedByDefault)
            {
                if (!Unlocked)
                {
                    Unlocked = true;
                }
            }
            foreach (var abilityTree in def.abilityTrees)
            {
                if (TreeUnlocked(abilityTree) is false && abilityTree.unlockedByDefault)
                {
                    UnlockTree(abilityTree);
                }
            }

            if (this.pawn is null && compAbilities != null)
            {
                this.pawn = compAbilities.Pawn;
            }
            foreach (var ability in learnedAbilities.Values.ToList())
            {
                ability.pawn = pawn;
                if (ability.GetType() != ability.def.abilityClass)
                {
                    learnedAbilities.Remove(ability.def);
                    var newAbility = LearnAbility(ability.def, false, ability.level);
                    newAbility.Init();
                }
                else
                {
                    ability.Init();
                }
            }
        }

        public DamageWorker.DamageResult DoDamage(Thing thing, DamageInfo dinfo)
        {
            dinfo.SetAmount(AdjustDamage(dinfo.Amount, thing));
            return thing.TakeDamage(dinfo);
        }

        public float AdjustDamage(float damageAmount, Thing thing)
        {
            if (def.damageMultiplierStat != null)
            {
                damageAmount *= TMagicUtils.GetStatValueForStat(compAbilities, def.damageMultiplierStat);
            }
            if (thing != null)
            {
                if (def.damageResistanceMultiplierStat != null)
                {
                    float resistanceMult = 1f - thing.GetStatValue(def.damageResistanceMultiplierStat);
                    damageAmount *= resistanceMult;
                }
            }
            return damageAmount;
        }
        public void ExposeData()
        {
            Scribe_Defs.Look(ref def, "def");
            Scribe_References.Look(ref pawn, "pawn");
            Scribe_References.Look(ref abilityResource, "abilityResource");
            Scribe_Collections.Look(ref learnedAbilities, "learnedAbilities", LookMode.Def, LookMode.Deep);
            Scribe_Collections.Look(ref removedAbilities, "removedAbilities", LookMode.Def, LookMode.Deep);
            Scribe_Values.Look(ref id, "id");
            Scribe_Values.Look(ref skillPoints, "skillPoints");
            Scribe_Values.Look(ref level, "level");
            Scribe_Values.Look(ref xpPoints, "xpPoints");
            Scribe_Values.Look(ref xpSinceLastLevel, "xpSinceLastLevel");
            Scribe_Values.Look(ref curSpentSkillPoints, "curSpentSkillPoints");
            Scribe_Values.Look(ref unlocked, "unlocked", true);
            Scribe_Collections.Look(ref unlockedTrees, "unlockedTrees", LookMode.Def);
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                unlockedTrees ??= new List<AbilityTreeDef>();
                learnedAbilities ??= new Dictionary<AbilityDef, Ability>();
                removedAbilities ??= new Dictionary<AbilityDef, Ability>();
            }
        }

        public string GetUniqueLoadID()
        {
            return "TMF_AbilityClass" + id;
        }

        public virtual IEnumerable<Gizmo> AbilityGizmos()
        {
            foreach (var tree in unlockedTrees)
            {
                foreach (var gizmo in tree.GetGizmos(this))
                {
                    yield return gizmo;
                }
            }
            var settings = pawn.GetUISettingsData();
            foreach (var ability in LearnedAbilities.OrderBy(x => x.def.index))
            {
                if (ability.ShouldShowGizmos)
                {
                    if (settings.abilityStates.TryGetValue(ability.def, out var state) && state is false)
                    {
                        continue;
                    }
                    foreach (var gizmo in ability.GetGizmos())
                    {
                        yield return gizmo;
                    }
                }
            }
        }
    }
}
