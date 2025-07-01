namespace CrazyMinnow.SALSA
{
    public class Switcher : IExpressionController
    {
        protected Switcher.OnState onState = Switcher.OnState.OnUntilOff;
        protected float frame;
        protected float currentDelta;
        protected bool isRestNull;
        private float fracMax;
        
        public enum OnState
        {
            OnWhenActive,
            OnActiveOne,
            OnUntilOff,
        }
    }
}