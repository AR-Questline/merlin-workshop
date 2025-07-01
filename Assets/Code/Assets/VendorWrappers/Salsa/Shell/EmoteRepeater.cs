namespace CrazyMinnow.SALSA
{
    public class EmoteRepeater
    {
        public EmoteExpression repeater;
        public readonly float timer;
        public float timeCheck;
        
        public enum StartDelay
        {
            Immediately,
            AfterDelay,
            AfterBaseCycle,
            AfterFullCycle,
        }
    }
}