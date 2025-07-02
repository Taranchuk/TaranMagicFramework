using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace TaranMagicFramework
{
    public class AbilityDef : Def
    {
        public List<AbilityTreeDef> abilityTrees = new List<AbilityTreeDef>();
        public AbilityCategoryDef abilityRank;
        public bool hidden;
        public bool hiddenUntilUnlocked;
        public Type abilityClass = typeof(Ability);
        public List<AbilityTierDef> abilityTiers;
        public string uiSkillIcon;
        public Vector2 uiPosition;
        public List<UnlockedWhenMastered> unlockedWhenMasteredList;
        public List<AbilityDef> unlockedWhenMastered;
        public List<AbilityDef> visibleWhenActive;
        public List<AbilityDef> endAbilitiesWhenActive;
        public List<AbilityDef> endAbilitiesWhenEnded;
        public Texture2D icon;
        public string letterTitleKeyGained;
        public string letterDescKeysGained;
        public bool showMastered = true;
        public AbilityModGroupDef abilityModGroup;
        public bool showWhenDrafted = true;
        public bool showWhenNotDrafted = true;
        public bool HasMastery => abilityTiers.Any(x => x.acquireRequirement?.masteryPointsToUnlock > 0);
        public override void PostLoad()
        {
            base.PostLoad();
            if (!uiSkillIcon.NullOrEmpty())
            {
                LongEventHandler.ExecuteWhenFinished(() => icon = ContentFinder<Texture2D>.Get(uiSkillIcon));
            }
        }
    }
}
