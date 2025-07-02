using UnityEngine;

namespace TaranMagicFramework
{
    public interface IAnimated
    {
        public int CurInd { get; set; }
        public int CurLoopInd { get; set; }
        public int PrevTick { get; set; }
        public void OnCycle_Completion();
        Vector2? SizeOverride { get; }
        AnimationDef AnimationDef { get; }
        float Angle { get; }
        Vector3 ExactScale { get; }
    }
}
