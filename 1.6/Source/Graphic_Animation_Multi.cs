using System;
using System.Linq;
using UnityEngine;
using Verse;

namespace TaranMagicFramework
{
    public class Graphic_Animation_Multi : Graphic_Animation
    {
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
                        select x.name.Replace("_west", "")
                        .Replace("_east", "")
                        .Replace("_south", "")
                        .Replace("_north", "")).Distinct().OrderBy(x => x).ToList();
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
                string path = req.path + "/" + list[i];
                var multi = GraphicDatabase.Get(typeof(Graphic_Multi), path, req.shader, drawSize,
                    color, colorTwo, null, req.shaderParameters) as Graphic_Multi;
                subGraphics[i] = multi;
            }
        }
    }
}
