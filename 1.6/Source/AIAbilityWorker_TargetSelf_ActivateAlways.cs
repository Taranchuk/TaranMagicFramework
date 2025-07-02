namespace TaranMagicFramework
{
    public class AIAbilityWorker_TargetSelf_ActivateAlways : AIAbilityWorker
    {
        public override bool CanActivate(Ability ability, out TargetData targetData)
        {
            if (base.CanActivate(ability, out targetData))
            {
                targetData = new(null, ability.pawn);
                return true;
            }
            return false;
        }
        public override float Priority(Ability ability)
        {
            return 999f;
        }
    }
}
