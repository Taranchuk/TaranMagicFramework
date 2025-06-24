using System.Collections.Generic;
using Verse;

namespace TaranMagicFramework
{
    public class GameComponent_MagicFramework : GameComponent
    {
        public List<Hediff_Drawable> hediffsToDraw = new();
        public int lastAbilityID;
        public int lastAbilityClassID;
        public int lastAbilityResourceID;
        public static GameComponent_MagicFramework Instance;
        public GameComponent_MagicFramework()
        {
            Instance = this;
        }

        public override void GameComponentUpdate()
        {
            base.GameComponentUpdate();
            for (var i = hediffsToDraw.Count - 1; i >= 0; i--)
            {
                var hediff = hediffsToDraw[i];
                if (hediff.pawn is null || hediff.pawn.health.hediffSet.hediffs.Contains(hediff) is false)
                    hediffsToDraw.RemoveAt(i);
                else if (hediff.pawn?.MapHeld != null)
                    hediff.Draw();
            }
        }
        public GameComponent_MagicFramework(Game game)
        {
            Instance = this;
        }
        public int GetNextAbilityResourceID()
        {
            lastAbilityResourceID++;
            return lastAbilityResourceID;
        }
        public int GetNextAbilityClassID()
        {
            lastAbilityClassID++;
            return lastAbilityClassID;
        }
        public int GetNextAbilityID()
        {
            lastAbilityID++;
            return lastAbilityID;
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref lastAbilityID, "lastAbilityID");
            Scribe_Values.Look(ref lastAbilityClassID, "lastAbilityClassID");
            Scribe_Values.Look(ref lastAbilityResourceID, "lastAbilityResourceID");
            Scribe_Collections.Look(ref hediffsToDraw, "hediffsToDraw", LookMode.Reference);
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                hediffsToDraw ??= new List<Hediff_Drawable>();
            }
        }
    }
}
