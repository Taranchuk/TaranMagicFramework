using System;
using System.Linq;
using UnityEngine;
using Verse;

namespace TaranMagicFramework
{
    [StaticConstructorOnStartup]
    public class Graphic_Animation : Graphic
    {
        public Graphic[] subGraphics;

        public IAnimated animated;
        public override void Init(GraphicRequest req)
        {
            data = req.graphicData;

            if (req.path.NullOrEmpty())
            {
                throw new ArgumentNullException("folderPath");
            }
            if (req.shader == null)
            {
                throw new ArgumentNullException("shader");
            }
            path = req.path;
            color = req.color;

            if (data is GraphicDataAnimation graphicAnimation)
            {
                color.a = graphicAnimation.transparency;
                colorTwo.a = graphicAnimation.transparency;
            }

            colorTwo = req.colorTwo;
            drawSize = req.drawSize;
            var list = (from x in ContentFinder<Texture2D>.GetAllInFolder(req.path)
                        where !x.name.EndsWith(Graphic_Single.MaskSuffix)
                        orderby x.name
                        select x).ToList();
            if (list.NullOrEmpty())
            {
                Log.Error("Collection cannot init: No textures found at path " + req.path);
                subGraphics = new Graphic[1]
                {
                    BaseContent.BadGraphic
                };
                return;
            }
            subGraphics = new Graphic[list.Count];
            for (int i = 0; i < list.Count; i++)
            {
                string path = req.path + "/" + list[i].name;
                subGraphics[i] = GraphicDatabase.Get(typeof(Graphic_Single), path, req.shader, drawSize, color, colorTwo, null, req.shaderParameters);
            }
        }

        protected MaterialPropertyBlock propertyBlock = new MaterialPropertyBlock();

        public override void DrawWorker(Vector3 loc, Rot4 rot, ThingDef thingDef, Thing thing, float extraRotation)
        {
            if (animated is null || animated.AnimationDef is null)
            {
                base.DrawWorker(loc, rot, thingDef, thing, extraRotation);
                return;
            }
            var sizeOverride = animated.SizeOverride;
            var s = sizeOverride.HasValue ? new Vector3(sizeOverride.Value.x, 1f, sizeOverride.Value.y) : new Vector3(1, 1f, 1);
            var exactScale = animated.ExactScale;
            s.x *= exactScale.x;
            s.z *= exactScale.z;
            var matrix = default(Matrix4x4);
            if (thingDef.rotatable is false)
            {
                rot = Rot4.North;
            }
            var quat = animated.Angle != 0 ? Quaternion.AngleAxis(animated.Angle, Vector3.up) : rot.AsQuat;
            if (this is Graphic_Animation_Multi)
            {
                quat = Quaternion.identity;
            }
            matrix.SetTRS(loc, quat, s);
            var curGraphic = subGraphics[animated.CurInd];
            var material = curGraphic.MatAt(rot, thing);
            if (animated is Mote_Animation mote)
            {
                float alpha = mote.Alpha;
                Color color2 = color * mote.instanceColor;
                color2.a *= alpha;
                if (color2.IndistinguishableFrom(material.color) is false)
				{
                    propertyBlock.SetColor(ShaderPropertyIDs.Color, color2);
                }
            }

            Graphics.DrawMesh(curGraphic.MeshAt(rot), matrix, material, 0, null, 0, propertyBlock);
            if (animated.PrevTick != Find.TickManager.TicksGame && ((animated.AnimationDef.lockToRealTime 
                && Time.frameCount % animated.AnimationDef.fpsRate == 0)
                || (!animated.AnimationDef.lockToRealTime && Find.TickManager.TicksGame % animated.AnimationDef.fpsRate == 0)))
            {
                if (animated.CurInd < subGraphics.Length - 1)
                {
                    animated.CurInd++;
                }
                if (animated.CurInd >= subGraphics.Length - 1)
                {
                    animated.CurLoopInd++;
                    animated.OnCycle_Completion();
                    if (animated.AnimationDef.maxLoopCount > animated.CurLoopInd || animated.AnimationDef.maxLoopCount <= 0)
                    {
                        animated.CurInd = 0;
                    }
                }
            }
            animated.PrevTick = Find.TickManager.TicksGame;
        }

        public override Graphic GetColoredVersion(Shader newShader, Color newColor, Color newColorTwo)
        {
            return GraphicDatabase.Get<Graphic_Animation>(path, newShader, drawSize, newColor, newColorTwo, data);
        }
    }
}
