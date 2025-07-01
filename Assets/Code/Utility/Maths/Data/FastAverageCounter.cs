using Awaken.Utility.Collections;

namespace Awaken.Utility.Maths.Data {
    public struct FastAverageCounterUlong {

        readonly int _ord;
        readonly SinkingBuffer<ulong> _framesTicks;
        
        public ulong Sum { get; private set; }
        public ulong Average { get; private set; }

        public FastAverageCounterUlong(int ord) {
            _ord = ord;
            _framesTicks = new SinkingBuffer<ulong>(1 << _ord);
            Sum = 0;
            Average = 0;
        }

        public void Push(ulong value) {
            _framesTicks.Push(value, out var change);
            Sum = Sum + change.added - change.sunk;
            Average = Sum >> _ord;
        }
    }
}