using System.Collections.Generic;
using Verse;

namespace TaranMagicFramework
{
    public class VerbCollection : IExposable
    {
        public List<Verb_ShootAbility> verbs = new();
        public List<Verb_MeleeAttackDamageAbility> meleeVerbs = new();
        public void ExposeData()
        {
            Scribe_Collections.Look(ref verbs, "verbs", LookMode.Deep);
            Scribe_Collections.Look(ref meleeVerbs, "meleeVerbs", LookMode.Deep);
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                verbs ??= new List<Verb_ShootAbility>();
                meleeVerbs ??= new List<Verb_MeleeAttackDamageAbility>();
            }
        }
    }
}
