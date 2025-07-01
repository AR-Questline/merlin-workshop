using System.Collections.Generic;

namespace Sirenix.Utilities.Editor
{
    public class GUIScopeStack<T>
    {
        public Stack<T> Stack = new();

        public int Count => Stack.Count;

        public void Push(T t)
        {
            Stack.Push(t);
        }

        public T Pop()
        {
            return Stack.Pop();
        }

        public T Peek()
        {
            return Stack.Peek();
        }
    }
}