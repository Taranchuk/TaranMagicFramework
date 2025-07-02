using UnityEngine;
using Verse;

namespace TaranMagicFramework
{
    [StaticConstructorOnStartup]
    public class Command_ToggleAbility : Command_Toggle, CommandAbility
    {
        private static readonly Texture2D cooldownBarTex = SolidColorMaterials.NewSolidColorTexture(Color.gray.r, Color.gray.g, Color.gray.b, 0.6f);

        private Ability ability;
        public Command_ToggleAbility(Ability ability)
        {
            this.ability = ability;
            order = 5f;
        }

        public Ability Ability => ability;

        public override GizmoResult GizmoOnGUI(Vector2 topLeft, float maxWidth, GizmoRenderParms parms)
        {
            var rect = new Rect(topLeft.x, topLeft.y, GetWidth(maxWidth), 75f);
            var result = base.GizmoOnGUI(topLeft, maxWidth, parms);
            if (ability.lastActivatedTick > 0)
            {
                int cooldownTicksRemaining = Find.TickManager.TicksGame - ability.lastActivatedTick;
                if (cooldownTicksRemaining < ability.cooldownPeriod)
                {
                    float num = Mathf.InverseLerp(ability.cooldownPeriod, 0, cooldownTicksRemaining);
                    Widgets.FillableBar(rect, Mathf.Clamp01(num), cooldownBarTex, null, doBorder: false);
                }

                if (ability.Active && ability.AbilityTier.durationTicks != 0)
                {
                    int durationTicksRemaining = Find.TickManager.TicksGame - ability.lastActivatedTick;
                    if (durationTicksRemaining < ability.AbilityTier.durationTicks)
                    {
                        float num = Mathf.InverseLerp(ability.AbilityTier.durationTicks, 0, durationTicksRemaining);
                        Widgets.FillableBar(rect, Mathf.Clamp01(num), cooldownBarTex, null, doBorder: false);
                    }
                }
            }

            if (result.State == GizmoState.Interacted)
            {
                return result;
            }
            return new GizmoResult(result.State);
        }
    }
}
