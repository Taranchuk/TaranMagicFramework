using System;
using Verse;

namespace TaranMagicFramework
{
    public class Stance_Wait : Stance_Busy
    {
        public Action actionToPerform;
        public Stance_Wait()
        {
        }

        public Stance_Wait(int ticks, LocalTargetInfo focusTarg, Verb verb, Action actionToPerform)
            : base(ticks, focusTarg, verb)
        {
            this.actionToPerform = actionToPerform;
        }

        public override void Expire()
        {
            base.Expire();
            actionToPerform?.Invoke();
        }
    }
}
