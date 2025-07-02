using Verse;

namespace TaranMagicFramework
{
    public class TargetData
    {
        public Verb verb;
        public LocalTargetInfo target;

        public TargetData()
        {

        }
        public TargetData(Verb verb, LocalTargetInfo target)
        {
            this.verb = verb;
            this.target = target;
        }
    }
}
