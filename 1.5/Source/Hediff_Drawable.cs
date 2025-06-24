using UnityEngine;
using Verse;

namespace TaranMagicFramework
{
    public class HediffCompProperties_Drawable : HediffCompProperties
    {
        public GraphicData graphicData;
        public OverlayProps overlayProps;
        public HediffCompProperties_Drawable()
        {
            compClass = typeof(HediffComp_Drawable);
        }
    }
    public class HediffComp_Drawable : HediffComp
    {
        public HediffCompProperties_Drawable Props => props as HediffCompProperties_Drawable;
        public Mote_Animation moteAnimation;
        public override void CompPostPostAdd(DamageInfo? dinfo)
        {
            base.CompPostPostAdd(dinfo);
            if (Props.overlayProps != null)
            {
                var pawn = Pawn;
                moteAnimation = ThingMaker.MakeThing(Props.overlayProps.overlay) as Mote_Animation;
                moteAnimation.exactPosition = pawn.PositionHeld.ToVector3Shifted();
                moteAnimation.Scale = Props.overlayProps.scale;
                moteAnimation.sourceHediff = parent;
                moteAnimation.Attach(pawn, Vector3.zero);
                GenSpawn.Spawn(moteAnimation, pawn.PositionHeld, pawn.MapHeld);
            }
        }

        public override void CompPostPostRemoved()
        {
            base.CompPostPostRemoved();
            if (moteAnimation.DestroyedOrNull() is false)
            {
                moteAnimation.Destroy();
            }
        }

        public override void CompExposeData()
        {
            base.CompExposeData();
            Scribe_References.Look(ref moteAnimation, "moteAnimation");
        }

        private Graphic _graphic;
        public Graphic Graphic => _graphic ??= Props.graphicData.Graphic;
        public void Draw()
        {
            if (Props.graphicData != null)
            {
                Graphic.Draw(Pawn.DrawPos, Pawn.Rotation, Pawn);
            }
        }
    }
    public class Hediff_Drawable : HediffAbility
    {
        private HediffComp_Drawable compDrawable;
        public HediffComp_Drawable CompDrawable => compDrawable ??= this.TryGetComp<HediffComp_Drawable>();
        public override void PostAdd(DamageInfo? dinfo)
        {
            base.PostAdd(dinfo);
            if (CompDrawable.Props.graphicData != null)
            {
                GameComponent_MagicFramework.Instance.hediffsToDraw.Add(this);
            }
        }

        public virtual void Draw()
        {
            CompDrawable.Draw();
        }
    }
}
