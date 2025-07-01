using System;
using System.Collections.Generic;
using UnityEngine.Pool;

namespace UnityEngine.Localization
{
    /// <summary>
    /// Alternative to using a delegate/event.
    /// Designed to reduce the amount of memory allocations by allocating in blocks.
    /// </summary>
    /// <typeparam name="TDelegate"></typeparam>
    /// <example>
    /// To invoke do the following:
    /// <code>
    /// try
    /// {
    ///     m_ChangeHandler.LockForChanges();
    ///     var len = m_ChangeHandler.Length;
    ///     if (len == 1)
    ///     {
    ///         m_ChangeHandler.SingleDelegate(value);
    ///     }
    ///     else if (len > 1)
    ///     {
    ///         var array = m_ChangeHandler.MultiDelegates;
    ///         for (int i = 0; i < len; ++i)
    ///             array[i](value);
    ///     }
    /// }
    /// catch (Exception ex)
    /// {
    ///     Debug.LogException(ex);
    /// }
    /// m_ChangeHandler.UnlockForChanges();
    /// </code>
    /// </example>
    struct CallbackArray<TDelegate> where TDelegate : Delegate
    {
        const int k_AllocationIncrement = 5;

        public TDelegate SingleDelegate => m_SingleDelegate;
        public TDelegate[] MultiDelegates => m_MultipleDelegates;

        public int Length => m_Length;

        TDelegate m_SingleDelegate;
        TDelegate[] m_MultipleDelegates;
        List<TDelegate> m_AddCallbacks;
        List<TDelegate> m_RemoveCallbacks;
        int m_Length;
        bool m_CannotMutateCallbacksArray;
        bool m_MutatedDuringCallback;

        public void Add(TDelegate callback, int capacityIncrement = k_AllocationIncrement)
        {
            if (callback == null)
                return;

            if (m_CannotMutateCallbacksArray)
            {
                if (m_AddCallbacks == null)
                    m_AddCallbacks = ListPool<TDelegate>.Get();
                m_AddCallbacks.Add(callback);
                m_MutatedDuringCallback = true;
                return;
            }

            if (m_Length == 0)
            {
                m_SingleDelegate = callback;
            }
            else if (m_Length == 1)
            {
                m_MultipleDelegates = new TDelegate[capacityIncrement];
                m_MultipleDelegates[0] = m_SingleDelegate;
                m_MultipleDelegates[1] = callback;
                m_SingleDelegate = null;
            }
            else
            {
                if (m_MultipleDelegates.Length == m_Length)
                    Array.Resize(ref m_MultipleDelegates, m_Length + capacityIncrement);
                m_MultipleDelegates[m_Length] = callback;
            }

            ++m_Length;
        }

        public void RemoveByMovingTail(TDelegate callback)
        {
            if (callback == null)
                return;

            if (m_CannotMutateCallbacksArray)
            {
                if (m_RemoveCallbacks == null)
                    m_RemoveCallbacks = ListPool<TDelegate>.Get();
                m_RemoveCallbacks.Add(callback);
                m_MutatedDuringCallback = true;
                return;
            }

            if (m_Length <= 1)
            {
                // m_SingleDelegate == callback creates GC
                if (Equals(m_SingleDelegate, callback))
                {
                    m_SingleDelegate = null;
                    m_Length = 0;
                }
            }
            else
            {
                for (int i = 0; i < m_Length; ++i)
                {
                    // m_MultipleDelegates[i] == callback creates GC
                    if (Equals(m_MultipleDelegates[i], callback))
                    {
                        m_MultipleDelegates[i] = m_MultipleDelegates[m_Length - 1];
                        --m_Length;
                        break;
                    }
                }

                if (m_Length == 1)
                {
                    m_SingleDelegate = m_MultipleDelegates[0];
                    m_MultipleDelegates = null;
                }
            }
        }

        public void LockForChanges()
        {
            m_CannotMutateCallbacksArray = true;
        }

        public void UnlockForChanges()
        {
            m_CannotMutateCallbacksArray = false;

            if (m_MutatedDuringCallback)
            {
                // Process mutations that have happened while we were executing callbacks.
                if (m_AddCallbacks != null)
                {
                    foreach (var cb in m_AddCallbacks)
                        Add(cb);
                    ListPool<TDelegate>.Release(m_AddCallbacks);
                    m_AddCallbacks = null;
                }

                if (m_RemoveCallbacks != null)
                {
                    foreach (var cb in m_RemoveCallbacks)
                        RemoveByMovingTail(cb);
                    ListPool<TDelegate>.Release(m_RemoveCallbacks);
                    m_RemoveCallbacks = null;
                }

                m_MutatedDuringCallback = true;
            }
        }

        public void Clear()
        {
            m_SingleDelegate = null;
            m_MultipleDelegates = null;
            m_Length = 0;
        }
    }
}
