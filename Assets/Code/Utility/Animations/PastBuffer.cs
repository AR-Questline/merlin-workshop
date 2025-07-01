using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Awaken.Utility.Animations {
    [UnityEngine.Scripting.Preserve]
    class PastBuffer<T> {
        // ========================= Fields

        T[] _buffer;
        int _current, _capacity;
        int _filled;

        // ========================= Constructors

        public PastBuffer(int capacity) {
            _buffer = new T[capacity];
            _current = 0;
            _filled = 0;
            _capacity = capacity;
        }

        // ========================= Operation

        public void Add(T value) {
            _buffer[_current] = value;
            _current = (_current + 1) % _capacity;
            _filled = (_filled < _capacity) ? _filled + 1 : _filled;
        }

        public T Current() => Get(0);
        public T Previous() => Get(1);
        public T Get(int age) {
            if (age > _capacity)
                throw new ArgumentException($"Buffer of size {_capacity} cannot give you an element with age {age}.");
            int index = (_current + _capacity - 1 - age) % _capacity;
            return _buffer[index];
        }

        public List<T> Last(int howMany) {
            howMany = howMany <= _filled ? howMany : _filled;
            return Enumerable.Range(0, howMany).Select(i => Get(i)).ToList();
        }
    }
}
