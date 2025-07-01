using System;
using System.Runtime.CompilerServices;

namespace DG.Tweening
{
    public struct SequenceAwaiter : INotifyCompletion
    {
        private readonly Sequence _sequence;

        public SequenceAwaiter(Sequence sequence)
        {
            _sequence = sequence;
        }

        public bool IsCompleted => _sequence.IsComplete();

        public void GetResult()
        {
            if (!IsCompleted)
            {
                _sequence.Complete();
            }
        }

        public void OnCompleted(Action continuation)
        {
            _sequence.OnComplete(() => continuation?.Invoke());
        }
    }

    public static class SequenceAwaiterUtils
    {
        public static SequenceAwaiter GetAwaiter(this Sequence sequence)
        {
            return new SequenceAwaiter(sequence);
        }
    }
}