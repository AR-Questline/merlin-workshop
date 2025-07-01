using System;
using System.Runtime.CompilerServices;

namespace DG.Tweening
{
    public struct TweenAwaiter : INotifyCompletion
    {
        private readonly Tween _tween;

        public TweenAwaiter(Tween tween) {
            _tween = tween;
        }

        public bool IsCompleted => !_tween.IsActive();
        
        public void GetResult() {
            if (!IsCompleted) 
            {
                _tween.Complete();
            }
        }
        
        public void OnCompleted(Action continuation) {
            _tween.onKill = () => continuation?.Invoke();
        }
    }

    public static class TweenAwaiterUtils
    {
        public static TweenAwaiter GetAwaiter(this Tween tween)
        {
            return new TweenAwaiter(tween);
        }
    }
}