using Verse;

namespace TaranMagicFramework
{
    public class HediffAbility : HediffWithComps
    {
        public Ability ability;
        public override void PostRemoved()
        {
            base.PostRemoved();
            if (ability != null && ability.Active)
            {
                ability.End();
            }
        }
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_References.Look(ref ability, "ability");
        }
    }
}
