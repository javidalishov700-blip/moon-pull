using System;

namespace MoonPull.Rewind
{
    /// <summary>Fixed-capacity overwrite-oldest ring buffer for value types. Allocates once, never in the loop.</summary>
    public sealed class RingBuffer<T> where T : struct
    {
        private readonly T[] items;
        private int head;

        public RingBuffer(int capacity)
        {
            if (capacity <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(capacity));
            }

            items = new T[capacity];
        }

        public int Capacity => items.Length;
        public int Count { get; private set; }

        public void Push(in T item)
        {
            items[head] = item;
            head = (head + 1) % items.Length;
            if (Count < items.Length)
            {
                Count++;
            }
        }

        /// <summary>0 = newest, Count - 1 = oldest.</summary>
        public ref readonly T FromNewest(int stepsBack)
        {
            if (stepsBack < 0 || stepsBack >= Count)
            {
                throw new ArgumentOutOfRangeException(nameof(stepsBack));
            }

            int index = (head - 1 - stepsBack + items.Length * 2) % items.Length;
            return ref items[index];
        }

        public ref readonly T Oldest => ref FromNewest(Count - 1);

        public void Clear()
        {
            head = 0;
            Count = 0;
        }
    }
}
