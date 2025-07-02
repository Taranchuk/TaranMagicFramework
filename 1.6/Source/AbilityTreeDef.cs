using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace TaranMagicFramework
{
    public class AbilityTreeDef : Def
    {
        public bool unlockedByDefault = true;
        public string backgroundTexPath;
        public Texture2D backgroundTexture;
        public string iconTexPath;
        public Texture2D icon;
        public bool visible;
        public AbilityTreeDef parentTree;

        private List<AbilityDef> allAbilities;
        public List<StatBonus> statBonusesPerLevel;
        public List<AbilityDef> AllAbilities => allAbilities ??= DefDatabase<AbilityDef>.AllDefs.Where(x => x.abilityTrees.Contains(this)).ToList();
        
        public virtual IEnumerable<Gizmo> GetGizmos(AbilityClass abilityClass)
        {
            yield break;
        }
        public override void PostLoad()
        {
            base.PostLoad();
            LongEventHandler.ExecuteWhenFinished(delegate
            {
                if (backgroundTexPath.NullOrEmpty())
                {
                    backgroundTexture = BaseContent.BadTex;
                }
                else
                {
                    backgroundTexture = ContentFinder<Texture2D>.Get(backgroundTexPath);
                }

                if (iconTexPath.NullOrEmpty())
                {
                    icon = BaseContent.BadTex;
                }
                else
                {
                    icon = ContentFinder<Texture2D>.Get(iconTexPath);
                }
            });
        }
    }
}
