using RimWorld;
using System.Linq;
using UnityEngine;
using Verse;

namespace TaranMagicFramework
{
    public abstract class ITabAbility : ITab
    {
        public AbilityTabDef tabDef;
        public float scrollHeight;
        public float scrollWidth;
        public Vector2 scrollPosition;
        public Vector2 scrollPosition2;
        public override bool IsVisible => tabDef.abilityClasses.Any(x => PawnToShowInfoAbout.GetAvailableAbilityClasses().Contains(x));
        public ITabAbility(AbilityTabDef tabDef)
        {
            this.tabDef = tabDef;
            size = tabDef.size;
            labelKey = tabDef.labelKey;
        }
        protected Pawn PawnToShowInfoAbout
        {
            get
            {
                Pawn pawn = SelPawn;
                if (pawn is null)
                {
                    var corpse = SelThing as Corpse;
                    if (corpse != null)
                    {
                        pawn = corpse.InnerPawn;
                    }
                }
                return pawn;
            }
        }
    }
}
