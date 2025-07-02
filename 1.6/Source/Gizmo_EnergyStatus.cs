using RimWorld;
using UnityEngine;
using Verse;

namespace TaranMagicFramework
{
    [StaticConstructorOnStartup]
    public class Gizmo_EnergyStatus : Gizmo
    {
        public AbilityResource abilityResource;

        private static readonly Texture2D FullShieldBarTex = SolidColorMaterials.NewSolidColorTexture(new Color(0.2f, 0.2f, 0.24f));

        private static readonly Texture2D EmptyShieldBarTex = SolidColorMaterials.NewSolidColorTexture(Color.clear);

        public Gizmo_EnergyStatus()
        {
            order = -100f;
        }

        public override float GetWidth(float maxWidth)
        {
            return 140f;
        }

        public override GizmoResult GizmoOnGUI(Vector2 topLeft, float maxWidth, GizmoRenderParms parms)
        {
            var rect = new Rect(topLeft.x, topLeft.y, GetWidth(maxWidth), 75f);
            var rect2 = rect.ContractedBy(6f);
            Widgets.DrawWindowBackground(rect);
            var rect3 = rect2;
            rect3.height = rect.height / 2f;
            Text.Font = GameFont.Tiny;
            Widgets.Label(rect3, "TMF.Energy".Translate(abilityResource.def.label));
            var rect4 = rect2;
            rect4.yMin = rect2.y + (rect2.height / 2f);
            float fillPercent = abilityResource.EnergyPct;
            Widgets.FillableBar(rect4, fillPercent, FullShieldBarTex, EmptyShieldBarTex, doBorder: false);
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.MiddleCenter;
            Widgets.Label(rect4, abilityResource.energy.ToString("F0") + " / " + abilityResource.MaxEnergy);
            Text.Anchor = TextAnchor.UpperLeft;
            return new GizmoResult(GizmoState.Clear);
        }
    }
}
