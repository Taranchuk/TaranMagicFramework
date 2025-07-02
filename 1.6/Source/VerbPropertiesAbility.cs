using HarmonyLib;
using UnityEngine;
using Verse;

namespace TaranMagicFramework
{
    public class VerbPropertiesAbility : VerbProperties
    {
        public bool autocast;
        public float autocastChance;
        public AnimationDef warmupAnimation;
        public Vector3 warmupAnimationOffset;
        public AnimationDef burstAnimation;
        public int maxNumMeleeAttacks = int.MaxValue;
        public AnimationDef animationOnTargetWhenHit;

        [HarmonyPatch(typeof(VerbProperties), "GetForceMissFactorFor")]
        public static class VerbProperties_GetForceMissFactorFor_Patch
        {
            public static bool Prefix(VerbProperties __instance, ref float __result)
            {
                if (__instance is VerbPropertiesAbility)
                {
                    __result = 1f;
                    return false;
                }
                return true;
            }
        }
    }
}
