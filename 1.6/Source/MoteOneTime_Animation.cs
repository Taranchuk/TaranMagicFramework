namespace TaranMagicFramework
{
    public class MoteOneTime_Animation : Mote_Animation
    {
        public override void OnCycle_Completion()
        {
            destroy = true;
        }
    }
}
