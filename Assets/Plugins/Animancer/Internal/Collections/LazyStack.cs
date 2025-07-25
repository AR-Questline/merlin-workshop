// Animancer // https://kybernetik.com.au/animancer // Copyright 2018-2023 Kybernetik //

using System.Collections.Generic;

namespace Animancer
{
    /// <summary>A simple stack implementation that tracks an active index without actually adding or removing objects.</summary>
    /// https://kybernetik.com.au/animancer/api/Animancer/LazyStack_1
    /// 
    public class LazyStack<T> where T : new()
    {
        /************************************************************************************************************************/

        /// <summary>The underlying collection of objects.</summary>
        /// <remarks>
        /// This is not a <see cref="Stack{T}"/> because that class comes from a different assembly that might not
        /// otherwise need to be included in builds, so using a <see cref="List{T}"/> can slightly reduce build size.
        /// </remarks>
        private readonly List<T> Stack;

        /// <summary>The index of the <see cref="Current"/> object in the <see cref="Stack"/>.</summary>
        private int _CurrentIndex = -1;

        /// <summary>The object currently on the top of the stack.</summary>
        public T Current { get; private set; }

        /************************************************************************************************************************/

        /// <summary>Creates a new <see cref="LazyStack{T}"/> with a default internal list capacity of 16.</summary>
        public LazyStack()
        {
        }

        /// <summary>Creates a new <see cref="LazyStack{T}"/> with the specified internal list capacity.</summary>
        public LazyStack(int capacity)
        {
        }

        /************************************************************************************************************************/

        /// <summary>Moves to the next object in the stack.</summary>
        public T Increment()
        {
            return default;
        }

        /************************************************************************************************************************/

        /// <summary>Moves to the previous object in the stack.</summary>
        public void Decrement()
        {
        }

        /************************************************************************************************************************/
    }
}

