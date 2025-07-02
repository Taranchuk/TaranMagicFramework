using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace TaranMagicFramework
{
    public class AbilityTabDef : Def
    {
        public Type tabClass;
        public List<AbilityClassDef> abilityClasses;
        public string labelKey;
        public Vector2 size;
    }
}
