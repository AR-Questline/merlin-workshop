using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace Unity.Services.UserReporting.Client
{
    class ConcurrentCycle<T>
    {
        public ConcurrentCycle(int capacity)
        {
            if (capacity < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(capacity),
                    $"{nameof(capacity)} must be at least 0");
            }
            Capacity = capacity;
            m_Queue = new ConcurrentQueue<T>();
        }

        public readonly int Capacity;

        ConcurrentQueue<T> m_Queue;

        public int Count => m_Queue.Count;

        public void Add(T item)
        {
            m_Queue.Enqueue(item);
            lock (m_Queue)
            {
                while (Count > Capacity)
                {
                    m_Queue.TryDequeue(out _);
                }
            }
        }

        public object GetNextEviction()
        {
            object result;
            lock (m_Queue)
            {
                result = m_Queue.TryPeek(out T content) ? content : null;
            }

            return result;
        }

        public List<T> ToList()
        {
            // Moment-in-time snapshot copy of queue contents.
            List<T> result;
            lock (m_Queue)
            {
                result = m_Queue.ToList();
            }
            return result;
        }
    }
}
