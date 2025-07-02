using HarmonyLib;
using Verse;
using Verse.AI;

namespace TaranMagicFramework
{
    public class JobAbility : Job
    {
        public new Ability ability;

        [HarmonyPatch(typeof(Job), "ExposeData")]
        public static class Job_ExposeData
        {
            public static void Postfix(Job __instance)
            {
                if (__instance is JobAbility jobAbility)
                {
                    Scribe_References.Look(ref jobAbility.ability, "TMF_jobAbility");
                }
            }
        }
    }
}
