using System.Collections.Generic;
using Verse;

namespace TaranMagicFramework
{
    public class AnimationDef : ThingDef
    {
        public new GraphicDataAnimation graphicData;

        public int maxLoopCount;

        public int fpsRate = 1;

        public bool lockToRealTime;

        public float angleOffset;

        public int additionalLifetimeTicks;

        public AnimationDef spawnOverlayOnEnd;

        public List<Rot4> attachedRotationDraw;
        public override void PostLoad()
        {
            base.PostLoad();
            LongEventHandler.ExecuteWhenFinished(delegate
            {
                graphicData.InitMainTextures();
            });
        }
    }
}
