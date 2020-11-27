using System.Collections.Generic;

namespace Spriter2Nez
{
    public static class StackPool<T>
    {
        private static readonly Queue<Stack<T>> pool = new Queue<Stack<T>>();

        public static Stack<T> Get(int capacity = 32)
        {
            if (pool.Count > 0)
                return pool.Dequeue();
            return new Stack<T>(capacity);
        }

        public static void Release(Stack<T> stack)
        {
            if (stack == null)
                return;
#if DEBUG
            if (pool.Contains(stack))
                throw new System.Exception("Stack has already been released into pool!");
#endif
            stack.Clear();
            pool.Enqueue(stack);
        }
    }
}
