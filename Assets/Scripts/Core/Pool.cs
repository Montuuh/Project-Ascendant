using System;
using System.Collections.Generic;

namespace ProjectAscendant.Core
{
    // Per §9.6.1 — generic object pool eliminating hot-path GC allocations.
    // All five hot-path factory types use this pool. (§9.11.4 — zero allocs in combat loop.)
    public sealed class Pool<T> where T : class, new()
    {
        private readonly Stack<T> _free;
        private readonly int _maxCapacity;
        private readonly Action<T> _resetHook;

        public int FreeCount => _free.Count;

        public Pool(int initialCapacity = 8, int maxCapacity = 64, Action<T> resetHook = null)
        {
            _maxCapacity = maxCapacity;
            _resetHook = resetHook;
            _free = new Stack<T>(initialCapacity);
            for (int i = 0; i < initialCapacity; i++)
                _free.Push(new T());
        }

        public T Rent()
        {
            return _free.Count > 0 ? _free.Pop() : new T();
        }

        // Returns item to pool. Calls resetHook if provided, then pushes.
        // Items returned when pool is full are silently discarded (no exception).
        public void Return(T item)
        {
            if (item == null || _free.Count >= _maxCapacity)
                return;

            _resetHook?.Invoke(item);
            _free.Push(item);
        }
    }
}
