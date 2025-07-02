using System.Collections.Generic;
using Verse;

namespace TaranMagicFramework
{
    public class Ability_Toggleable : Ability
    {
        public override IEnumerable<Gizmo> GetGizmos()
        {
            yield return GetToggleAbilityGizmo();
        }
        public override bool IsInstantAction => false;
    }
}
