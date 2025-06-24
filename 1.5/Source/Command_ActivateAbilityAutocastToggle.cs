using UnityEngine;
using Verse;

namespace TaranMagicFramework
{
    [StaticConstructorOnStartup]
    public class Command_ActivateAbilityAutocastToggle : Command_Action, CommandAbility
    {
        private static readonly Texture2D cooldownBarTex = SolidColorMaterials.NewSolidColorTexture(Color.gray.r, Color.gray.g, Color.gray.b, 0.6f);

        private Ability ability;
        public Command_ActivateAbilityAutocastToggle(Ability ability)
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

            var texture = ability.autocastEnabled ? Widgets.CheckboxOnTex : Widgets.CheckboxOffTex;
            var position = new Rect(rect.x + rect.width - 24f, rect.y, 24f, 24f);
            GUI.DrawTexture(position, texture);

            if (result.State == GizmoState.Interacted)
            {
                return result;
            }
            return new GizmoResult(result.State);
        }

        public override void ProcessInput(Event ev)
        {
            if (Event.current.button == 1)
            {
                ability.autocastEnabled = !ability.autocastEnabled;
            }
            else
            {
                base.ProcessInput(ev);
            }
        }
    }
}
