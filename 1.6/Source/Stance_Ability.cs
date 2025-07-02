using Verse;

namespace TaranMagicFramework
{
    public class Stance_Ability : Stance_Busy
    {
        public Ability ability;
        public Stance_Ability()
        {

        }

        public Stance_Ability(Ability ability, int ticks, LocalTargetInfo focusTarg, Verb verb)
            : base(ticks, focusTarg, verb)
        {
            this.ability = ability;
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_References.Look(ref ability, "ability");
        }
        public override void StanceTick()
        {
            base.StanceTick();
            if (ability.Active is false)
            {
                Expire();
            }
        }
    }
}
