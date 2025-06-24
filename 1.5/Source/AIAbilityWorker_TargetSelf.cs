namespace TaranMagicFramework
{
    public class AIAbilityWorker_TargetSelf : AIAbilityWorker
    {
        public override bool CanActivate(Ability ability, out TargetData targetData)
        {
            if (base.CanActivate(ability, out targetData) && ability.pawn.mindState.CombatantRecently)
            {
                targetData = new(null, ability.pawn);
                return true;
            }
            return false;
        }
        public override float Priority(Ability ability)
        {
            return 9f;
        }
    }
}
