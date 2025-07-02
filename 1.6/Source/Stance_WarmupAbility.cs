using UnityEngine;
using Verse;

namespace TaranMagicFramework
{
    [HotSwappable]
    public class Stance_WarmupAbility : Stance_Warmup
    {
        public Mote_Animation animation;
        public Stance_WarmupAbility(VerbPropertiesAbility verbProps, Pawn caster, int ticks, LocalTargetInfo focusTarg, Verb verb)
            : base(ticks, focusTarg, verb)
        {
            animation = ThingMaker.MakeThing(verbProps.warmupAnimation) as Mote_Animation;
            animation.exactPosition = caster.DrawPos;
            caster.rotationTracker.Face(focusTarg.CenterVector3);
            animation.Scale = 1f;
            animation.Attach(caster, verbProps.warmupAnimationOffset, rotateWithTarget: true);
            GenSpawn.Spawn(animation, caster.Position, caster.Map);
            animation.TimeInterval(0f);
            ticksLeft = animation.Graphic_Animation.subGraphics.Length * animation.AnimationDef.fpsRate;
        }

        public static bool expire;
        public override void StanceTick()
        {
            base.StanceTick();
            if (animation.Destroyed)
            {
                expire = true;
                Expire();
                expire = false;
            }
        }
        public override void Expire()
        {
            if (expire)
            {
                base.Expire();
            }
        }
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_References.Look(ref animation, "animation");
        }
    }
}
