using UnityEngine;
using Verse;

namespace TaranMagicFramework
{
    public class Bullet_Animated : Bullet_Ability, IAnimated
    {
        public int curInd;
        protected int curLoopInd;
        protected int prevTick;
        public float angle;
        public float Angle => angle;
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

        public Vector3 ExactScale => Vector3.one;
        public Vector2? SizeOverride => sizeOverride;
        public Vector2? sizeOverride;
        protected bool destroy;
        public bool endLoop;
        public AnimationDef AnimationDef => def as AnimationDef;
        public void OnCycle_Completion()
        {
            CurLoopInd++;
            if (CurLoopInd >= AnimationDef.maxLoopCount && (endLoop || AnimationDef.maxLoopCount > 0))
            {
                destroy = true;
            }
        }
        private new Graphic graphicInt;
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

        public override void DrawAt(Vector3 drawLoc, bool flip = false)
        {
            float num = ArcHeightFactor * GenMath.InverseParabola(DistanceCoveredFraction);
            Vector3 drawPos = DrawPos;
            Vector3 position = drawPos + new Vector3(0f, 0f, 1f) * num;
            if (def.projectile.shadowSize > 0f)
            {
                DrawShadow(drawPos, num);
            }
            var sizeOverride = SizeOverride;
            var s = sizeOverride.HasValue ? new Vector3(sizeOverride.Value.x, 1f, sizeOverride.Value.y)
                : new Vector3(AnimationDef.graphicData.drawSize.x, 1f, AnimationDef.graphicData.drawSize.y);
            var exactScale = ExactScale;
            s.x *= exactScale.x;
            s.z *= exactScale.y;
            var matrix = default(Matrix4x4);
            var quat = ExactRotation;
            matrix.SetTRS(position, quat, s);

            int ind = CurInd <= Graphic_Animation.subGraphics.Length - 1 ? CurInd : Graphic_Animation.subGraphics.Length - 1;
            Graphics.DrawMesh(MeshPool.GridPlane(AnimationDef.graphicData.drawSize), matrix, Graphic_Animation.subGraphics[ind].MatSingle, 0);
            if (PrevTick != Find.TickManager.TicksGame && ((AnimationDef.lockToRealTime
                && Time.frameCount % AnimationDef.fpsRate == 0)
                || (!AnimationDef.lockToRealTime && Find.TickManager.TicksGame % AnimationDef.fpsRate == 0)))
            {
                CurInd++;
                if (CurInd >= Graphic_Animation.subGraphics.Length - 1)
                {
                    OnCycle_Completion();
                    CurInd = 0;
                }
            }
            PrevTick = Find.TickManager.TicksGame;
            Comps_PostDraw();
        }
    }
}
