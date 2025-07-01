// Animancer // https://kybernetik.com.au/animancer // Copyright 2018-2023 Kybernetik //

#if UNITY_EDITOR

using System;
using UnityEditor;
using UnityEngine;

namespace Animancer.Editor
{
    /// <summary>[Editor-Only] Draws the Inspector GUI for an <see cref="AnimancerState"/>.</summary>
    /// https://kybernetik.com.au/animancer/api/Animancer.Editor/ParametizedAnimancerStateDrawer_1
    /// 
    public abstract class ParametizedAnimancerStateDrawer<T> : AnimancerStateDrawer<T> where T : AnimancerState
    {
        /************************************************************************************************************************/

        /// <summary>The number of parameters being managed by the target state.</summary>
        public virtual int ParameterCount => 0;

        /// <summary>Returns the name of a parameter being managed by the target state.</summary>
        /// <exception cref="NotSupportedException">The target state doesn't manage any parameters.</exception>
        public virtual string GetParameterName(int index) => throw new NotSupportedException();

        /// <summary>Returns the type of a parameter being managed by the target state.</summary>
        /// <exception cref="NotSupportedException">The target state doesn't manage any parameters.</exception>
        public virtual AnimatorControllerParameterType GetParameterType(int index) => throw new NotSupportedException();

        /// <summary>Returns the value of a parameter being managed by the target state.</summary>
        /// <exception cref="NotSupportedException">The target state doesn't manage any parameters.</exception>
        public virtual object GetParameterValue(int index) => throw new NotSupportedException();

        /// <summary>Sets the value of a parameter being managed by the target state.</summary>
        /// <exception cref="NotSupportedException">The target state doesn't manage any parameters.</exception>
        public virtual void SetParameterValue(int index, object value) => throw new NotSupportedException();

        /************************************************************************************************************************/

        /// <summary>
        /// Creates a new <see cref="ParametizedAnimancerStateDrawer{T}"/> to manage the Inspector GUI for the `state`.
        /// </summary>
        protected ParametizedAnimancerStateDrawer(T state) : base(state) {
        }

        /************************************************************************************************************************/

        /// <inheritdoc/>
        protected override void DoDetailsGUI()
        {
        }

        /************************************************************************************************************************/
    }
}

#endif

