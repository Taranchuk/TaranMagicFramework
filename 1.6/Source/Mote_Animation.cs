using UnityEngine;
using Verse;

namespace TaranMagicFramework
{
    public class Mote_Animation : MoteAttached, IAnimated
    {
        public int curInd;
        protected int curLoopInd;
        protected int prevTick;
        public float angle;
        public float Angle
        {
            get
            {
                if (AnimationDef.angleOffset == 0)
                {
                    return angle;
                }
                return AnimationDef.angleOffset;
            }
        }
        public int CurInd
        {
            get
            {
                return curInd;
            }
            set
            {
                curInd = value;
            }
        }
        public int CurLoopInd
        {
            get
            {
                return curLoopInd;
            }
            set 
            { 
                curLoopInd = value;
            }
        }


        public int PrevTick
        {
            get
            {
                return prevTick;
            }
            set
            {
                prevTick = value;
            }
        }

        public virtual Vector2? SizeOverride => sizeOverride;
        public Vector2? sizeOverride;

        protected bool destroy;
        public bool endLoop;
        public int expireInTick;
        public int activatedTick;
        public override bool EndOfLife => false;
        public Ability sourceAbility;
        public Hediff sourceHediff;
        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            if (!respawningAfterLoad)
            {
                activatedTick = Find.TickManager.TicksGame;
                TMagicUtils.Message("Creating animation: " + this, sourceAbility?.pawn);
            }
        }

        public virtual void OnCycle_Completion()
        {
            if (!destroy)
            {
                if (CurLoopInd >= AnimationDef.maxLoopCount && (endLoop || AnimationDef.maxLoopCount > 0))
                {
                    destroy = true;
                    if (AnimationDef.additionalLifetimeTicks > 0)
                    {
                        expireInTick = AnimationDef.additionalLifetimeTicks;
                        activatedTick = Find.TickManager.TicksGame;
                    }
                }
            }
        }

        public override void Destroy(DestroyMode mode = DestroyMode.Vanish)
        {
            DoAction();
            TMagicUtils.Message("Destroying animation: " + this, sourceAbility?.pawn);
            base.Destroy(mode);
            if (sourceAbility != null)
            {
                sourceAbility.animations.Remove(this);
                sourceAbility.pawn?.ResolveAllGraphicsSafely();
            }
        }

        public virtual void DoAction()
        {
            if (AnimationDef.spawnOverlayOnEnd != null)
            {
                var thing = GenSpawn.Spawn(AnimationDef.spawnOverlayOnEnd, Position, Map) as MoteAttached;
                if (thing != null)
                {
                    thing.Attach(link1.Target);
                }
            }
        }

        public override void DrawAt(Vector3 drawLoc, bool flip = false)
        {
            DrawCustom(AnimationDef.altitudeLayer.AltitudeFor());
        }

        private new Graphic graphicInt;

        public AnimationDef AnimationDef => def as AnimationDef;
        public new Graphic DefaultGraphic
        {
            get
            {
                if (graphicInt == null)
                {
                    if (AnimationDef.graphicData == null)
                    {
                        return BaseContent.BadGraphic;
                    }
                    graphicInt = AnimationDef.graphicData.GraphicColoredFor(this);
                }
                return graphicInt;
            }
        }
        public override Graphic Graphic => DefaultGraphic;
        public Graphic_Animation Graphic_Animation => Graphic as Graphic_Animation;
        public virtual void DrawCustom(float altitude)
        {
            exactPosition.y = altitude;
            if (AnimationDef.attachedRotationDraw != null)
            {
                var rot = link1.Target.Thing.Rotation;
                if (AnimationDef.attachedRotationDraw.Contains(rot) is false)
                {
                    return;
                }
            }
            var loc = DrawPos + AnimationDef.graphicData.drawOffset;
            if (Graphic is Graphic_Animation graphicAnimation)
            {
                graphicAnimation.animated = this;
            }
            if (link1.Target.IsValid)
            {
                Graphic.DrawWorker(loc, link1.Target.Thing.Rotation, def, this, 0);
            }
            else
            {
                Graphic.DrawWorker(loc, Rotation, def, this, 0);
            }
        }

        public override void Tick()
        {
            base.Tick();
            if (!Destroyed)
            {
                if (link1.Target.Thing is not null && link1.Target.Map is null)
                {
                    Destroy();
                }
                else if (destroy && expireInTick <= 0)
                {
                    Destroy();
                }
                else if (expireInTick > 0 && Find.TickManager.TicksGame >= activatedTick + expireInTick)
                {
                    Destroy();
                }
                else if 
                    (expireInTick <= 0 && (AnimationDef.maxLoopCount <= 0 && (sourceAbility is null 
                    || sourceAbility.pawn.DestroyedOrNull() 
                    || sourceAbility.pawn.Dead 
                    || sourceAbility.animations.Contains(this) is false)))
                {
                    if (sourceHediff != null && sourceHediff.pawn.health.hediffSet.hediffs.Contains(sourceHediff))
                    {
                        return;
                    }
                    Destroy();
                }
            }
        }

        private TargetInfo target;
        private Vector3 offset;
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref curInd, "curInd");
            Scribe_Values.Look(ref curLoopInd, "curLoopInd");
            Scribe_Values.Look(ref sizeOverride, "sizeOverride");
            Scribe_Values.Look(ref linearScale, "linearScale");
            Scribe_Values.Look(ref curvedScale, "curvedScale");
            Scribe_Values.Look(ref activatedTick, "activatedTick");
            Scribe_Values.Look(ref expireInTick, "expireInTick");
            Scribe_References.Look(ref sourceAbility, "ability");
            Scribe_References.Look(ref sourceHediff, "sourceHediff");
            Scribe_Values.Look(ref angle, "angle");
            Scribe_Values.Look(ref prevTick, "prevTick");

            Scribe_Values.Look(ref exactPosition, "exactPosition");
            Scribe_Values.Look(ref exactRotation, "exactRotation");
            Scribe_Values.Look(ref rotationRate, "rotationRate");
            Scribe_Values.Look(ref yOffset, "yOffset");
            Scribe_Values.Look(ref instanceColor, "instanceColor", Color.white);
            Scribe_Values.Look(ref lastMaintainTick, "lastMaintainTick");
            Scribe_Values.Look(ref currentAnimationTick, "currentAnimationTick");
            Scribe_Values.Look(ref solidTimeOverride, "solidTimeOverride", -1f);
            Scribe_Values.Look(ref pausedTicks, "pausedTicks");
            Scribe_Values.Look(ref paused, "paused");
            Scribe_Values.Look(ref spawnTick, "spawnTick");
            Scribe_Values.Look(ref animationPaused, "animationPaused");
            Scribe_Values.Look(ref detachAfterTicks, "detachAfterTicks", -1);
            Scribe_Values.Look(ref spawnRealTime, "spawnRealTime");
            if (link1.Target.HasThing)
            {
                target = link1.Target;
                offset = link1.offsetInt;
            }
            Scribe_Values.Look(ref offset, "offset");
            Scribe_TargetInfo.Look(ref target, "target");
            if (target.IsValid)
            {
                link1.UpdateTarget(target, offset);
            }
        }
    }
}
