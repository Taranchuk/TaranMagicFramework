using UnityEngine;
using Verse;

namespace TaranMagicFramework
{
    public interface CommandAbility
    {
        Ability Ability { get; }
    }
    [StaticConstructorOnStartup]
    public class Command_ActivateAbility : Command_Action, CommandAbility
    {
        private static readonly Texture2D cooldownBarTex = SolidColorMaterials.NewSolidColorTexture(Color.gray.r, Color.gray.g, Color.gray.b, 0.6f);

        private Ability ability;
        public Command_ActivateAbility(Ability ability)
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
            }
            if (ability.HasCharge)
            {

            }
            if (result.State == GizmoState.Interacted)
            {
                return result;
            }
            return new GizmoResult(result.State);
        }
    }
}
