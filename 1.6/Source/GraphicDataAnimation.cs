using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace TaranMagicFramework
{
    public class GraphicDataAnimation : GraphicData
    {
        public float transparency = 1f;
        public float animationSpeedRate = 1f;

        private Material[] cachedMaterials;

        private static Dictionary<string, Material[]> loadedMaterials = new();
        public Material[] Materials
        {
            get
            {
                if (cachedMaterials == null)
                {
                    InitMainTextures();
                }
                return cachedMaterials;
            }
        }
        public void InitMainTextures()
        {
            if (!loadedMaterials.TryGetValue(texPath, out var materials))
            {
                var mainTextures = LoadAllFiles(texPath).OrderBy(x => x).ToList();
                if (mainTextures.Count > 0)
                {
                    cachedMaterials = new Material[mainTextures.Count];
                    for (int i = 0; i < mainTextures.Count; i++)
                    {
                        var shader = shaderType != null ? shaderType.Shader : ShaderDatabase.DefaultShader;
                        cachedMaterials[i] = MaterialPool.MatFrom(mainTextures[i], shader, color);
                    }
                }
                loadedMaterials[texPath] = cachedMaterials;
            }
            else
            {
                cachedMaterials = materials;
            }
        }
        public List<string> LoadAllFiles(string folderPath)
        {
            var list = new List<string>();
            foreach (var mod in LoadedModManager.RunningModsListForReading)
            {
                foreach (var f in ModContentPack.GetAllFilesForMod(mod, "Textures/" + folderPath))
                {
                    string path = f.Value.FullName;
                    if (path.EndsWith(".png"))
                    {
                        path = path.Replace("\\", "/");
                        path = path.Substring(path.IndexOf("/Textures/") + 10);
                        path = path.Replace(".png", "");
                        list.Add(path);
                    }
                }
            }
            return list;
        }
    }
}
